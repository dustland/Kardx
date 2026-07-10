extends RefCounted

const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const ReplayLog = preload("res://scripts/core/replay_log.gd")


static func run(t) -> void:
	_test_replay_reproduces_setup_and_sequence_guard(t)
	_test_replay_aborts_on_tampered_sequence(t)
	_test_replay_aborts_on_tampered_action(t)
	_test_state_hash_is_canonical_for_dictionary_order(t)
	_test_state_hash_includes_rng_state(t)
	_test_rejected_action_preserves_state_hash_and_log(t)
	_test_broken_zone_invariant_aborts_match_once(t)
	_test_owner_slot_frontline_and_terminal_invariants_abort(t)
	_test_terminal_action_does_not_mutate_state(t)
	_test_seeded_matches_are_identical_and_diverge_for_other_seeds(t)


static func _test_replay_reproduces_setup_and_sequence_guard(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90210)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("mulligan", "opponent", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("confirm_mulligan", "player", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("confirm_mulligan", "opponent", "", [], {}, controller.state.sequence))

	var replayed = ReplayLog.from_dict(controller.replay_log.to_dict()).replay(fixture.definitions)
	t.assert_eq(replayed.state_hash(), controller.state_hash(), "replay reaches identical state")
	var stale = replayed.submit_action(GameAction.create("end_turn", replayed.state.active_player_id, "", [], {}, replayed.state.sequence - 1))
	t.assert_eq(stale.reason_code, "stale_action", "sequence guard survives replay")
	var opponent_snapshot: Dictionary = replayed.state.snapshot_for("opponent")
	t.assert_true(opponent_snapshot.players.player.hand[0].hidden, "replay keeps opponent hand snapshots hidden")


static func _test_replay_aborts_on_tampered_sequence(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90211)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	var tampered: Dictionary = controller.replay_log.to_dict()
	var entry: Dictionary = tampered.actions[0]
	entry.result_sequence = int(entry.result_sequence) + 1
	tampered.actions[0] = entry

	var replayed = ReplayLog.from_dict(tampered).replay(fixture.definitions)
	t.assert_eq(replayed.state.phase, "invalid", "tampered replay aborts as invalid")
	t.assert_eq(replayed.invalid_diagnostics.code, "replay_sequence_diverged", "replay exposes divergence diagnostics")


static func _test_replay_aborts_on_tampered_action(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90216)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	var tampered: Dictionary = controller.replay_log.to_dict()
	var entry: Dictionary = tampered.actions[0]
	var action: Dictionary = entry.action
	action.type = "end_turn"
	entry.action = action
	tampered.actions[0] = entry

	var replayed = ReplayLog.from_dict(tampered).replay(fixture.definitions)
	t.assert_eq(replayed.state.phase, "invalid", "tampered action aborts replay")
	t.assert_eq(replayed.invalid_diagnostics.code, "replay_action_rejected", "action tampering records rejection diagnostics")


static func _test_state_hash_is_canonical_for_dictionary_order(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var first := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90212)
	var second := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90212)
	first.state.players.player.deck[0].statuses = {"Pinned": true, "Guard": true}
	second.state.players.player.deck[0].statuses = {"Guard": true, "Pinned": true}
	t.assert_eq(first.state_hash(), second.state_hash(), "state hash ignores dictionary insertion order")


static func _test_state_hash_includes_rng_state(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90217)
	var hash_before := controller.state_hash()
	controller._rng.randi()
	t.assert_true(controller.state_hash() != hash_before, "state hash includes owned rng state")


static func _test_rejected_action_preserves_state_hash_and_log(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90213)
	var hash_before := controller.state_hash()
	var sequence_before := controller.state.sequence
	var rng_before := controller.state.rng_state
	var result = controller.submit_action(GameAction.create("start_match", "player", "", [], {}, controller.state.sequence))
	t.assert_true(not result.accepted, "invalid action is rejected")
	t.assert_eq(controller.state_hash(), hash_before, "rejected action leaves hash unchanged")
	t.assert_eq(controller.state.sequence, sequence_before, "rejected action leaves sequence unchanged")
	t.assert_eq(controller.state.rng_state, rng_before, "rejected action leaves rng unchanged")
	t.assert_eq(controller.replay_log.actions.size(), 0, "rejected action is not logged")


