extends RefCounted

const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")

static func run(t) -> void:
	_test_first_turn_economy(t)
	_test_initial_turn_event_order(t)
	_test_end_turn_lifecycle(t)
	_test_countermeasures_expire_after_enemy_turn(t)
	_test_end_turn_rejection_is_atomic(t)
	_test_overdraw_and_fatigue(t)
	_test_fatigue_defeat_stops_turn_start_trigger(t)
	_test_credit_slots_cap_without_change_event(t)
	_test_terminal_end_trigger_stops_turn_transition(t)
	_test_terminal_modifier_expiry_stops_turn_transition(t)
	_test_seeded_turns_are_deterministic(t)

static func _test_first_turn_economy(t) -> void:
	var controller := _started_controller(301)
	var active = controller.state.players[controller.state.active_player_id]
	t.assert_eq(active.credit_slots, 1, "first own turn starts at one slot")
	t.assert_eq(active.credit, 1, "Credit refills")
	t.assert_eq(controller.state.turn, 1, "first turn increments turn counter")

static func _test_initial_turn_event_order(t) -> void:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 301)
	controller.submit_action(GameAction.create("start_match", "system"))
	controller.submit_action(GameAction.create("mulligan", "player"))
	controller.submit_action(GameAction.create("mulligan", "opponent"))
	controller.submit_action(GameAction.create("confirm_mulligan", "player"))
	var result = controller.submit_action(GameAction.create("confirm_mulligan", "opponent"))
	t.assert_eq(_event_types(result.events), ["turn_started", "credit_slots_changed", "credit_refilled", "card_drawn"], "initial turn emits lifecycle events in replay order")
	t.assert_eq(result.events[0].turn, 1, "initial turn event identifies first turn")

static func _test_end_turn_lifecycle(t) -> void:
	var controller := _started_controller(301)
	var first_player_id: String = controller.state.active_player_id
	var first_player = controller.state.players[first_player_id]
	var result = controller.submit_action(GameAction.create("end_turn", first_player_id, "", [], {}, controller.state.sequence))
	var next_player_id := "opponent" if first_player_id == "player" else "player"
	var next_player = controller.state.players[next_player_id]
	t.assert_true(result.accepted, "active player can end turn")
	t.assert_eq(_event_types(result.events), ["turn_ended", "turn_started", "credit_slots_changed", "credit_refilled", "card_drawn"], "end turn emits lifecycle events in replay order")
	t.assert_eq(result.events[0].player_id, first_player_id, "turn end event identifies ending player")
	t.assert_eq(result.events[1].player_id, next_player_id, "turn start event identifies next player")
	t.assert_eq(result.events[1].turn, 2, "turn start event identifies new turn")
	t.assert_eq(controller.state.active_player_id, next_player_id, "turn passes to opponent")
	t.assert_eq(controller.state.turn, 2, "turn counter increments")
	t.assert_eq(first_player.credit, 1, "ending turn does not change ending Credit")
	t.assert_eq(next_player.credit_slots, 1, "opponent starts at one slot")
	t.assert_eq(next_player.credit, 1, "opponent Credit refills")
	t.assert_eq(result.snapshot.players[first_player_id].hand.size(), first_player.hand.size(), "actor snapshot reveals ending actor hand")
	t.assert_true(result.snapshot.players[next_player_id].hand[0].hidden, "actor snapshot hides next player hand")

static func _test_end_turn_rejection_is_atomic(t) -> void:
	var controller := _started_controller(301)
	var active_player_id: String = controller.state.active_player_id
	var wrong_actor_id := "opponent" if active_player_id == "player" else "player"
	var active = controller.state.players[active_player_id]
	var support_card = active.hand.pop_back()
	support_card.zone = "support_line"
	support_card.operations_used = 3
	support_card.countermeasure_active = true
	active.support_line[0] = support_card
	active.active_countermeasures.append(support_card)
	var before := _state_digest(controller)
	var result = controller.submit_action(GameAction.create("end_turn", wrong_actor_id, "", [], {}, controller.state.sequence))
	t.assert_true(not result.accepted, "inactive player cannot end turn")
	t.assert_eq(result.reason_code, "not_active_player", "wrong actor has stable rejection code")
	t.assert_eq(_state_digest(controller), before, "wrong actor end turn is atomic")
	t.assert_true(result.snapshot.players[active_player_id].hand[0].hidden, "wrong actor snapshot hides active hand")

static func _test_countermeasures_expire_after_enemy_turn(t) -> void:
	var controller := _started_controller(301)
	var ending_player_id: String = controller.state.active_player_id
	var owner_id := "opponent" if ending_player_id == "player" else "player"
	var owner = controller.state.players[owner_id]
	var countermeasure = owner.hand[0]
	countermeasure.countermeasure_active = true
	owner.active_countermeasures.append(countermeasure)
	controller.submit_action(GameAction.create("end_turn", ending_player_id, "", [], {}, controller.state.sequence))
	t.assert_true(not countermeasure.countermeasure_active, "untriggered Countermeasure expires after enemy turn")
	t.assert_eq(owner.active_countermeasures, [], "expired Countermeasure leaves active list")

