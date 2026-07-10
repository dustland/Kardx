class_name MatchController
extends RefCounted

const ActionResult = preload("res://scripts/core/action_result.gd")
const CardInstance = preload("res://scripts/core/card_instance.gd")
const CombatRules = preload("res://scripts/core/combat_rules.gd")
const EffectEngine = preload("res://scripts/core/effect_engine.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchState = preload("res://scripts/core/match_state.gd")
const PlayerState = preload("res://scripts/core/player_state.gd")

var state: MatchState
var initial_deck_ids: Dictionary
var _definitions: Dictionary
var _rng := RandomNumberGenerator.new()
var _effect_engine: EffectEngine
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
	controller._effect_engine = EffectEngine.create(controller.state, controller._definitions, controller._rng)
	controller._shuffle_deck(player.deck)
	controller._shuffle_deck(opponent.deck)
	return controller

func submit_action(action: GameAction) -> ActionResult:
	if action.expected_sequence > 0 and action.expected_sequence != state.sequence:
		return _reject(action, "stale_action", "State changed")
	var transaction := _capture_transaction()
	_effect_engine.begin_action()
	var result := _dispatch_action(action)
	var resolution := _effect_engine.finish_action()
	if not result.accepted or not resolution.valid:
		_restore_transaction(transaction)
		if not resolution.valid:
			return _reject_rule(action, resolution)
		return _reject(action, result.reason_code, result.message)
	return result

func _dispatch_action(action: GameAction) -> ActionResult:
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
		"play_order":
			return _play_order(action)
		"toggle_countermeasure":
			return _toggle_countermeasure(action)
		"activate_ability":
			return _activate_ability(action)
		_:
			return _reject(action, "unknown_action", "Unknown action")

func _capture_transaction() -> Dictionary:
	var player_snapshots := {}
	var card_snapshots: Array[Dictionary] = []
	var seen_cards := {}
	for player_id in state.players:
		var player: PlayerState = state.players[player_id]
		player_snapshots[player_id] = {
			"deck": player.deck.duplicate(),
			"hand": player.hand.duplicate(),
			"support_line": player.support_line.duplicate(),
			"discard": player.discard.duplicate(),
			"active_countermeasures": player.active_countermeasures.duplicate(),
			"credit_slots": player.credit_slots,
			"credit": player.credit,
			"fatigue": player.fatigue,
			"turns_started": player.turns_started,
			"mulligan_used": player.mulligan_used,
			"mulligan_confirmed": player.mulligan_confirmed,
			"max_hand_size": player.max_hand_size,
		}
		_capture_card(player.headquarters, card_snapshots, seen_cards)
		for collection in [player.deck, player.hand, player.support_line, player.discard, state.frontline]:
			for card in collection:
				_capture_card(card, card_snapshots, seen_cards)
	return {
		"active_player_id": state.active_player_id,
		"starting_player_id": state.starting_player_id,
		"turn": state.turn,
		"frontline": state.frontline.duplicate(),
		"frontline_controller_id": state.frontline_controller_id,
		"winner_id": state.winner_id,
		"rng_state": state.rng_state,
		"rng_internal_state": _rng.state,
		"sequence": state.sequence,
		"phase": state.phase,
		"players": player_snapshots,
		"cards": card_snapshots,
	}

func _capture_card(card, snapshots: Array[Dictionary], seen: Dictionary) -> void:
	if card == null or seen.has(card):
		return
	seen[card] = true
	snapshots.append({
		"card": card,
		"current_attack": card.current_attack,
		"current_defense": card.current_defense,
		"zone": card.zone,
		"slot": card.slot,
		"operations_used": card.operations_used,
		"operation_chain": card.operation_chain,
		"smokescreen_revealed": card.smokescreen_revealed,
		"deployed_turn": card.deployed_turn,
		"modifiers": card.modifiers.duplicate(true),
		"statuses": card.statuses.duplicate(true),
		"face_down": card.face_down,
		"countermeasure_active": card.countermeasure_active,
		"countermeasure_activation_cost": card.countermeasure_activation_cost,
	})

func _restore_transaction(transaction: Dictionary) -> void:
	state.active_player_id = transaction.active_player_id
	state.starting_player_id = transaction.starting_player_id
	state.turn = transaction.turn
	state.frontline = transaction.frontline.duplicate()
	state.frontline_controller_id = transaction.frontline_controller_id
	state.winner_id = transaction.winner_id
	state.rng_state = transaction.rng_state
	state.sequence = transaction.sequence
	state.phase = transaction.phase
	_rng.state = transaction.rng_internal_state
	for player_id in transaction.players:
		var player: PlayerState = state.players[player_id]
		var snapshot: Dictionary = transaction.players[player_id]
		player.deck = snapshot.deck.duplicate()
		player.hand = snapshot.hand.duplicate()
		player.support_line = snapshot.support_line.duplicate()
		player.discard = snapshot.discard.duplicate()
		player.active_countermeasures = snapshot.active_countermeasures.duplicate()
		player.credit_slots = snapshot.credit_slots
		player.credit = snapshot.credit
		player.fatigue = snapshot.fatigue
		player.turns_started = snapshot.turns_started
		player.mulligan_used = snapshot.mulligan_used
		player.mulligan_confirmed = snapshot.mulligan_confirmed
		player.max_hand_size = snapshot.max_hand_size
	for snapshot in transaction.cards:
		var card = snapshot.card
		card.current_attack = snapshot.current_attack
		card.current_defense = snapshot.current_defense
		card.zone = snapshot.zone
		card.slot = snapshot.slot
		card.operations_used = snapshot.operations_used
		card.operation_chain = snapshot.operation_chain
		card.smokescreen_revealed = snapshot.smokescreen_revealed
		card.deployed_turn = snapshot.deployed_turn
		card.modifiers = snapshot.modifiers.duplicate(true)
		card.statuses = snapshot.statuses.duplicate(true)
		card.face_down = snapshot.face_down
		card.countermeasure_active = snapshot.countermeasure_active
		card.countermeasure_activation_cost = snapshot.countermeasure_activation_cost
	_effect_engine.reset_after_rollback()

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
	var deploy_validation := _effect_engine.validate_trigger("deploy", {
		"source_id": card.instance_id,
		"actor_id": player.id,
		"target_ids": [],
	})
	if not deploy_validation.valid:
		return _reject_rule(action, deploy_validation)

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
	_resolve_trigger("deploy", {"source_id": card.instance_id, "actor_id": player.id, "target_ids": []}, events)
	if not _effect_engine.last_resolution.valid:
		return _reject_rule(action, _effect_engine.last_resolution)
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

func _play_order(action: GameAction) -> ActionResult:
	var validation := _validate_turn_action(action)
	if not validation.valid:
		return _reject_rule(action, validation)
	var player: PlayerState = state.players[action.actor_id]
	var order: CardInstance = _card_in_hand(player, action.source_id)
	if order == null:
		return _reject(action, "source_not_in_hand", "Order is not in hand")
	if order.category != "Order":
		return _reject(action, "invalid_card_type", "Only Orders can be played")
	if player.credit < order.deployment_cost:
		return _reject(action, "insufficient_credit", "Not enough Credit")
	var context := {
		"source_id": order.instance_id,
		"actor_id": player.id,
		"target_ids": action.target_ids.duplicate(),
	}
	var effect_validation := _effect_engine.validate_trigger("play_order", context)
	if not effect_validation.valid:
		return _reject_rule(action, effect_validation)
	var pending_event := {
		"type": "order_played",
		"order_id": order.instance_id,
		"actor_id": player.id,
		"target_ids": action.target_ids.duplicate(),
		"cancelled": false,
	}
	var counter_context := {
		"source_id": order.instance_id,
		"actor_id": player.id,
		"target_ids": action.target_ids.duplicate(),
		"event": pending_event,
	}
	var counter_validation := _effect_engine.validate_trigger("order_played", counter_context)
	if not counter_validation.valid:
		return _reject_rule(action, counter_validation)

	var events: Array = []
	_spend_credit(player, order.deployment_cost, "order", order.instance_id, events)
	_emit(events, "order_played", pending_event)
	var counter_events := _effect_engine.resolve_trigger("order_played", counter_context)
	events.append_array(counter_events)
	if not _effect_engine.last_resolution.valid:
		return _reject_rule(action, _effect_engine.last_resolution)
	if not bool(pending_event.get("cancelled", false)):
		events.append_array(_effect_engine.resolve_trigger("play_order", context))
		if not _effect_engine.last_resolution.valid:
			return _reject_rule(action, _effect_engine.last_resolution)
	player.hand.erase(order)
	order.zone = "discard"
	order.slot = -1
	player.discard.append(order)
	_emit(events, "card_discarded", {"player_id": player.id, "instance_id": order.instance_id})
	return _accept(action, events)


func _toggle_countermeasure(action: GameAction) -> ActionResult:
	var validation := _validate_turn_action(action)
	if not validation.valid:
		return _reject_rule(action, validation)
	var player: PlayerState = state.players[action.actor_id]
	var countermeasure: CardInstance = _card_in_hand(player, action.source_id)
	if countermeasure == null:
		return _reject(action, "source_not_in_hand", "Countermeasure is not in hand")
	if countermeasure.category != "Countermeasure":
		return _reject(action, "invalid_card_type", "Only Countermeasures can be activated")
	var events: Array = []
	if countermeasure.countermeasure_active:
		countermeasure.countermeasure_active = false
		player.active_countermeasures.erase(countermeasure)
		player.credit += countermeasure.countermeasure_activation_cost
		_emit(events, "credit_refunded", {
			"player_id": player.id,
			"source_id": countermeasure.instance_id,
			"amount": countermeasure.countermeasure_activation_cost,
			"credit": player.credit,
		})
		countermeasure.countermeasure_activation_cost = 0
		_emit(events, "countermeasure_deactivated", {"player_id": player.id, "instance_id": countermeasure.instance_id})
		return _accept(action, events)
	if player.credit < countermeasure.deployment_cost:
		return _reject(action, "insufficient_credit", "Not enough Credit")
	countermeasure.countermeasure_activation_cost = countermeasure.deployment_cost
	_spend_credit(player, countermeasure.countermeasure_activation_cost, "countermeasure", countermeasure.instance_id, events)
	countermeasure.countermeasure_active = true
	countermeasure.face_down = true
	player.active_countermeasures.append(countermeasure)
	_emit(events, "countermeasure_activated", {"player_id": player.id, "instance_id": countermeasure.instance_id})
	return _accept(action, events)


func _activate_ability(action: GameAction) -> ActionResult:
	var validation := _validate_turn_action(action)
	if not validation.valid:
		return _reject_rule(action, validation)
	var source: CardInstance = CombatRules.find_card(state, action.source_id)
	if source == null:
		return _reject(action, "card_not_found", "Ability source was not found")
	if source.owner_id != action.actor_id:
		return _reject(action, "not_owner", "Ability source belongs to another player")
	var ability_id := str(action.payload.get("ability_id", ""))
	var ability: Dictionary = {}
	for candidate_value in source.abilities:
		var candidate: Dictionary = candidate_value
		if str(candidate.get("id", "")) == ability_id and str(candidate.get("trigger", "")) == "manual":
			ability = candidate
			break
	if ability.is_empty():
		return _reject(action, "ability_not_found", "Manual ability was not found")
	var allowed_zones: Array = ability.get("allowed_zones", ["support_line", "frontline"])
	if not (source.zone in allowed_zones):
		return _reject(action, "invalid_origin", "Ability source is not on the battlefield")
	var player: PlayerState = state.players[action.actor_id]
	var credit_cost := int(ability.get("credit_cost", 0))
	if player.credit < credit_cost:
		return _reject(action, "insufficient_credit", "Not enough Credit")
	var context := {
		"source_id": source.instance_id,
		"actor_id": player.id,
		"target_ids": action.target_ids.duplicate(),
		"ability_id": ability_id,
	}
	var effect_validation := _effect_engine.validate_trigger("manual", context)
	if not effect_validation.valid:
		return _reject_rule(action, effect_validation)
	var events: Array = []
	_spend_credit(player, credit_cost, "ability", source.instance_id, events)
	events.append_array(_effect_engine.resolve_trigger("manual", context))
	if not _effect_engine.last_resolution.valid:
		return _reject_rule(action, _effect_engine.last_resolution)
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
	var pending_event := {"type": "attack", "actor_id": action.actor_id, "target_ids": action.target_ids.duplicate(), "cancelled": false}
	var reaction_context := {"source_id": attacker.instance_id, "actor_id": action.actor_id, "target_ids": action.target_ids.duplicate(), "event": pending_event}
	var reaction_validation := _effect_engine.validate_trigger("attack", reaction_context)
	if not reaction_validation.valid:
		return _reject_rule(action, reaction_validation)

	var events: Array = []
	_resolve_trigger("attack", reaction_context, events)
	if not _effect_engine.last_resolution.valid:
		return _reject_rule(action, _effect_engine.last_resolution)
	if bool(pending_event.get("cancelled", false)):
		return _accept(action, events)
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

	var retaliation_damage := defender.current_attack
	var resolves_retaliation := not ambush and CombatRules.receives_counterattack(attacker)
	_deal_combat_damage(attacker, defender, attacker.current_attack, "attack", events)
	if resolves_retaliation:
		_deal_combat_damage(defender, attacker, retaliation_damage, "counterattack", events)
	_destroy_if_dead(defender, events)
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
	if action.target_ids.size() > 1:
		return _reject(action, "invalid_target", "Attack requires at most one Headquarters target")
	var defender_player_id := ""
	if action.target_ids.size() == 1:
		var headquarters: CardInstance = CombatRules.find_card(state, str(action.target_ids[0]))
		if headquarters == null or headquarters.category != "Headquarters":
			return _reject(action, "invalid_target", "Attack target must be a Headquarters")
		defender_player_id = headquarters.owner_id
		var payload_player_id := str(action.payload.get("target_player_id", ""))
		if not payload_player_id.is_empty() and payload_player_id != defender_player_id:
			return _reject(action, "invalid_target", "Headquarters target conflicts with payload")
	else:
		defender_player_id = str(action.payload.get("target_player_id", ""))
	var validation := CombatRules.validate_hq_attack(state, attacker.instance_id, defender_player_id)
	if not validation.valid:
		return _reject_rule(action, validation)
	var defender: PlayerState = state.players[defender_player_id]
	var pending_event := {
		"type": "attack",
		"actor_id": action.actor_id,
		"target_ids": [defender.headquarters.instance_id],
		"cancelled": false,
	}
	var reaction_context := {
		"source_id": attacker.instance_id,
		"actor_id": action.actor_id,
		"target_ids": [defender.headquarters.instance_id],
		"event": pending_event,
	}
	var reaction_validation := _effect_engine.validate_trigger("attack", reaction_context)
	if not reaction_validation.valid:
		return _reject_rule(action, reaction_validation)

	var events: Array = []
	_reserve_attack_operation(attacker, events)
	_emit(events, "attack_started", {
		"attacker_id": attacker.instance_id,
		"defender_id": defender.headquarters.instance_id,
		"target_type": "headquarters",
	})
	_resolve_trigger("attack", reaction_context, events)
	if not _effect_engine.last_resolution.valid:
		return _reject_rule(action, _effect_engine.last_resolution)
	if bool(pending_event.get("cancelled", false)):
		return _accept(action, events)
	_deal_combat_damage(attacker, defender.headquarters, attacker.current_attack, "attack", events)
	if defender.headquarters.current_defense <= 0:
		_resolve_trigger("hq_lethal", {
			"source_id": defender.headquarters.instance_id,
			"actor_id": action.actor_id,
			"target_ids": [defender.headquarters.instance_id],
		}, events)
		if not _effect_engine.last_resolution.valid:
			return _reject_rule(action, _effect_engine.last_resolution)
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
	_resolve_trigger("death", {"source_id": card.instance_id, "actor_id": card.owner_id, "target_ids": []}, events)

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
	if _effect_engine != null:
		events.append_array(_effect_engine.resolve_trigger(trigger, context))

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
