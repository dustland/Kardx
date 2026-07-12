extends RefCounted

const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const ActionResult = preload("res://scripts/core/action_result.gd")
const DeckValidator = preload("res://scripts/core/deck_validator.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const PlayerState = preload("res://scripts/core/player_state.gd")

static func run(t) -> void:
	_test_deck_validation(t)
	_test_seeded_setup(t)
	_test_empty_mulligan(t)
	_test_all_card_mulligan(t)
	_test_mulligan_rejections(t)

static func _test_deck_validation(t) -> void:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var valid: Dictionary = DeckValidator.validate(fixture.player_deck, fixture.definitions)
	t.assert_true(valid.valid, "40-card starter deck validates")
	t.assert_eq(valid.main_nation, "UnitedStates", "headquarters selects main nation")
	t.assert_eq(valid.ally_nation, "", "single-nation deck has no ally")

	var too_many: Array = fixture.player_deck.duplicate()
	too_many[4] = too_many[0]
	too_many[5] = too_many[0]
	var invalid: Dictionary = DeckValidator.validate(too_many, fixture.definitions)
	t.assert_true("copy_limit" in invalid.errors, "rarity copy limit enforced")

	var wrong_size: Array = fixture.player_deck.duplicate()
	wrong_size.pop_back()
	t.assert_true("deck_size" in DeckValidator.validate(wrong_size, fixture.definitions).errors, "deck size enforced")

	var no_headquarters: Array = fixture.player_deck.duplicate()
	no_headquarters.pop_back()
	no_headquarters.append("us-00")
	t.assert_true("headquarters_count" in DeckValidator.validate(no_headquarters, fixture.definitions).errors, "headquarters required")

	var multiple_headquarters: Array = fixture.player_deck.duplicate()
	multiple_headquarters[0] = "us-hq"
	t.assert_true("headquarters_count" in DeckValidator.validate(multiple_headquarters, fixture.definitions).errors, "only one headquarters allowed")

	var third_nation_definitions: Dictionary = fixture.definitions.duplicate(true)
	third_nation_definitions["ca-00"] = {
		"id": "ca-00", "title": "ca-00", "nation": "Canada", "category": "Unit", "rarity": "Standard"
	}
	var three_nations: Array = fixture.player_deck.duplicate()
	three_nations[0] = "su-00"
	three_nations[1] = "ca-00"
	t.assert_true("nation_limit" in DeckValidator.validate(three_nations, third_nation_definitions).errors, "two nations maximum")

	var too_many_allies: Array = fixture.player_deck.duplicate()
	for index in range(13):
		too_many_allies[index] = "su-%02d" % (index % 4)
	t.assert_true("ally_limit" in DeckValidator.validate(too_many_allies, fixture.definitions).errors, "ally count capped")

	var too_few_main: Array = fixture.player_deck.duplicate()
	for index in range(13):
		too_few_main[index] = "su-%02d" % (index % 4)
	t.assert_true("main_nation_minimum" in DeckValidator.validate(too_few_main, fixture.definitions).errors, "main nation minimum includes headquarters")

	var unknown_card: Array = fixture.player_deck.duplicate()
	unknown_card[0] = "missing-card"
	t.assert_true("unknown_id" in DeckValidator.validate(unknown_card, fixture.definitions).errors, "unknown card IDs rejected")

static func _test_seeded_setup(t) -> void:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 88)
	var started: ActionResult = controller.submit_action(GameAction.create("start_match", "system"))
	t.assert_true(started.accepted, "match starts")
	var state = controller.state
	t.assert_eq(state.starting_player_id, "opponent", "seed 88 selects opponent as starter")
	t.assert_eq(state.players[state.starting_player_id].hand.size(), 4, "starter gets four")
	var other_id := "opponent" if state.starting_player_id == "player" else "player"
	t.assert_eq(state.players[other_id].hand.size(), 5, "other player gets five")
	t.assert_eq(state.phase, "mulligan", "starting hands enter mulligan phase")
	t.assert_eq(state.players.player.headquarters.instance_id, "p-039", "player headquarters has stable ID")
	t.assert_eq(state.players.opponent.headquarters.instance_id, "o-039", "opponent headquarters has stable ID")
	t.assert_eq(_hand_ids(state.players.player.hand), ["p-029", "p-035", "p-009", "p-001", "p-005"], "seed 88 player opening hand is stable")
	t.assert_eq(_hand_ids(state.players.opponent.hand), ["o-033", "o-031", "o-008", "o-019"], "seed 88 opponent opening hand is stable")
	t.assert_eq(controller.initial_deck_ids.player, fixture.player_deck, "player deck IDs retained for replay")
	t.assert_eq(controller.initial_deck_ids.opponent, fixture.enemy_deck, "opponent deck IDs retained for replay")

	var repeat: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 88)
	repeat.submit_action(GameAction.create("start_match", "system"))
	t.assert_eq(_hand_ids(state.players.player.hand), _hand_ids(repeat.state.players.player.hand), "fixed seed has identical player opening hand")
	t.assert_eq(_hand_ids(state.players.opponent.hand), _hand_ids(repeat.state.players.opponent.hand), "fixed seed has identical opponent opening hand")