static func _test_overdraw_and_fatigue(t) -> void:
	var controller := _started_controller(301)
	var active = controller.state.players[controller.state.active_player_id]
	active.hand.resize(GameConstants.MAX_HAND_SIZE)
	var before_discard: int = active.discard.size()
	var expected_overdraw_card = active.deck.back()
	var overdraw_events = controller.debug_draw(active.id)
	t.assert_eq(active.hand.size(), GameConstants.MAX_HAND_SIZE, "hand remains capped")
	t.assert_eq(active.discard.size(), before_discard + 1, "overdraw discarded")
	t.assert_eq(_event_types(overdraw_events), ["card_overdrawn"], "overflow has a dedicated draw outcome event")
	t.assert_eq(overdraw_events[0].instance_id, expected_overdraw_card.instance_id, "overdraw event identifies the drawn card")
	t.assert_eq(active.discard.back().instance_id, expected_overdraw_card.instance_id, "exact drawn card moves to discard")
	t.assert_eq(active.discard.back().zone, "discard", "overdrawn card has discard zone")
	active.deck.clear()
	var first_fatigue_events = controller.debug_draw(active.id)
	var second_fatigue_events = controller.debug_draw(active.id)
	t.assert_eq(active.headquarters.current_defense, 17, "fatigue deals one then two")
	t.assert_eq(active.fatigue, 3, "fatigue increases after each failed draw")
	t.assert_eq(_event_types(first_fatigue_events), ["fatigue_damage"], "empty deck has fatigue draw outcome event")
	t.assert_eq(first_fatigue_events[0].damage, 1, "first fatigue event reports one damage")
	t.assert_eq(second_fatigue_events[0].damage, 2, "second fatigue event reports two damage")

static func _test_fatigue_defeat_stops_turn_start_trigger(t) -> void:
	var controller := _started_controller(301)
	var ending_player_id: String = controller.state.active_player_id
	var next_player_id := "opponent" if ending_player_id == "player" else "player"
	var next_player = controller.state.players[next_player_id]
	next_player.deck.clear()
	next_player.headquarters.current_defense = 1
	if not _require_debug_hook(t, controller, "debug_set_trigger_hook"):
		return
	controller.debug_set_trigger_hook(func(trigger: String, context: Dictionary, events: Array) -> void:
		if trigger == "turn_start":
			events.append({"type": "trigger_resolved", "player_id": str(context.player_id)})
	)
	var result = controller.submit_action(GameAction.create("end_turn", ending_player_id, "", [], {}, controller.state.sequence))
	t.assert_true(result.accepted, "ending player can hand over into fatigue defeat")
	t.assert_eq(next_player.headquarters.current_defense, 0, "fatigue clamps Headquarters defense at zero")
	t.assert_eq(controller.state.winner_id, ending_player_id, "fatigue defeat awards the opponent")
	t.assert_eq(controller.state.phase, "complete", "fatigue defeat completes the match")
	t.assert_eq(_event_types(result.events), ["turn_ended", "turn_started", "credit_slots_changed", "credit_refilled", "fatigue_damage"], "fatal fatigue stops later turn-start triggers")

static func _test_credit_slots_cap_without_change_event(t) -> void:
	var controller := _started_controller(301)
	var capped_player_id: String = controller.state.active_player_id
	var capped_player = controller.state.players[capped_player_id]
	capped_player.credit_slots = GameConstants.MAX_CREDITS - 1
	capped_player.credit = 0
	var first_result = controller.submit_action(GameAction.create("end_turn", capped_player_id, "", [], {}, controller.state.sequence))
	var other_player_id := "opponent" if capped_player_id == "player" else "player"
	var second_result = controller.submit_action(GameAction.create("end_turn", other_player_id, "", [], {}, controller.state.sequence))
	var third_result = controller.submit_action(GameAction.create("end_turn", capped_player_id, "", [], {}, controller.state.sequence))
	var fourth_result = controller.submit_action(GameAction.create("end_turn", other_player_id, "", [], {}, controller.state.sequence))
	t.assert_true(first_result.accepted and second_result.accepted and third_result.accepted and fourth_result.accepted, "both players can complete the turns around the Credit cap")
	t.assert_eq(capped_player.credit_slots, GameConstants.MAX_CREDITS, "Credit slots stay capped at twelve")
	t.assert_eq(capped_player.credit, GameConstants.MAX_CREDITS, "capped Credit refills to twelve")
	t.assert_true(_event_types(second_result.events).has("credit_slots_changed"), "Credit reaching twelve emits a slot change event")
	t.assert_true(not _event_types(fourth_result.events).has("credit_slots_changed"), "capped turn omits credit slot change event")

