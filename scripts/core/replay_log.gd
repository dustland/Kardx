class_name ReplayLog
extends RefCounted

const GameAction = preload("res://scripts/core/game_action.gd")

var seed: int = 0
var player_deck_ids: Array = []
var opponent_deck_ids: Array = []
var starting_player_id: String = ""
var actions: Array = []
var terminal_result: Dictionary = {}


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


func to_dict() -> Dictionary:
	return {
		"seed": seed,
		"player_deck_ids": player_deck_ids.duplicate(true),
		"opponent_deck_ids": opponent_deck_ids.duplicate(true),
		"starting_player_id": starting_player_id,
		"actions": actions.duplicate(true),
		"terminal_result": terminal_result.duplicate(true),
	}


static func from_dict(data: Dictionary) -> ReplayLog:
	var log: ReplayLog = load("res://scripts/core/replay_log.gd").new()
	log.seed = int(data.get("seed", 0))
	log.player_deck_ids = (data.get("player_deck_ids", []) as Array).duplicate(true)
	log.opponent_deck_ids = (data.get("opponent_deck_ids", []) as Array).duplicate(true)
	log.starting_player_id = str(data.get("starting_player_id", ""))
	log.actions = (data.get("actions", []) as Array).duplicate(true)
	log.terminal_result = (data.get("terminal_result", {}) as Dictionary).duplicate(true)
	return log


func replay(card_definitions: Dictionary):
	var controller = load("res://scripts/core/match_controller.gd").create(
		card_definitions,
		player_deck_ids,
		opponent_deck_ids,
		seed,
	)
	for entry_value in actions:
		if not (entry_value is Dictionary):
			controller._abort_invalid("replay_invalid_entry", {"entry": entry_value})
			return controller
		var entry: Dictionary = entry_value
		var action_value = entry.get("action", {})
		if not (action_value is Dictionary):
			controller._abort_invalid("replay_invalid_action", {"entry": entry})
			return controller
		var action := _deserialize_action(action_value)
		var result = controller.submit_action(action)
		if not result.accepted:
			controller._abort_invalid("replay_action_rejected", {"reason_code": result.reason_code, "action": action_value})
			return controller
		var expected_result_sequence := int(entry.get("result_sequence", -1))
		if controller.state.sequence != expected_result_sequence:
			controller._abort_invalid("replay_sequence_diverged", {
				"expected_sequence": expected_result_sequence,
				"actual_sequence": controller.state.sequence,
				"action": action_value,
			})
			return controller
	if not starting_player_id.is_empty() and controller.state.starting_player_id != starting_player_id:
		controller._abort_invalid("replay_starting_player_diverged", {
			"expected_starting_player_id": starting_player_id,
			"actual_starting_player_id": controller.state.starting_player_id,
		})
		return controller
	if not terminal_result.is_empty() and controller._replay_terminal_result() != terminal_result:
		controller._abort_invalid("replay_terminal_result_diverged", {
			"expected_terminal_result": terminal_result,
			"actual_terminal_result": controller._replay_terminal_result(),
		})
	return controller


static func _serialize_action(action: GameAction) -> Dictionary:
	return {
		"type": action.type,
		"actor_id": action.actor_id,
		"source_id": action.source_id,
		"target_ids": action.target_ids.duplicate(),
		"payload": action.payload.duplicate(true),
		"expected_sequence": action.expected_sequence,
	}


static func _deserialize_action(data: Dictionary) -> GameAction:
	var target_ids: Array[String] = []
	for target_id in data.get("target_ids", []):
		target_ids.append(str(target_id))
	return GameAction.create(
		str(data.get("type", "")),
		str(data.get("actor_id", "")),
		str(data.get("source_id", "")),
		target_ids,
		(data.get("payload", {}) as Dictionary).duplicate(true),
		int(data.get("expected_sequence", 0)),
	)
