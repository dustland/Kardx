class_name TestCase
extends RefCounted

var _failures := 0

func assert_eq(actual: Variant, expected: Variant, message: String) -> void:
	if actual != expected:
		_failures += 1
		print("FAIL: %s" % message)

func assert_true(value: bool, message: String) -> void:
	if not value:
		_failures += 1
		print("FAIL: %s" % message)

func finish() -> int:
	return 0 if _failures == 0 else 1
