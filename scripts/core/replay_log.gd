class_name ReplayLog
extends RefCounted

const GameAction = preload("res://scripts/core/game_action.gd")

var seed: int = 0
var player_deck_ids: Array = []
var opponent_deck_ids: Array = []
var starting_player_id: String = ""
var actions: Array = []
var terminal_result: Dictionary = {}
var validation_error: Dictionary = {}


static func create(match_seed: int, player_deck: Array, opponent_deck: Array) -> ReplayLog:
	var log: ReplayLog = load("res://scripts/core/replay_log.gd").new()
	log.seed = match_seed
	log.player_deck_ids = player_deck.duplicate(true)
	log.opponent_deck_ids = opponent_deck.duplicate(true)
	return log


func record(action: GameAction, result_sequence: int) -> void:
	actions.append({
		"action": _serialize_action(action),
		"result_sequence": result_sequence,
	})


func record_invalid_terminal(
	code: String,
	details: Dictionary,
	pre_abort_sequence: int,
	result_sequence: int,
	state_hash: String,
	pre_abort_state_hash: String,
	state_snapshot: Dictionary
) -> void:
	terminal_result = {
		"type": "invalid",
		"pre_abort_sequence": pre_abort_sequence,
		"result_sequence": result_sequence,
		"diagnostic": {
			"code": code,
			"details": details.duplicate(true),
		},
		"state_hash": state_hash,
		"pre_abort_state_hash": pre_abort_state_hash,
		"state_snapshot": state_snapshot.duplicate(true),
	}


func to_dict() -> Dictionary:
	return {
		"seed": seed,
		"player_deck_ids": player_deck_ids.duplicate(true),
		"opponent_deck_ids": opponent_deck_ids.duplicate(true),
		"starting_player_id": starting_player_id,
		"actions": actions.duplicate(true),
		"terminal_result": terminal_result.duplicate(true),
	}


static func from_dict(data) -> ReplayLog:
	var log: ReplayLog = load("res://scripts/core/replay_log.gd").new()
	var validation := _validate_log(data)
	if not validation.valid:
		log.validation_error = {"code": str(validation.code)}
		return log
	var source: Dictionary = data
	log.seed = source.seed
	log.player_deck_ids = source.player_deck_ids.duplicate(true)
	log.opponent_deck_ids = source.opponent_deck_ids.duplicate(true)
	log.starting_player_id = source.starting_player_id
	log.actions = source.actions.duplicate(true)
	log.terminal_result = source.terminal_result.duplicate(true)
	return log


func replay(card_definitions: Dictionary):
	var controller = load("res://scripts/core/match_controller.gd").create(
		card_definitions,
		player_deck_ids,
		opponent_deck_ids,
		seed,
	)
	if not validation_error.is_empty():
		controller._abort_invalid("replay_invalid", validation_error)
		return controller
	for entry_value in actions:
		var entry: Dictionary = entry_value
		var action_data: Dictionary = entry.action
		var decoded := _deserialize_action(action_data)
		if not decoded.valid:
			controller._abort_invalid("replay_invalid", {"schema_code": decoded.code})
			return controller
		var action: GameAction = decoded.action
		if action.expected_sequence != controller.state.sequence:
			controller._abort_invalid("replay_expected_sequence_diverged", {
				"expected_sequence": action.expected_sequence,
				"actual_sequence": controller.state.sequence,
				"action": action_data,
			})
			return controller
		var result = controller.submit_action(action)
		if not result.accepted:
			controller._abort_invalid("replay_action_rejected", {"reason_code": result.reason_code, "action": action_data})
			return controller
		if controller.state.sequence != entry.result_sequence:
			controller._abort_invalid("replay_sequence_diverged", {
				"expected_sequence": entry.result_sequence,
				"actual_sequence": controller.state.sequence,
				"action": action_data,
			})
			return controller
	if not starting_player_id.is_empty() and controller.state.starting_player_id != starting_player_id:
		controller._abort_invalid("replay_starting_player_diverged", {
			"expected_starting_player_id": starting_player_id,
			"actual_starting_player_id": controller.state.starting_player_id,
		})
		return controller
	if terminal_result.type == "invalid":
		return _replay_invalid_terminal(controller)
	if controller._replay_terminal_result() != terminal_result:
		controller._abort_invalid("replay_terminal_result_diverged", {
			"expected_terminal_result": terminal_result,
			"actual_terminal_result": controller._replay_terminal_result(),
		})
	return controller


func _replay_invalid_terminal(controller):
	var restored: Dictionary = controller._restore_invalid_replay_snapshot(terminal_result.state_snapshot)
	if not restored.valid:
		controller._abort_invalid("replay_invalid", {"reason": "invalid_terminal_snapshot"})
		return controller
	if controller.state.sequence != terminal_result.pre_abort_sequence \
		or controller.state_hash() != terminal_result.pre_abort_state_hash:
		controller._abort_invalid("replay_invalid", {"reason": "invalid_terminal_metadata"})
		return controller
	var diagnostic: Dictionary = terminal_result.diagnostic
	if terminal_result.result_sequence != terminal_result.pre_abort_sequence + 1 \
		or controller._predicted_invalid_terminal_hash(diagnostic.code, diagnostic.details) != terminal_result.state_hash:
		controller._abort_invalid("replay_invalid", {"reason": "invalid_terminal_metadata"})
		return controller
	controller._abort_invalid(diagnostic.code, diagnostic.details)
	return controller


