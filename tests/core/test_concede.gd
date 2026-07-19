extends RefCounted

const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const ReplayLog = preload("res://scripts/core/replay_log.gd")
const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const Main = preload("res://scripts/main.gd")


static func run(t) -> void:
	_test_concede_ends_match_and_crowns_opponent(t)
	_test_concede_is_atomic_when_already_terminal(t)
	_test_concede_replays_identically(t)
	_test_concede_emits_terminal_reason(t)


# Build a controller that has advanced past mulligan into the action phase via
# the real start_match/mulligan/confirm flow, so the recorded replay is a valid
# deterministic sequence (a manually-seeded controller is not replayable on its
# own because create() does not run start_match).
static func _action_phase_controller(seed: int) -> MatchController:
	var fixture := CoreCards.build_valid_fixture()
	var controller := MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, seed)
	controller.submit_action(GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("mulligan", "player", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("mulligan", "opponent", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("confirm_mulligan", "player", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("confirm_mulligan", "opponent", "", [], {}, controller.state.sequence))
	return controller


static func _test_concede_ends_match_and_crowns_opponent(t) -> void:
	var controller := _action_phase_controller(7117)
	t.assert_eq(controller.state.phase, "action", "fixture reached the action phase")
	var conceding_id: String = controller.state.active_player_id
	var expected_winner := "opponent" if conceding_id == "player" else "player"

	var result = controller.submit_action(_concede_action(controller, conceding_id))

	t.assert_true(result.accepted, "active player concede action is accepted")
	t.assert_eq(controller.state.winner_id, expected_winner, "concede crowns the opponent")
	t.assert_eq(controller.state.phase, "complete", "concede completes the match")
	t.assert_eq(controller.state.players[conceding_id].headquarters.current_defense, 0, "conceding Headquarters is at zero")
	var types := _event_types(result.events)
	t.assert_true(types.has("player_conceded"), "concede emits a player_conceded marker event")
	t.assert_true(types.has("match_ended"), "concede emits the terminal match_ended event")


static func _test_concede_is_atomic_when_already_terminal(t) -> void:
	var controller := _action_phase_controller(7118)
	var conceding_id: String = controller.state.active_player_id
	controller.submit_action(_concede_action(controller, conceding_id))
	t.assert_eq(controller.state.phase, "complete", "first concede completes the match")
	var sequence_before := controller.state.sequence

	var second = controller.submit_action(_concede_action(controller, conceding_id))

	t.assert_eq(second.accepted, false, "conceding a second time is rejected")
	t.assert_eq(second.reason_code, "match_complete", "terminal concede reports a match_complete reason")
	t.assert_eq(controller.state.sequence, sequence_before, "rejected concede does not advance state")


static func _test_concede_replays_identically(t) -> void:
	var controller := _action_phase_controller(7119)
	controller.submit_action(_concede_action(controller, controller.state.active_player_id))
	t.assert_eq(controller.state.phase, "complete", "concede reached a terminal state")

	var replayed = ReplayLog.from_dict(controller.replay_log.to_dict()).replay(controller.card_definitions)
	t.assert_eq(replayed.state_hash(), controller.state_hash(), "concede replay reaches the same state hash")
	t.assert_eq(replayed.event_history, controller.event_history, "concede replay emits the same events")
	t.assert_eq(replayed.state.winner_id, controller.state.winner_id, "concede replay preserves the winner")


static func _test_concede_emits_terminal_reason(t) -> void:
	# main.terminal_reason is the bridge the result screen reads; a conceded
	# match must surface the "concede" reason code so ResultView shows "Conceded".
	var controller := _action_phase_controller(7120)
	controller.submit_action(_concede_action(controller, controller.state.active_player_id))
	var reason := Main.terminal_reason(
		controller.state.phase,
		controller.state.winner_id,
		controller.event_history,
		controller.replay_log.terminal_result if controller.replay_log != null else {},
	)
	t.assert_eq(reason, "concede", "conceded match derives the concede terminal reason")


# Concede actions carry the current sequence so the recorded replay replays
# against a freshly-seeded controller (replay rejects stale-sequence actions).
static func _concede_action(controller: MatchController, conceding_id: String) -> GameAction:
	return GameAction.create("concede", conceding_id, "", [], {}, controller.state.sequence)


static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(str(event.get("type", "")))
	return types
