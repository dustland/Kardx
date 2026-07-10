extends RefCounted

const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const ReplayLog = preload("res://scripts/core/replay_log.gd")


static func run(t) -> void:
	_test_replay_reproduces_setup_and_sequence_guard(t)
	_test_replay_rejects_zero_tampered_expected_sequence(t)
	_test_invalid_terminal_record_replays_deterministically(t)
	_test_replay_schema_rejects_untrusted_logs(t)
	_test_replay_aborts_on_tampered_sequence(t)
	_test_replay_aborts_on_tampered_action(t)
	_test_state_hash_is_canonical_for_dictionary_order(t)
	_test_state_hash_preserves_numeric_and_string_dictionary_keys(t)
	_test_state_hash_includes_rng_state(t)
	_test_rejected_action_preserves_state_hash_and_log(t)
	_test_broken_zone_invariant_aborts_match_once(t)
	_test_owner_slot_frontline_and_terminal_invariants_abort(t)
	_test_complete_and_invalid_terminal_invariants(t)
	_test_active_countermeasure_invariants(t)
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


static func _test_replay_rejects_zero_tampered_expected_sequence(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90210)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, controller.state.sequence))
	var tampered: Dictionary = controller.replay_log.to_dict()
	var entry: Dictionary = tampered.actions[1]
	var action: Dictionary = entry.action
	action.expected_sequence = 0
	entry.action = action
	tampered.actions[1] = entry

	var replayed = ReplayLog.from_dict(tampered).replay(fixture.definitions)
	t.assert_eq(replayed.state.phase, "invalid", "zeroed expected sequence aborts replay")
	t.assert_eq(replayed.invalid_diagnostics.code, "replay_expected_sequence_diverged", "zeroed sequence has stable diagnostics")


static func _test_invalid_terminal_record_replays_deterministically(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90222)
	controller._abort_invalid("forced_invalid", {"source": "test"})
	var log: Dictionary = controller.replay_log.to_dict()
	t.assert_eq(log.terminal_result.type, "invalid", "invalid match exports a typed terminal record")
	t.assert_eq(log.terminal_result.pre_abort_sequence, 0, "invalid terminal records its replay boundary")
	t.assert_eq(log.terminal_result.result_sequence, controller.state.sequence, "invalid terminal records resulting sequence")

	var replayed = ReplayLog.from_dict(log).replay(fixture.definitions)
	t.assert_eq(replayed.state_hash(), controller.state_hash(), "invalid terminal replay reaches the same hash")
	t.assert_eq(replayed.event_history, controller.event_history, "invalid terminal replay emits the same event")
	t.assert_eq(replayed.replay_log.to_dict().terminal_result, log.terminal_result, "invalid terminal record survives replay export")
	t.assert_eq(replayed.replay_log.actions.size(), 0, "invalid terminal replay does not log a fake action")


static func _test_replay_schema_rejects_untrusted_logs(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90229)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	var base: Dictionary = controller.replay_log.to_dict()
	var missing_deck: Dictionary = base.duplicate(true)
	missing_deck.erase("player_deck_ids")
	var string_action: Dictionary = base.duplicate(true)
	string_action.actions[0].action = "not-an-action"
	var array_payload: Dictionary = base.duplicate(true)
	array_payload.actions[0].action.payload = []
	var bad_targets: Dictionary = base.duplicate(true)
	bad_targets.actions[0].action.target_ids = ["ok", 1]
	var bad_terminal: Dictionary = base.duplicate(true)
	bad_terminal.terminal_result = {"type": "invalid", "diagnostic": "bad"}
	for malformed in ["not-a-dictionary", missing_deck, string_action, array_payload, bad_targets, bad_terminal]:
		var replayed = ReplayLog.from_dict(malformed).replay(fixture.definitions)
		t.assert_eq(replayed.state.phase, "invalid", "malformed replay log is invalid: %s" % [typeof(malformed)])
		t.assert_eq(replayed.invalid_diagnostics.code, "replay_invalid", "malformed replay has stable diagnostic")
		t.assert_eq(replayed.event_history.size(), 1, "malformed replay emits one terminal diagnostic")


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


