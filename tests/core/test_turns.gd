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
	var overdraw_events = controller.debug_draw(active.id)
	t.assert_eq(active.hand.size(), GameConstants.MAX_HAND_SIZE, "hand remains capped")
	t.assert_eq(active.discard.size(), before_discard + 1, "overdraw discarded")
	t.assert_eq(_event_types(overdraw_events), ["card_overdrawn"], "overflow has a dedicated draw outcome event")
	active.deck.clear()
	var first_fatigue_events = controller.debug_draw(active.id)
	var second_fatigue_events = controller.debug_draw(active.id)
	t.assert_eq(active.headquarters.current_defense, 17, "fatigue deals one then two")
	t.assert_eq(active.fatigue, 3, "fatigue increases after each failed draw")
	t.assert_eq(_event_types(first_fatigue_events), ["fatigue_damage"], "empty deck has fatigue draw outcome event")
	t.assert_eq(first_fatigue_events[0].damage, 1, "first fatigue event reports one damage")
	t.assert_eq(second_fatigue_events[0].damage, 2, "second fatigue event reports two damage")

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

static func _state_digest(controller: MatchController) -> Dictionary:
	var state = controller.state
	var players := {}
	for player_id in state.players:
		var player = state.players[player_id]
		players[player_id] = {
			"credit_slots": player.credit_slots,
			"credit": player.credit,
			"fatigue": player.fatigue,
			"hq_defense": player.headquarters.current_defense,
			"deck": _card_ids(player.deck),
			"hand": _card_ids(player.hand),
			"discard": _card_ids(player.discard),
		}
	return {
		"active_player_id": state.active_player_id,
		"turn": state.turn,
		"phase": state.phase,
		"sequence": state.sequence,
		"rng_state": state.rng_state,
		"players": players,
	}

static func _card_ids(cards: Array) -> Array[String]:
	var ids: Array[String] = []
	for card in cards:
		ids.append(card.instance_id)
	return ids