static func _test_broken_zone_invariant_aborts_match_once(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90214)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	controller.state.players.player.deck.append(controller.state.players.player.hand[0])
	var result = controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, controller.state.sequence))
	t.assert_true(not result.accepted, "invariant-breaking action is rejected")
	t.assert_eq(controller.state.phase, "invalid", "broken zone membership invalidates match")
	t.assert_eq(result.events.size(), 1, "invalid match emits one terminal event")
	t.assert_eq(result.events[0].type, "match_invalid", "invalid match event is diagnostic")
	var after = controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, controller.state.sequence))
	t.assert_eq(after.reason_code, "match_invalid", "invalid match refuses later actions")


static func _test_owner_slot_frontline_and_terminal_invariants_abort(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var owner_controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90218)
	owner_controller.submit_action(GameAction.create("start_match", "system", "", [], {}, owner_controller.state.sequence))
	owner_controller.state.players.player.hand[0].owner_id = "opponent"
	owner_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, owner_controller.state.sequence))
	t.assert_eq(owner_controller.state.phase, "invalid", "owner mismatch invalidates an accepted action")

	var frontline_controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90219)
	frontline_controller.submit_action(GameAction.create("start_match", "system", "", [], {}, frontline_controller.state.sequence))
	var frontline_card = frontline_controller.state.players.player.hand.pop_back()
	frontline_card.zone = "frontline"
	frontline_card.slot = 0
	frontline_controller.state.frontline[0] = frontline_card
	frontline_controller.state.frontline_controller_id = "opponent"
	frontline_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, frontline_controller.state.sequence))
	t.assert_eq(frontline_controller.state.phase, "invalid", "invalid Frontline controller invalidates an accepted action")

	var terminal_controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90220)
	terminal_controller.submit_action(GameAction.create("start_match", "system", "", [], {}, terminal_controller.state.sequence))
	terminal_controller.debug_set_trigger_hook(func(trigger: String, _context: Dictionary, _events: Array) -> void:
		if trigger == "turn_end":
			terminal_controller.state.phase = "complete"
			terminal_controller.state.winner_id = "player"
			terminal_controller.state.players.player.headquarters.current_defense = 0
	)
	terminal_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, terminal_controller.state.sequence))
	terminal_controller.submit_action(GameAction.create("mulligan", "opponent", "", [], {}, terminal_controller.state.sequence))
	terminal_controller.submit_action(GameAction.create("confirm_mulligan", "player", "", [], {}, terminal_controller.state.sequence))
	terminal_controller.submit_action(GameAction.create("confirm_mulligan", "opponent", "", [], {}, terminal_controller.state.sequence))
	terminal_controller.submit_action(GameAction.create("end_turn", terminal_controller.state.active_player_id, "", [], {}, terminal_controller.state.sequence))
	t.assert_eq(terminal_controller.state.phase, "invalid", "terminal contradiction invalidates the match")
	terminal_controller.debug_set_trigger_hook(Callable())


static func _test_terminal_action_does_not_mutate_state(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90215)
	controller.state.phase = "complete"
	controller.state.winner_id = "player"
	var hash_before := controller.state_hash()
	var result = controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
	t.assert_eq(result.reason_code, "match_complete", "terminal action is refused")
	t.assert_eq(controller.state_hash(), hash_before, "terminal action leaves hash unchanged")


static func _test_seeded_matches_are_identical_and_diverge_for_other_seeds(t) -> void:
	var first = CoreCards.scripted_full_match(90210)
	var second = CoreCards.scripted_full_match(90210)
	var different = CoreCards.scripted_full_match(90221)
	t.assert_eq(first.state_hash(), second.state_hash(), "same seed and actions produce identical state")
	t.assert_eq(first.event_history, second.event_history, "same seed and actions produce identical events")
	t.assert_eq(first.replay_log.to_dict().actions, second.replay_log.to_dict().actions, "same seed produces identical accepted actions")
	t.assert_true(first.state_hash() != different.state_hash(), "different seed diverges when shuffle randomness is used")
