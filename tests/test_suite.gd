extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const UI_SUITE = preload("res://tests/ui/test_ui_contracts.gd")
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
	preload("res://tests/ai/test_ai.gd"),
	preload("res://tests/ai/test_ai_matches.gd"),
	UI_SUITE,
]

func _init() -> void:
	var test_case := TestCase.new()
	var suites := [UI_SUITE] if "--ui-only" in OS.get_cmdline_user_args() else SUITES
	for suite in suites:
		suite.run(test_case)
	var failures := test_case.finish()
	if failures == 0:
		print("PASS contracts, state, setup, turns, combat, effects, replay, and full match")
	quit(failures)