static func _test_empty_mulligan(t) -> void:
	var controller := _started_controller()
	var before := _mulligan_state(controller, "player")
	var result: ActionResult = controller.submit_action(GameAction.create("mulligan", "player"))
	t.assert_true(result.accepted, "empty mulligan is accepted")
	t.assert_eq(result.events, [], "empty mulligan emits no events")
	t.assert_eq(controller.state.sequence, before.sequence, "empty mulligan preserves sequence")
	t.assert_eq(controller.state.rng_state, before.rng_state, "empty mulligan preserves RNG")
	t.assert_eq(_hand_ids(controller.state.players.player.hand), before.hand_ids, "empty mulligan preserves hand")
	t.assert_eq(_deck_ids(controller.state.players.player.deck), before.deck_ids, "empty mulligan preserves deck")
	t.assert_true(controller.state.players.player.mulligan_used, "empty mulligan consumes the mulligan")
	t.assert_true(not controller.state.players.player.mulligan_confirmed, "empty mulligan does not confirm")
	t.assert_true(result.snapshot.players.player.has("deck_order"), "viewer sees own deck order")
	t.assert_true(not result.snapshot.players.opponent.has("deck_order"), "viewer cannot see opponent deck order")
	t.assert_true(result.snapshot.players.opponent.hand[0].hidden, "viewer cannot see opponent hand card")

	var confirmation: ActionResult = controller.submit_action(GameAction.create("confirm_mulligan", "player"))
	t.assert_true(confirmation.accepted, "confirmation remains separate after empty mulligan")
	t.assert_eq(controller.state.phase, "mulligan", "one confirmation keeps mulligan phase")

static func _test_all_card_mulligan(t) -> void:
	var controller := _started_controller()
	var player: PlayerState = controller.state.players.player
	var selected_ids := _hand_ids(player.hand)
	var expected_hand_size: int = player.hand.size()
	var result: ActionResult = controller.submit_action(GameAction.create("mulligan", "player", "", selected_ids))
	t.assert_true(result.accepted, "all opening cards can be mulliganed")
	t.assert_eq(_event_types(result.events), _mulligan_event_types(selected_ids.size()), "all-card mulligan events are ordered")
	t.assert_eq(player.hand.size(), expected_hand_size, "all-card mulligan redraws every card")
	t.assert_true(player.mulligan_used, "all-card mulligan records single use")
	var repeat_before := _mulligan_state(controller, "player")
	var repeat_result: ActionResult = controller.submit_action(GameAction.create("mulligan", "player", "", selected_ids))
	t.assert_true(not repeat_result.accepted, "used mulligan cannot repeat")
	t.assert_eq(repeat_result.reason_code, "mulligan_used", "used mulligan rejection has stable reason")
	_assert_mulligan_state_unchanged(t, controller, "player", repeat_before, "used mulligan rejection")

