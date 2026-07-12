extends RefCounted

const GameAction = preload("res://scripts/core/game_action.gd")
const ActionResult = preload("res://scripts/core/action_result.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")

static func run(t) -> void:
	var action = GameAction.create("end_turn", "player", "", [], {}, 7)
	t.assert_eq(action.type, "end_turn", "action type")
	t.assert_eq(action.expected_sequence, 7, "sequence guard")
	var rejected = ActionResult.reject("stale_action", "State changed", {"sequence": 8})
	t.assert_true(not rejected.accepted, "rejection flag")
	t.assert_eq(rejected.reason_code, "stale_action", "stable error code")
	t.assert_eq(GameConstants.MAX_CREDITS, 12, "Credit cap")
