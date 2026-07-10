extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const SUITES := [
	preload("res://tests/core/test_contracts.gd"),
	preload("res://tests/core/test_state.gd"),
	preload("res://tests/core/test_setup.gd"),
	preload("res://tests/core/test_turns.gd"),
	preload("res://tests/core/test_combat.gd"),
	preload("res://tests/core/test_effects.gd"),
]

func _init() -> void:
	var test_case := TestCase.new()
	for suite in SUITES:
		suite.run(test_case)
	if test_case.finish() == 0:
		print("PASS contracts, state, setup, turns, combat, and effects")
	quit(test_case.finish())
