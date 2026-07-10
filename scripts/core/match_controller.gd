class_name MatchController
extends RefCounted

const ActionResult = preload("res://scripts/core/action_result.gd")
const CardInstance = preload("res://scripts/core/card_instance.gd")
const CombatRules = preload("res://scripts/core/combat_rules.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchState = preload("res://scripts/core/match_state.gd")
const PlayerState = preload("res://scripts/core/player_state.gd")

var state: MatchState
var initial_deck_ids: Dictionary
var _definitions: Dictionary
var _rng := RandomNumberGenerator.new()
var _debug_trigger_hook: Callable
var _debug_modifier_expiry_hook: Callable

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
		"deploy_unit":
			return _deploy_unit(action)
		"move_unit":
			return _move_unit(action)
		"attack_unit":
			return _attack_unit(action)
		"attack_hq":
			return _attack_hq(action)
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
	if _is_terminal():
		return _accept(action, events)
	_expire_temporary_modifiers(player)
	if _is_terminal():
		return _accept(action, events)
	next_player.deactivate_countermeasures()
	_emit(events, "turn_ended", {"player_id": player.id, "turn": state.turn})
	state.active_player_id = next_player_id
	_start_turn(next_player, events)
	return _accept(action, events)

func _deploy_unit(action: GameAction) -> ActionResult:
	var context := _validate_turn_action(action)
	if not context.valid:
		return _reject_rule(action, context)
	var player: PlayerState = state.players[action.actor_id]
	var card: CardInstance = _card_in_hand(player, action.source_id)
	if card == null:
		return _reject(action, "source_not_in_hand", "Unit is not in hand")
	if card.category != "Unit":
		return _reject(action, "invalid_card_type", "Only Units deploy to the Support Line")
	if _occupied_count(player.support_line) >= GameConstants.SUPPORT_UNIT_SLOTS:
		return _reject(action, "support_line_full", "Support Line is full")
	if not action.payload.has("support_slot") or typeof(action.payload.support_slot) != TYPE_INT:
		return _reject(action, "invalid_slot", "Support slot is invalid")
	var slot: int = action.payload.support_slot
	if slot < 0 or slot >= GameConstants.SUPPORT_UNIT_SLOTS:
		return _reject(action, "invalid_slot", "Support slot is invalid")
	if player.support_line[slot] != null:
		return _reject(action, "slot_occupied", "Support slot is occupied")
	if player.credit < card.deployment_cost:
		return _reject(action, "insufficient_credit", "Not enough Credit")

	var events: Array = []
	_spend_credit(player, card.deployment_cost, "deployment", card.instance_id, events)
	player.hand.erase(card)
	player.support_line[slot] = card
	card.zone = "support_line"
	card.slot = slot
	card.deployed_turn = state.turn
	card.reset_operation_state()
	_emit(events, "card_deployed", {
		"player_id": player.id,
		"instance_id": card.instance_id,
		"zone": "support_line",
		"slot": slot,
	})
	return _accept(action, events)

func _move_unit(action: GameAction) -> ActionResult:
	var context := _validate_turn_action(action)
	if not context.valid:
		return _reject_rule(action, context)
	var unit: CardInstance = CombatRules.find_card(state, action.source_id)
	if unit == null:
		return _reject(action, "card_not_found", "Unit was not found")
	if unit.owner_id != action.actor_id:
		return _reject(action, "not_owner", "Unit belongs to another player")
	if str(action.payload.get("zone", "")) != "frontline":
		return _reject(action, "invalid_destination", "Units only move to the Frontline")
	if not action.payload.has("slot") or typeof(action.payload.slot) != TYPE_INT:
		return _reject(action, "invalid_slot", "Frontline slot is invalid")
	var slot: int = action.payload.slot
	var validation := CombatRules.validate_move_to_frontline(state, unit.instance_id, slot)
	if not validation.valid:
		return _reject_rule(action, validation)

	var events: Array = []
	var player: PlayerState = state.players[action.actor_id]
	_spend_credit(player, unit.operation_cost, "operation", unit.instance_id, events)
	unit.operations_used += 1
	unit.smokescreen_revealed = true
	unit.operation_chain = CardInstance.OperationChain.TANK_ADVANCE \
		if CombatRules.opens_tank_advance(unit) else CardInstance.OperationChain.NONE
	var from_slot := unit.slot
	player.support_line[from_slot] = null
	state.frontline[slot] = unit
	unit.zone = "frontline"
	unit.slot = slot
	_emit(events, "unit_moved", {
		"player_id": player.id,
		"instance_id": unit.instance_id,
		"from_zone": "support_line",
		"from_slot": from_slot,
		"to_zone": "frontline",
		"to_slot": slot,
	})
	if state.frontline_controller_id != player.id:
		var previous_controller := state.frontline_controller_id
		state.frontline_controller_id = player.id
		_emit(events, "frontline_changed", {
			"previous_controller_id": previous_controller,
			"controller_id": player.id,
		})
	return _accept(action, events)

func _attack_unit(action: GameAction) -> ActionResult:
	var context := _validate_turn_action(action)
	if not context.valid:
		return _reject_rule(action, context)
	if action.target_ids.size() != 1:
		return _reject(action, "invalid_target", "Attack requires one unit target")
	var attacker: CardInstance = CombatRules.find_card(state, action.source_id)
	if attacker == null:
		return _reject(action, "card_not_found", "Attacker was not found")
	if attacker.owner_id != action.actor_id:
		return _reject(action, "not_owner", "Attacker belongs to another player")
	var defender_id := str(action.target_ids[0])
	var validation := CombatRules.validate_unit_attack(state, attacker.instance_id, defender_id)
	if not validation.valid:
		return _reject_rule(action, validation)
	var defender: CardInstance = CombatRules.find_card(state, defender_id)

	var events: Array = []
	_reserve_attack_operation(attacker, events)
	_emit(events, "attack_started", {
		"attacker_id": attacker.instance_id,
		"defender_id": defender.instance_id,
		"target_type": "unit",
	})
	var ambush := CombatRules.receives_counterattack(attacker) and CombatRules.is_ambush(defender)
	if ambush:
		_deal_combat_damage(defender, attacker, defender.current_attack, "ambush", events)
		_destroy_if_dead(attacker, events)
		if attacker.current_defense <= 0:
			_update_frontline_control(events)
			return _accept(action, events)

	_deal_combat_damage(attacker, defender, attacker.current_attack, "attack", events)
	_destroy_if_dead(defender, events)
	if defender.current_defense > 0 and not ambush and CombatRules.receives_counterattack(attacker):
		_deal_combat_damage(defender, attacker, defender.current_attack, "counterattack", events)
		_destroy_if_dead(attacker, events)
	_update_frontline_control(events)
	return _accept(action, events)

func _attack_hq(action: GameAction) -> ActionResult:
	var context := _validate_turn_action(action)
	if not context.valid:
		return _reject_rule(action, context)
	var attacker: CardInstance = CombatRules.find_card(state, action.source_id)
	if attacker == null:
		return _reject(action, "card_not_found", "Attacker was not found")
	if attacker.owner_id != action.actor_id:
		return _reject(action, "not_owner", "Attacker belongs to another player")
	var defender_player_id := str(action.payload.get("target_player_id", ""))
	if defender_player_id.is_empty() and action.target_ids.size() == 1:
		var headquarters: CardInstance = CombatRules.find_card(state, str(action.target_ids[0]))
		if headquarters != null and headquarters.category == "Headquarters":
			defender_player_id = headquarters.owner_id
	var validation := CombatRules.validate_hq_attack(state, attacker.instance_id, defender_player_id)
	if not validation.valid:
		return _reject_rule(action, validation)
	var defender: PlayerState = state.players[defender_player_id]

	var events: Array = []
	_reserve_attack_operation(attacker, events)
	_emit(events, "attack_started", {
		"attacker_id": attacker.instance_id,
		"defender_id": defender.headquarters.instance_id,
		"target_type": "headquarters",
	})
	_deal_combat_damage(attacker, defender.headquarters, attacker.current_attack, "attack", events)
	_check_headquarters_death(defender)
	if _is_terminal():
		_emit(events, "match_ended", {
			"winner_id": state.winner_id,
			"loser_id": defender.id,
		})
	return _accept(action, events)

func _draw_opening_cards(player: PlayerState, count: int, events: Array) -> void:
	for index in range(count):
		_draw_card(player, events)
		if _is_terminal():
			return

func _draw_card(player: PlayerState, events: Array) -> void:
	if _is_terminal():
		return
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
	if _is_terminal():
		return
	state.turn += 1
	player.turns_started += 1
	_emit(events, "turn_started", {"player_id": player.id, "turn": state.turn})
	var previous_credit_slots := player.credit_slots
	player.credit_slots = mini(GameConstants.MAX_CREDITS, player.credit_slots + 1)
	if player.credit_slots != previous_credit_slots:
		_emit(events, "credit_slots_changed", {"player_id": player.id, "credit_slots": player.credit_slots})
	player.credit = player.credit_slots
	player.reset_operations()
	for card in player.support_line:
		if card != null:
			card.reset_operation_state()
	for card in state.frontline:
		if card != null and card.owner_id == player.id:
			card.reset_operation_state()
	_emit(events, "credit_refilled", {"player_id": player.id, "credit": player.credit})
	_draw_card(player, events)
	if _is_terminal():
		return
	_resolve_trigger("turn_start", {"player_id": player.id}, events)

func debug_draw(player_id: String) -> Array:
	if not state.players.has(player_id):
		return []
	var events: Array = []
	_draw_card(state.players[player_id], events)
	return events

func debug_set_trigger_hook(hook: Callable) -> void:
	_debug_trigger_hook = hook

func debug_set_modifier_expiry_hook(hook: Callable) -> void:
	_debug_modifier_expiry_hook = hook

func _validate_turn_action(action: GameAction) -> Dictionary:
	if _is_terminal():
		return {"valid": false, "code": "match_complete"}
	if state.phase != "action":
		return {"valid": false, "code": "invalid_phase"}
	if not state.players.has(action.actor_id):
		return {"valid": false, "code": "invalid_actor"}
	if action.actor_id != state.active_player_id:
		return {"valid": false, "code": "not_active_player"}
	return {"valid": true}

func _reserve_attack_operation(attacker: CardInstance, events: Array) -> void:
	attacker.smokescreen_revealed = true
	if CombatRules.attack_uses_tank_chain(attacker):
		attacker.operation_chain = CardInstance.OperationChain.NONE
		return
	_spend_credit(
		state.players[attacker.owner_id],
		attacker.operation_cost,
		"operation",
		attacker.instance_id,
		events
	)
	attacker.operations_used += 1
	attacker.operation_chain = CardInstance.OperationChain.NONE

func _spend_credit(player: PlayerState, amount: int, reason: String, source_id: String, events: Array) -> void:
	player.credit -= amount
	if amount <= 0:
		return
	_emit(events, "credit_spent", {
		"player_id": player.id,
		"source_id": source_id,
		"amount": amount,
		"reason": reason,
		"credit": player.credit,
	})

func _deal_combat_damage(
	source: CardInstance,
	target: CardInstance,
	base_damage: int,
	damage_type: String,
	events: Array
) -> void:
	var reduction := 1 if CombatRules.reduces_damage(target) else 0
	var damage := maxi(0, base_damage - reduction)
	target.current_defense = maxi(0, target.current_defense - damage)
	_emit(events, "damage_dealt", {
		"source_id": source.instance_id,
		"target_id": target.instance_id,
		"damage": damage,
		"damage_type": damage_type,
		"remaining_defense": target.current_defense,
	})

func _destroy_if_dead(card: CardInstance, events: Array) -> void:
	if card.current_defense > 0 or card.zone == "discard" or card.category != "Unit":
		return
	var player: PlayerState = state.players[card.owner_id]
	if card.zone == "support_line":
		if card.slot >= 0 and card.slot < player.support_line.size() and player.support_line[card.slot] == card:
			player.support_line[card.slot] = null
	elif card.zone == "frontline":
		if card.slot >= 0 and card.slot < state.frontline.size() and state.frontline[card.slot] == card:
			state.frontline[card.slot] = null
	card.zone = "discard"
	card.slot = -1
	card.operation_chain = CardInstance.OperationChain.NONE
	player.discard.append(card)
	_emit(events, "card_destroyed", {
		"player_id": player.id,
		"instance_id": card.instance_id,
	})

func _update_frontline_control(events: Array) -> void:
	var controller_id := ""
	for card in state.frontline:
		if card != null:
			controller_id = card.owner_id
			break
	if controller_id == state.frontline_controller_id:
		return
	var previous_controller := state.frontline_controller_id
	state.frontline_controller_id = controller_id
	_emit(events, "frontline_changed", {
		"previous_controller_id": previous_controller,
		"controller_id": controller_id,
	})

func _occupied_count(cards: Array) -> int:
	var count := 0
	for card in cards:
		if card != null:
			count += 1
	return count

func _reject_rule(action: GameAction, validation: Dictionary) -> ActionResult:
	var code := str(validation.get("code", "invalid_action"))
	return _reject(action, code, code.replace("_", " ").capitalize())

func _resolve_trigger(trigger: String, context: Dictionary, events: Array) -> void:
	if _is_terminal():
		return
	if _debug_trigger_hook.is_valid():
		_debug_trigger_hook.call(trigger, context, events)

func _expire_temporary_modifiers(player: PlayerState) -> void:
	if _is_terminal():
		return
	if _debug_modifier_expiry_hook.is_valid():
		_debug_modifier_expiry_hook.call(player.id)

func _check_headquarters_death(player: PlayerState) -> void:
	if player.headquarters.current_defense > 0 or _is_terminal():
		return
	state.winner_id = _other_player_id(player.id)
	state.phase = "complete"

func _is_terminal() -> bool:
	return state.phase == "complete" or not state.winner_id.is_empty()

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