static func _test_mulligan_rejections(t) -> void:
	var confirmed_controller := _started_controller()
	confirmed_controller.submit_action(GameAction.create("confirm_mulligan", "player"))
	t.assert_true(not confirmed_controller.state.players.opponent.mulligan_confirmed, "opponent remains unconfirmed")
	var confirmed_before := _mulligan_state(confirmed_controller, "player")
	var confirmed_result: ActionResult = confirmed_controller.submit_action(GameAction.create("mulligan", "player"))
	t.assert_true(not confirmed_result.accepted, "confirmed player cannot mulligan while opponent is unconfirmed")
	t.assert_eq(confirmed_result.reason_code, "mulligan_confirmed", "confirmed mulligan rejection has stable reason")
	_assert_mulligan_state_unchanged(t, confirmed_controller, "player", confirmed_before, "confirmed mulligan rejection")

	var invalid_controller := _started_controller()
	var invalid_before := _mulligan_state(invalid_controller, "player")
	var invalid_result: ActionResult = invalid_controller.submit_action(GameAction.create("mulligan", "player", "", ["p-missing"]))
	t.assert_true(not invalid_result.accepted, "unknown mulligan card is rejected")
	t.assert_eq(invalid_result.reason_code, "invalid_selection", "unknown card rejection has stable reason")
	_assert_mulligan_state_unchanged(t, invalid_controller, "player", invalid_before, "unknown card rejection")

	var duplicate_controller := _started_controller()
	var selected_id: String = duplicate_controller.state.players.player.hand[0].instance_id
	var duplicate_before := _mulligan_state(duplicate_controller, "player")
	var duplicate_result: ActionResult = duplicate_controller.submit_action(GameAction.create("mulligan", "player", "", [selected_id, selected_id]))
	t.assert_true(not duplicate_result.accepted, "duplicate mulligan card is rejected")
	t.assert_eq(duplicate_result.reason_code, "invalid_selection", "duplicate card rejection has stable reason")
	_assert_mulligan_state_unchanged(t, duplicate_controller, "player", duplicate_before, "duplicate card rejection")

	var stale_controller := _started_controller()
	var stale_before := _mulligan_state(stale_controller, "player")
	var stale_result: ActionResult = stale_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, stale_controller.state.sequence + 1))
	t.assert_true(not stale_result.accepted, "stale mulligan action is rejected")
	t.assert_eq(stale_result.reason_code, "stale_action", "stale action rejection has stable reason")
	_assert_mulligan_state_unchanged(t, stale_controller, "player", stale_before, "stale action rejection")

static func _started_controller() -> MatchController:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 88)
	controller.submit_action(GameAction.create("start_match", "system"))
	return controller

static func _mulligan_state(controller: MatchController, player_id: String) -> Dictionary:
	var player: PlayerState = controller.state.players[player_id]
	return {
		"sequence": controller.state.sequence,
		"rng_state": controller.state.rng_state,
		"hand_ids": _hand_ids(player.hand),
		"deck_ids": _deck_ids(player.deck),
	}

static func _assert_mulligan_state_unchanged(t, controller: MatchController, player_id: String, before: Dictionary, message: String) -> void:
	var after := _mulligan_state(controller, player_id)
	t.assert_eq(after.sequence, before.sequence, "%s preserves sequence" % message)
	t.assert_eq(after.rng_state, before.rng_state, "%s preserves RNG" % message)
	t.assert_eq(after.hand_ids, before.hand_ids, "%s preserves hand" % message)
	t.assert_eq(after.deck_ids, before.deck_ids, "%s preserves deck" % message)

static func _hand_ids(hand: Array) -> Array[String]:
	var ids: Array[String] = []
	for card in hand:
		ids.append(card.instance_id)
	return ids

static func _deck_ids(deck: Array) -> Array[String]:
	var ids: Array[String] = []
	for card in deck:
		ids.append(card.instance_id)
	return ids

static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(event.type)
	return types

static func _mulligan_event_types(selection_count: int) -> Array[String]:
	var types: Array[String] = []
	for index in range(selection_count):
		types.append("card_returned")
	types.append("deck_shuffled")
	for index in range(selection_count):
		types.append("card_drawn")
	return types