static func _test_terminal_end_trigger_stops_turn_transition(t) -> void:
	var controller := _started_controller(301)
	var ending_player_id: String = controller.state.active_player_id
	var before_active_player_id: String = controller.state.active_player_id
	var before_turn: int = controller.state.turn
	if not _require_debug_hook(t, controller, "debug_set_trigger_hook"):
		return
	controller.debug_set_trigger_hook(func(trigger: String, context: Dictionary, _events: Array) -> void:
		if trigger == "turn_end":
			controller.state.winner_id = "opponent" if str(context.player_id) == "player" else "player"
			controller.state.phase = "complete"
	)
	var result = controller.submit_action(GameAction.create("end_turn", ending_player_id, "", [], {}, controller.state.sequence))
	t.assert_true(result.accepted, "terminal end trigger accepts its initiating action")
	t.assert_eq(controller.state.phase, "complete", "end trigger can complete the match")
	t.assert_eq(controller.state.active_player_id, before_active_player_id, "terminal end trigger does not switch players")
	t.assert_eq(controller.state.turn, before_turn, "terminal end trigger does not start another turn")
	t.assert_eq(_event_types(result.events), [], "terminal end trigger emits no later lifecycle events")

static func _test_terminal_modifier_expiry_stops_turn_transition(t) -> void:
	var controller := _started_controller(301)
	var ending_player_id: String = controller.state.active_player_id
	var before_active_player_id: String = controller.state.active_player_id
	var before_turn: int = controller.state.turn
	if not _require_debug_hook(t, controller, "debug_set_modifier_expiry_hook"):
		return
	controller.debug_set_modifier_expiry_hook(func(_player_id: String) -> void:
		controller.state.winner_id = "opponent" if ending_player_id == "player" else "player"
		controller.state.phase = "complete"
	)
	var result = controller.submit_action(GameAction.create("end_turn", ending_player_id, "", [], {}, controller.state.sequence))
	t.assert_true(result.accepted, "terminal modifier expiry accepts its initiating action")
	t.assert_eq(controller.state.phase, "complete", "modifier expiry can complete the match")
	t.assert_eq(controller.state.active_player_id, before_active_player_id, "terminal modifier expiry does not switch players")
	t.assert_eq(controller.state.turn, before_turn, "terminal modifier expiry does not start another turn")
	t.assert_eq(_event_types(result.events), [], "terminal modifier expiry emits no later lifecycle events")

static func _test_seeded_turns_are_deterministic(t) -> void:
	var first := _started_controller(301)
	var second := _started_controller(301)
	var first_result = first.submit_action(GameAction.create("end_turn", first.state.active_player_id, "", [], {}, first.state.sequence))
	var second_result = second.submit_action(GameAction.create("end_turn", second.state.active_player_id, "", [], {}, second.state.sequence))
	t.assert_eq(first_result.events, second_result.events, "same seed has identical turn events")
	t.assert_eq(_state_digest(first), _state_digest(second), "same seed has identical turn state")

static func _started_controller(seed: int) -> MatchController:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, seed)
	controller.submit_action(GameAction.create("start_match", "system"))
	controller.submit_action(GameAction.create("mulligan", "player"))
	controller.submit_action(GameAction.create("mulligan", "opponent"))
	controller.submit_action(GameAction.create("confirm_mulligan", "player"))
	controller.submit_action(GameAction.create("confirm_mulligan", "opponent"))
	return controller

static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(event.type)
	return types

static func _require_debug_hook(t, controller: MatchController, method_name: String) -> bool:
	var available := controller.has_method(method_name)
	t.assert_true(available, "terminal lifecycle test hook is available: %s" % method_name)
	return available

static func _state_digest(controller: MatchController) -> Dictionary:
	var state = controller.state
	var players := {}
	for player_id in state.players:
		var player = state.players[player_id]
		players[player_id] = {
		"credit_slots": player.credit_slots,
		"credit": player.credit,
		"fatigue": player.fatigue,
		"turns_started": player.turns_started,
		"hq_defense": player.headquarters.current_defense,
		"mulligan_used": player.mulligan_used,
		"mulligan_confirmed": player.mulligan_confirmed,
		"deck": _card_states(player.deck),
		"hand": _card_states(player.hand),
		"support_line": _support_line_states(player.support_line),
		"discard": _card_states(player.discard),
		"active_countermeasures": _card_ids(player.active_countermeasures),
	}
	return {
		"active_player_id": state.active_player_id,
		"starting_player_id": state.starting_player_id,
		"turn": state.turn,
		"phase": state.phase,
		"winner_id": state.winner_id,
		"sequence": state.sequence,
		"rng_state": state.rng_state,
		"players": players,
	}

static func _card_ids(cards: Array) -> Array[String]:
	var ids: Array[String] = []
	for card in cards:
		ids.append(card.instance_id)
	return ids

static func _card_states(cards: Array) -> Array[Dictionary]:
	var states: Array[Dictionary] = []
	for card in cards:
		states.append({
			"instance_id": card.instance_id,
			"zone": card.zone,
			"operations_used": card.operations_used,
			"countermeasure_active": card.countermeasure_active,
			"modifiers": card.modifiers.duplicate(true),
		})
	return states

static func _support_line_states(cards: Array) -> Array:
	var states: Array = []
	for card in cards:
		if card == null:
			states.append(null)
			continue
		states.append({
			"instance_id": card.instance_id,
			"zone": card.zone,
			"operations_used": card.operations_used,
			"countermeasure_active": card.countermeasure_active,
			"modifiers": card.modifiers.duplicate(true),
		})
	return states
