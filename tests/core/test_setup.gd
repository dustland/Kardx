extends RefCounted

const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const ActionResult = preload("res://scripts/core/action_result.gd")
const DeckValidator = preload("res://scripts/core/deck_validator.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")

static func run(t) -> void:
	_test_deck_validation(t)
	_test_seeded_setup(t)
	_test_mulligan(t)

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
	t.assert_eq(state.players[state.starting_player_id].hand.size(), 4, "starter gets four")
	var other_id := "opponent" if state.starting_player_id == "player" else "player"
	t.assert_eq(state.players[other_id].hand.size(), 5, "other player gets five")
	t.assert_eq(state.phase, "mulligan", "starting hands enter mulligan phase")
	t.assert_eq(controller.initial_deck_ids.player, fixture.player_deck, "player deck IDs retained for replay")
	t.assert_eq(controller.initial_deck_ids.opponent, fixture.enemy_deck, "opponent deck IDs retained for replay")

	var repeat: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 88)
	repeat.submit_action(GameAction.create("start_match", "system"))
	t.assert_eq(_hand_ids(state.players.player.hand), _hand_ids(repeat.state.players.player.hand), "fixed seed has identical player opening hand")
	t.assert_eq(_hand_ids(state.players.opponent.hand), _hand_ids(repeat.state.players.opponent.hand), "fixed seed has identical opponent opening hand")

static func _test_mulligan(t) -> void:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 88)
	controller.submit_action(GameAction.create("start_match", "system"))
	var player = controller.state.players.player
	var selected_id: String = player.hand[0].instance_id
	var result: ActionResult = controller.submit_action(GameAction.create("mulligan", "player", "", [selected_id]))
	t.assert_true(result.accepted, "mulligan accepts owned opening card")
	t.assert_eq(_event_types(result.events), ["card_returned", "deck_shuffled", "card_drawn"], "mulligan events are ordered")
	t.assert_true(player.mulligan_used, "mulligan records single use")
	t.assert_eq(player.hand.size(), 4 if controller.state.starting_player_id == "player" else 5, "mulligan redraws replacements")
	t.assert_true(not controller.submit_action(GameAction.create("mulligan", "player", "", [selected_id])).accepted, "mulligan cannot repeat")

	controller.submit_action(GameAction.create("confirm_mulligan", "player"))
	t.assert_eq(controller.state.phase, "mulligan", "one confirmation keeps mulligan phase")
	controller.submit_action(GameAction.create("confirm_mulligan", "opponent"))
	t.assert_eq(controller.state.phase, "action", "both confirmations start action phase")

static func _hand_ids(hand: Array) -> Array[String]:
	var ids: Array[String] = []
	for card in hand:
		ids.append(card.instance_id)
	return ids

static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(event.type)
	return types