static func _test_state_hash_preserves_numeric_and_string_dictionary_keys(t) -> void:
	var fixture := CoreCards.build_valid_fixture()
	var first := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90223)
	var reordered := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90223)
	var changed := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 90223)
	first.state.players.player.deck[0].statuses = {1: "numeric", "1": "string"}
	reordered.state.players.player.deck[0].statuses = {"1": "string", 1: "numeric"}
	changed.state.players.player.deck[0].statuses = {1: "changed", "1": "string"}
	t.assert_eq(first.state_hash(), reordered.state_hash(), "mixed dictionary keys remain insertion-order independent")
	t.assert_true(first.state_hash() != changed.state_hash(), "numeric and string keys do not collide")


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


static func _test_complete_and_invalid_terminal_invariants(t) -> void:
	var complete = CoreCards.scripted_full_match(90210)
	t.assert_true(complete._validate_invariants().valid, "normal Headquarters defeat has a coherent terminal state")
	complete.state.players.opponent.headquarters.current_defense = 1
	t.assert_eq(complete._validate_invariants().code, "terminal_loser_headquarters_not_zero", "complete match requires losing Headquarters at zero")
	complete.state.players.opponent.headquarters.current_defense = 0
	complete.state.frontline_controller_id = "opponent"
	t.assert_eq(complete._validate_invariants().code, "invalid_frontline_controller", "complete match validates Frontline controller")
	complete._abort_invalid("forced_invalid", {"source": "test"})
	t.assert_true(complete._validate_invariants().valid, "invalid diagnostic state does not recursively fail invariants")


static func _test_active_countermeasure_invariants(t) -> void:
	var valid_controller := _countermeasure_invariant_controller(90224)
	var valid_counter = valid_controller.state.players.player.hand[0]
	_set_active_countermeasure(valid_controller, valid_counter)
	var valid_result = valid_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, valid_controller.state.sequence))
	t.assert_true(valid_result.accepted, "valid active Countermeasure remains legal")

	var duplicate_controller := _countermeasure_invariant_controller(90225)
	var duplicate_counter = duplicate_controller.state.players.player.hand[0]
	_set_active_countermeasure(duplicate_controller, duplicate_counter)
	duplicate_controller.state.players.player.active_countermeasures.append(duplicate_counter)
	duplicate_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, duplicate_controller.state.sequence))
	t.assert_eq(duplicate_controller.state.phase, "invalid", "duplicate active Countermeasure reference invalidates")

	var face_up_controller := _countermeasure_invariant_controller(90226)
	var face_up_counter = face_up_controller.state.players.player.hand[0]
	_set_active_countermeasure(face_up_controller, face_up_counter)
	face_up_counter.face_down = false
	face_up_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, face_up_controller.state.sequence))
	t.assert_eq(face_up_controller.state.phase, "invalid", "face-up active Countermeasure invalidates")

	var foreign_controller := _countermeasure_invariant_controller(90227)
	var foreign_counter = foreign_controller.state.players.opponent.hand[0]
	foreign_counter.category = "Countermeasure"
	foreign_counter.countermeasure_active = true
	foreign_counter.face_down = true
	foreign_controller.state.players.player.active_countermeasures.append(foreign_counter)
	foreign_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, foreign_controller.state.sequence))
	t.assert_eq(foreign_controller.state.phase, "invalid", "foreign active Countermeasure invalidates")

	var stale_controller := _countermeasure_invariant_controller(90228)
	var stale_counter = stale_controller.state.players.player.hand[0]
	_set_active_countermeasure(stale_controller, stale_counter)
	stale_controller.state.players.player.hand.erase(stale_counter)
	stale_controller.state.players.player.deck.append(stale_counter)
	stale_counter.zone = "deck"
	stale_controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, stale_controller.state.sequence))
	t.assert_eq(stale_controller.state.phase, "invalid", "stale active Countermeasure zone invalidates")


static func _countermeasure_invariant_controller(seed: int) -> MatchController:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, seed)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	return controller


static func _set_active_countermeasure(controller: MatchController, countermeasure) -> void:
	countermeasure.category = "Countermeasure"
	countermeasure.countermeasure_active = true
	countermeasure.face_down = true
	controller.state.players.player.active_countermeasures.append(countermeasure)


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
