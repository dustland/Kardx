extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const UiContracts = preload("res://tests/ui/test_ui_contracts.gd")


func _init() -> void:
	call_deferred("_run")


func _run() -> void:
	var test_case := TestCase.new()
	await UiContracts.run_task4(test_case)
	var failures := test_case.finish()
	if failures == 0:
		print("PASS focused mulligan contracts")
	await process_frame
	await process_frame
	quit(failures)
