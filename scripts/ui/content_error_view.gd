class_name ContentErrorView
extends Control

var _retry_validation: Callable


func initialize(diagnostics: Array[Dictionary], retry_validation: Callable) -> void:
	_retry_validation = retry_validation
	var sorted_diagnostics := diagnostics.duplicate(true)
	sorted_diagnostics.sort_custom(func(left: Dictionary, right: Dictionary) -> bool:
		var left_key := "%s\n%s\n%s" % [left.get("code", ""), left.get("path", ""), left.get("message", "")]
		var right_key := "%s\n%s\n%s" % [right.get("code", ""), right.get("path", ""), right.get("message", "")]
		return left_key < right_key
	)
	%DiagnosticCount.text = "%d validation issue%s" % [sorted_diagnostics.size(), "" if sorted_diagnostics.size() == 1 else "s"]
	%Diagnostics.text = _format_diagnostics(sorted_diagnostics)


func _format_diagnostics(diagnostics: Array) -> String:
	var lines: PackedStringArray = []
	for diagnostic_value in diagnostics:
		var diagnostic: Dictionary = diagnostic_value
		lines.append("[%s] %s\n%s" % [
			diagnostic.get("code", "unknown"),
			diagnostic.get("path", "unknown"),
			diagnostic.get("message", "No diagnostic message."),
		])
	return "\n\n".join(lines)


func _on_retry_pressed() -> void:
	if _retry_validation.is_valid():
		_retry_validation.call()


func _on_quit_pressed() -> void:
	get_tree().quit()
