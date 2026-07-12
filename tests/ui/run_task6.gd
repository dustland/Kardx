extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const UiContracts = preload("res://tests/ui/test_ui_contracts.gd")


func _init() -> void:
	call_deferred("_run")


func _run() -> void:
	var test_case := TestCase.new()
	await UiContracts.run_task6(test_case)
	var failures := test_case.finish()
	await process_frame
	await process_frame
	if failures == 0:
		print("PASS strict Task6 contracts")
	quit(failures)
