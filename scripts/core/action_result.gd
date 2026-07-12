class_name ActionResult
extends RefCounted

var accepted: bool
var reason_code: String
var message: String
var events: Array
var snapshot: Dictionary

static func accept(result_events: Array = [], state_snapshot: Dictionary = {}) -> ActionResult:
	var result = load("res://scripts/core/action_result.gd").new()
	result.accepted = true
	result.reason_code = ""
	result.message = ""
	result.events = result_events.duplicate(true)
	result.snapshot = state_snapshot.duplicate(true)
	return result

static func reject(code: String, rejection_message: String, state_snapshot: Dictionary = {}) -> ActionResult:
	var result = load("res://scripts/core/action_result.gd").new()
	result.accepted = false
	result.reason_code = code
	result.message = rejection_message
	result.events = []
	result.snapshot = state_snapshot.duplicate(true)
	return result
