extends SceneTree

const TestCase = preload("res://tests/support/test_case.gd")
const OnboardingTests = preload("res://tests/ui/test_onboarding.gd")


func _init() -> void:
	call_deferred("_run")


func _run() -> void:
	var test_case := TestCase.new()
	OnboardingTests.run(test_case)
	var failures := test_case.finish()
	await process_frame
	await process_frame
	if failures == 0:
		print("PASS onboarding model and persistence")
	quit(failures)
