extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const SUITES := [
	preload("res://tests/core/test_contracts.gd"),
	preload("res://tests/core/test_state.gd"),
	preload("res://tests/core/test_setup.gd"),
	preload("res://tests/core/test_turns.gd"),
	preload("res://tests/core/test_combat.gd"),
	preload("res://tests/core/test_effects.gd"),
	preload("res://tests/core/test_replay.gd"),
	preload("res://tests/core/test_full_match.gd"),
	preload("res://tests/content/test_catalog.gd"),
	preload("res://tests/content/test_card_behaviors.gd"),
]

func _init() -> void:
	var test_case := TestCase.new()
	for suite in SUITES:
		suite.run(test_case)
	if test_case.finish() == 0:
		print("PASS contracts, state, setup, turns, combat, effects, replay, and full match")
	quit(test_case.finish())