static func _validate_log(data) -> Dictionary:
	if not (data is Dictionary):
		return {"valid": false, "code": "log_not_dictionary"}
	for field in ["seed", "player_deck_ids", "opponent_deck_ids", "starting_player_id", "actions", "terminal_result"]:
		if not data.has(field):
			return {"valid": false, "code": "missing_%s" % field}
	if typeof(data.seed) != TYPE_INT:
		return {"valid": false, "code": "invalid_seed"}
	if not _is_string_array(data.player_deck_ids) or not _is_string_array(data.opponent_deck_ids):
		return {"valid": false, "code": "invalid_deck_ids"}
	if typeof(data.starting_player_id) != TYPE_STRING:
		return {"valid": false, "code": "invalid_starting_player"}
	if not (data.actions is Array):
		return {"valid": false, "code": "invalid_actions"}
	for entry in data.actions:
		var action_entry_validation := _validate_action_entry(entry)
		if not action_entry_validation.valid:
			return action_entry_validation
	return _validate_terminal_result(data.terminal_result)


static func _validate_action_entry(entry) -> Dictionary:
	if not (entry is Dictionary) or not entry.has("action") or not entry.has("result_sequence"):
		return {"valid": false, "code": "invalid_action_entry"}
	if typeof(entry.result_sequence) != TYPE_INT:
		return {"valid": false, "code": "invalid_result_sequence"}
	return _validate_action_data(entry.action)


static func _validate_action_data(data) -> Dictionary:
	if not (data is Dictionary):
		return {"valid": false, "code": "invalid_action"}
	for field in ["type", "actor_id", "source_id", "target_ids", "payload", "expected_sequence"]:
		if not data.has(field):
			return {"valid": false, "code": "missing_action_%s" % field}
	if typeof(data.type) != TYPE_STRING or typeof(data.actor_id) != TYPE_STRING or typeof(data.source_id) != TYPE_STRING:
		return {"valid": false, "code": "invalid_action_strings"}
	if not _is_string_array(data.target_ids):
		return {"valid": false, "code": "invalid_target_ids"}
	if not (data.payload is Dictionary):
		return {"valid": false, "code": "invalid_payload"}
	if typeof(data.expected_sequence) != TYPE_INT:
		return {"valid": false, "code": "invalid_expected_sequence"}
	return {"valid": true}


static func _validate_terminal_result(data) -> Dictionary:
	if not (data is Dictionary) or not data.has("type") or typeof(data.type) != TYPE_STRING:
		return {"valid": false, "code": "invalid_terminal_result"}
	if data.type == "state":
		for field in ["phase", "winner_id", "sequence"]:
			if not data.has(field):
				return {"valid": false, "code": "missing_terminal_%s" % field}
		if typeof(data.phase) != TYPE_STRING or typeof(data.winner_id) != TYPE_STRING or typeof(data.sequence) != TYPE_INT:
			return {"valid": false, "code": "invalid_terminal_state"}
		return {"valid": true}
	if data.type == "invalid":
		for field in ["pre_abort_sequence", "result_sequence", "diagnostic", "state_hash", "pre_abort_state_hash", "state_snapshot"]:
			if not data.has(field):
				return {"valid": false, "code": "missing_invalid_terminal_%s" % field}
		if typeof(data.pre_abort_sequence) != TYPE_INT or typeof(data.result_sequence) != TYPE_INT \
			or typeof(data.state_hash) != TYPE_STRING or typeof(data.pre_abort_state_hash) != TYPE_STRING \
			or not (data.diagnostic is Dictionary) or not (data.state_snapshot is Dictionary):
			return {"valid": false, "code": "invalid_invalid_terminal"}
		if not data.diagnostic.has("code") or not data.diagnostic.has("details") \
			or typeof(data.diagnostic.code) != TYPE_STRING or not (data.diagnostic.details is Dictionary):
			return {"valid": false, "code": "invalid_invalid_diagnostic"}
		return {"valid": true}
	return {"valid": false, "code": "unknown_terminal_type"}


static func _deserialize_action(data) -> Dictionary:
	var validation := _validate_action_data(data)
	if not validation.valid:
		return validation
	var target_ids: Array[String] = []
	for target_id in data.target_ids:
		target_ids.append(target_id)
	return {
		"valid": true,
		"action": GameAction.create(
			data.type,
			data.actor_id,
			data.source_id,
			target_ids,
			data.payload.duplicate(true),
			data.expected_sequence,
		),
	}


static func _is_string_array(value) -> bool:
	if not (value is Array):
		return false
	for entry in value:
		if typeof(entry) != TYPE_STRING:
			return false
	return true


static func _serialize_action(action: GameAction) -> Dictionary:
	return {
		"type": action.type,
		"actor_id": action.actor_id,
		"source_id": action.source_id,
		"target_ids": action.target_ids.duplicate(),
		"payload": action.payload.duplicate(true),
		"expected_sequence": action.expected_sequence,
	}
