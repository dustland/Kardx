extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const Suite = preload("res://tests/ui/test_ui_contracts.gd")

func _initialize() -> void:
	var test_case := TestCase.new()
	await Suite.run_task5(test_case)
	quit(test_case.finish())
