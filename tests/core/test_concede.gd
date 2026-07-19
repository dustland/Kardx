extends RefCounted

const CardInstance = preload("res://scripts/core/card_instance.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const ReplayLog = preload("res://scripts/core/replay_log.gd")
const Main = preload("res://scripts/main.gd")


static func run(t) -> void:
	_test_concede_ends_match_and_crowns_opponent(t)
	_test_concede_is_atomic_when_already_terminal(t)
	_test_concede_replays_identically(t)
	_test_concede_emits_terminal_reason(t)


static func _test_concede_ends_match_and_crowns_opponent(t) -> void:
	var controller := _controller(7117)
	var before_winner := controller.state.winner_id
	var before_phase := controller.state.phase

	var result = controller.submit_action(GameAction.create("concede", "player"))

	t.assert_true(result.accepted, "player concede action is accepted")
	t.assert_eq(controller.state.winner_id, "opponent", "concede crowns the opponent")
	t.assert_eq(controller.state.phase, "complete", "concede completes the match")
	t.assert_ne(before_winner, "opponent", "fixture did not already have an opponent winner")
	t.assert_ne(before_phase, "complete", "fixture did not already start complete")
	var types := _event_types(result.events)
	t.assert_true(types.has("player_conceded"), "concede emits a player_conceded marker event")
	t.assert_true(types.has("match_ended"), "concede emits the terminal match_ended event")


static func _test_concede_is_atomic_when_already_terminal(t) -> void:
	var controller := _controller(7118)
	# End the match once.
	controller.submit_action(GameAction.create("concede", "player"))
	t.assert_eq(controller.state.phase, "complete", "first concede completes the match")
	var sequence_before := controller.state.sequence

	var second = controller.submit_action(GameAction.create("concede", "player"))

	t.assert_false(second.accepted, "conceding a second time is rejected")
	t.assert_eq(second.reason_code, "match_complete", "terminal concede reports a match_complete reason")
	t.assert_eq(controller.state.sequence, sequence_before, "rejected concede does not advance state")


static func _test_concede_replays_identically(t) -> void:
	var controller := _controller(7119)
	controller.submit_action(GameAction.create("concede", "player"))
	t.assert_eq(controller.state.phase, "complete", "concede reached a terminal state")

	var replayed = ReplayLog.from_dict(controller.replay_log.to_dict()).replay(controller.card_definitions)
	t.assert_eq(replayed.state_hash(), controller.state_hash(), "concede replay reaches the same state hash")
	t.assert_eq(replayed.event_history, controller.event_history, "concede replay emits the same events")
	t.assert_eq(replayed.state.winner_id, controller.state.winner_id, "concede replay preserves the winner")


static func _test_concede_emits_terminal_reason(t) -> void:
	# main.terminal_reason is the bridge the result screen reads; a conceded
	# match must surface the "concede" reason code so ResultView shows "Conceded".
	var controller := _controller(7120)
	controller.submit_action(GameAction.create("concede", "player"))
	var reason := Main.terminal_reason(
		controller.state.phase,
		controller.state.winner_id,
		controller.event_history,
		controller.replay_log.terminal_result if controller.replay_log != null else {},
	)
	t.assert_eq(reason, "concede", "conceded match derives the concede terminal reason")


static func _controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": {"id": "p-hq", "title": "Player HQ", "nation": "US", "category": "Headquarters", "rarity": "Elite"},
		"o-hq": {"id": "o-hq", "title": "Opponent HQ", "nation": "SU", "category": "Headquarters", "rarity": "Elite"},
	}
	var controller: MatchController = MatchController.create(definitions, ["p-hq"], ["o-hq"], seed)
	controller.state.phase = "action"
	controller.state.active_player_id = "player"
	controller.state.starting_player_id = "player"
	controller.state.turn = 3
	return controller


static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(str(event.get("type", "")))
	return types
