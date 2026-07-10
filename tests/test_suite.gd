extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const SUITES := [
	preload("res://tests/core/test_contracts.gd"),
	preload("res://tests/core/test_state.gd"),
]

func _init() -> void:
	var test_case := TestCase.new()
	for suite in SUITES:
		suite.run(test_case)
	if test_case.finish() == 0:
		print("PASS contracts and state")
	quit(test_case.finish())
