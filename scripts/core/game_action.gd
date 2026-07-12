class_name GameAction
extends RefCounted

var type: String
var actor_id: String
var source_id: String
var target_ids: Array[String]
var payload: Dictionary
var expected_sequence: int

static func create(action_type: String, actor: String, source: String = "", targets: Array[String] = [], data: Dictionary = {}, sequence: int = 0) -> GameAction:
	var action = load("res://scripts/core/game_action.gd").new()
	action.type = action_type
	action.actor_id = actor
	action.source_id = source
	action.target_ids = targets.duplicate()
	action.payload = data.duplicate(true)
	action.expected_sequence = sequence
	return action
