class_name MatchController
extends RefCounted

const ActionResult = preload("res://scripts/core/action_result.gd")
const CardInstance = preload("res://scripts/core/card_instance.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchState = preload("res://scripts/core/match_state.gd")
const PlayerState = preload("res://scripts/core/player_state.gd")

var state: MatchState
var initial_deck_ids: Dictionary
var _definitions: Dictionary
var _rng := RandomNumberGenerator.new()

static func create(card_definitions: Dictionary, player_deck: Array, opponent_deck: Array, seed: int) -> MatchController:
	var controller: MatchController = load("res://scripts/core/match_controller.gd").new()
	controller._definitions = card_definitions.duplicate(true)
	controller.initial_deck_ids = {
		"player": player_deck.duplicate(),
		"opponent": opponent_deck.duplicate(),
	}
	controller._rng.seed = seed
	var player := controller._create_player("player", "p", player_deck)
	var opponent := controller._create_player("opponent", "o", opponent_deck)
	controller.state = MatchState.create(player, opponent, seed)
	controller._shuffle_deck(player.deck)
	controller._shuffle_deck(opponent.deck)
	return controller

func submit_action(action: GameAction) -> ActionResult:
	if action.expected_sequence > 0 and action.expected_sequence != state.sequence:
		return _reject(action, "stale_action", "State changed")
	match action.type:
		"start_match":
			return _start_match(action)
		"mulligan":
			return _mulligan(action)
		"confirm_mulligan":
			return _confirm_mulligan(action)
		"end_turn":
			return _end_turn(action)
		_:
			return _reject(action, "unknown_action", "Unknown action")

func _create_player(player_id: String, instance_prefix: String, deck_ids: Array) -> PlayerState:
	var cards: Array = []
	var headquarters: CardInstance = CardInstance.headquarters("", player_id, "%s-hq" % instance_prefix)
	var nation := ""
	for index in range(deck_ids.size()):
		var definition_id := str(deck_ids[index])
		var definition: Dictionary = _definitions.get(definition_id, {})
		var instance_id := "%s-%03d" % [instance_prefix, index]
		if str(definition.get("category", "")) == "Headquarters":
			headquarters = CardInstance.headquarters(definition_id, player_id, instance_id)
			nation = str(definition.get("nation", ""))
			continue
		var card := CardInstance.from_definition(definition, player_id, instance_id)
		card.zone = "deck"
		cards.append(card)
	return PlayerState.create(player_id, nation, headquarters, cards)

func _start_match(action: GameAction) -> ActionResult:
	if state.phase != "setup":
		return _reject(action, "invalid_phase", "Match already started")
	if action.actor_id != "system":
		return _reject(action, "invalid_actor", "Only the system starts a match")
	state.starting_player_id = "player" if _rng.randi_range(0, 1) == 0 else "opponent"
	state.rng_state = _rng.state
	var events: Array = []
	var starting_player: PlayerState = state.players[state.starting_player_id]
	var other_player: PlayerState = state.players[_other_player_id(state.starting_player_id)]
	_draw_opening_cards(starting_player, 4, events)
	_draw_opening_cards(other_player, 5, events)
	state.phase = "mulligan"
	return _accept(action, events)

func _mulligan(action: GameAction) -> ActionResult:
	if state.phase != "mulligan":
		return _reject(action, "invalid_phase", "Mulligan is not available")
	if not state.players.has(action.actor_id):
		return _reject(action, "invalid_actor", "Unknown player")
	var player: PlayerState = state.players[action.actor_id]
	if player.mulligan_confirmed:
		return _reject(action, "mulligan_confirmed", "Mulligan already confirmed")
	if player.mulligan_used:
		return _reject(action, "mulligan_used", "Mulligan already used")
	var selected_cards: Array = []
	var selected_ids := {}
	for selected_id_value in action.target_ids:
		var selected_id := str(selected_id_value)
		if selected_ids.has(selected_id):
			return _reject(action, "invalid_selection", "Duplicate mulligan selection")
		selected_ids[selected_id] = true
		var selected_card = _card_in_hand(player, selected_id)
		if selected_card == null:
			return _reject(action, "invalid_selection", "Mulligan card is not in hand")
		selected_cards.append(selected_card)

	if selected_cards.is_empty():
		player.mulligan_used = true
		return _accept(action)

	var events: Array = []
	for card in selected_cards:
		player.hand.erase(card)
		card.zone = "deck"
		player.deck.append(card)
		_emit(events, "card_returned", {"player_id": player.id, "instance_id": card.instance_id})
	_shuffle_deck(player.deck)
	_emit(events, "deck_shuffled", {"player_id": player.id})
	for ignored in selected_cards:
		_draw_card(player, events)
	player.mulligan_used = true
	return _accept(action, events)

func _confirm_mulligan(action: GameAction) -> ActionResult:
	if state.phase != "mulligan":
		return _reject(action, "invalid_phase", "Mulligan is not available")
	if not state.players.has(action.actor_id):
		return _reject(action, "invalid_actor", "Unknown player")
	var player: PlayerState = state.players[action.actor_id]
	if player.mulligan_confirmed:
		return _reject(action, "already_confirmed", "Mulligan already confirmed")
	player.mulligan_confirmed = true
	var events: Array = []
	if _both_mulligans_confirmed():
		state.phase = "action"
		state.active_player_id = state.starting_player_id
		_start_turn(state.players[state.active_player_id], events)
	return _accept(action, events)

func _end_turn(action: GameAction) -> ActionResult:
	if state.phase != "action":
		return _reject(action, "invalid_phase", "Turn actions are not available")
	if action.actor_id != state.active_player_id:
		return _reject(action, "not_active_player", "Only the active player can end the turn")
	var events: Array = []
	var player: PlayerState = state.players[action.actor_id]
	var next_player_id := _other_player_id(player.id)
	var next_player: PlayerState = state.players[next_player_id]
	_resolve_trigger("turn_end", {"player_id": player.id}, events)
	_expire_temporary_modifiers(player)
	next_player.deactivate_countermeasures()
	_emit(events, "turn_ended", {"player_id": player.id, "turn": state.turn})
	state.active_player_id = next_player_id
	_start_turn(next_player, events)
	return _accept(action, events)

func _draw_opening_cards(player: PlayerState, count: int, events: Array) -> void:
	for index in range(count):
		_draw_card(player, events)

func _draw_card(player: PlayerState, events: Array) -> void:
	if player.deck.is_empty():
		var damage := player.fatigue
		player.headquarters.current_defense = maxi(0, player.headquarters.current_defense - damage)
		player.fatigue += 1
		_emit(events, "fatigue_damage", {"player_id": player.id, "damage": damage})
		_check_headquarters_death(player)
		return
	var card: CardInstance = player.deck.pop_back()
	if player.hand.size() >= GameConstants.MAX_HAND_SIZE:
		card.zone = "discard"
		player.discard.append(card)
		_emit(events, "card_overdrawn", {"player_id": player.id, "instance_id": card.instance_id})
		return
	card.zone = "hand"
	player.hand.append(card)
	_emit(events, "card_drawn", {"player_id": player.id, "instance_id": card.instance_id})

func _start_turn(player: PlayerState, events: Array) -> void:
	state.turn += 1
	player.turns_started += 1
	_emit(events, "turn_started", {"player_id": player.id, "turn": state.turn})
	var previous_credit_slots := player.credit_slots
	player.credit_slots = mini(GameConstants.MAX_CREDITS, player.credit_slots + 1)
	if player.credit_slots != previous_credit_slots:
		_emit(events, "credit_slots_changed", {"player_id": player.id, "credit_slots": player.credit_slots})
	player.credit = player.credit_slots
	player.reset_operations()
	_emit(events, "credit_refilled", {"player_id": player.id, "credit": player.credit})
	_draw_card(player, events)
	_resolve_trigger("turn_start", {"player_id": player.id}, events)

func debug_draw(player_id: String) -> Array:
	if not state.players.has(player_id):
		return []
	var events: Array = []
	_draw_card(state.players[player_id], events)
	return events

func _resolve_trigger(trigger: String, context: Dictionary, events: Array) -> void:
	pass

func _expire_temporary_modifiers(player: PlayerState) -> void:
	pass

func _check_headquarters_death(player: PlayerState) -> void:
	if player.headquarters.current_defense > 0:
		return
	state.winner_id = _other_player_id(player.id)
	state.phase = "complete"

func _shuffle_deck(deck: Array) -> void:
	for index in range(deck.size() - 1, 0, -1):
		var swap_index := _rng.randi_range(0, index)
		var card = deck[index]
		deck[index] = deck[swap_index]
		deck[swap_index] = card
	if state != null:
		state.rng_state = _rng.state

func _card_in_hand(player: PlayerState, instance_id: String) -> CardInstance:
	for card in player.hand:
		if card.instance_id == instance_id:
			return card
	return null

func _both_mulligans_confirmed() -> bool:
	for player_id in state.players:
		var player: PlayerState = state.players[player_id]
		if not player.mulligan_confirmed:
			return false
	return true

func _other_player_id(player_id: String) -> String:
	return "opponent" if player_id == "player" else "player"

func _emit(events: Array, event_type: String, payload: Dictionary) -> void:
	state.sequence += 1
	var event := {"type": event_type, "sequence": state.sequence}
	for key in payload:
		event[key] = payload[key]
	events.append(event)

func _accept(action: GameAction, events: Array = []) -> ActionResult:
	return ActionResult.accept(events, state.snapshot_for(action.actor_id))

func _reject(action: GameAction, code: String, message: String) -> ActionResult:
	return ActionResult.reject(code, message, state.snapshot_for(action.actor_id))
