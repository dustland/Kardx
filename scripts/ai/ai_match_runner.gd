class_name AiMatchRunner
extends RefCounted

const AIPlayer = preload("res://scripts/ai/ai_player.gd")
const ContentCatalog = preload("res://scripts/content/content_catalog.gd")
const ContentValidator = preload("res://scripts/content/content_validator.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const ReplayLog = preload("res://scripts/core/replay_log.gd")

const MAX_ACTIONS_PER_TURN := 2
const SEARCH_METRIC_FIELDS := ["action_lists", "candidate_actions", "simulation_attempts", "rejected_simulations"]


static func run_match(seed: int, player_difficulty: String, opponent_difficulty: String, max_turns: int = 300) -> Dictionary:
	var catalog = ContentCatalog.load_from_paths(
		"res://data/cards.json",
		"res://data/abilities.json",
		"res://data/decks.json",
		"res://data/rules.json"
	)
	var content_diagnostics: Array = catalog.load_errors.duplicate(true)
	content_diagnostics.append_array(ContentValidator.validate(catalog))
	var result := _new_result(seed, player_difficulty, opponent_difficulty, max_turns, content_diagnostics)
	if not content_diagnostics.is_empty():
		result.final_reason = "content_invalid"
		return result
	if not catalog.decks_by_id.has("us-starter") or not catalog.decks_by_id.has("su-starter"):
		result.final_reason = "starter_deck_missing"
		return result

	var controller: MatchController = MatchController.create(
		catalog.cards_by_id,
		(catalog.decks_by_id["us-starter"] as Dictionary).get("cards", []).duplicate(),
		(catalog.decks_by_id["su-starter"] as Dictionary).get("cards", []).duplicate(),
		seed
	)
	if not _run_setup(controller, seed, result):
		return _finish(controller, result)

	var ais := {
		"player": AIPlayer.create(player_difficulty, seed ^ 0x13579bdf),
		"opponent": AIPlayer.create(opponent_difficulty, seed ^ 0x2468ace0),
	}
	var current_turn := controller.state.turn
	var hashes_this_turn := {controller.state_hash(): true}
	var actions_this_turn := 0
	var force_end_turn := false
	var action_limit := maxi(max_turns, 1) * MAX_ACTIONS_PER_TURN

	while controller.state.phase == "action":
		if result.action_count >= action_limit:
			result.final_reason = "action_limit"
			break
		var actor_id: String = controller.state.active_player_id
		var action: GameAction = null
		if force_end_turn:
			action = _legal_end_turn(controller, actor_id)
			if action == null:
				result.final_reason = "deadlock"
				result.diagnostics.append({"code": "no_legal_end_turn", "actor_id": actor_id})
				break
			force_end_turn = false
		else:
			var ai: AIPlayer = ais[actor_id]
			action = ai.choose_action(controller, actor_id)
			_record_visited_nodes(result, actor_id, ai.visited_nodes)
			_record_search_metrics(result, actor_id, ai.search_metrics)
			if action == null or action.type.is_empty():
				action = _legal_end_turn(controller, actor_id)
				if action == null:
					result.final_reason = "deadlock"
					result.diagnostics.append({"code": "no_ai_action_or_legal_end_turn", "actor_id": actor_id})
					break

		if action.type == "end_turn" and controller.state.turn >= max_turns:
			result.final_reason = "turn_limit"
			break
		if not _submit(controller, action, result):
			controller.abort_invalid("ai_action_rejected", result.diagnostics.back())
			result.final_reason = "illegal_action"
			break
		if controller.state.phase != "action":
			break

		if controller.state.turn != current_turn:
			current_turn = controller.state.turn
			hashes_this_turn = {controller.state_hash(): true}
			actions_this_turn = 0
			continue
		actions_this_turn += 1
		var current_hash := controller.state_hash()
		if hashes_this_turn.has(current_hash):
			force_end_turn = true
			result.diagnostics.append({"code": "repeated_state_hash", "turn": current_turn, "state_hash": current_hash})
		else:
			hashes_this_turn[current_hash] = true
		if actions_this_turn >= MAX_ACTIONS_PER_TURN:
			force_end_turn = true
			result.diagnostics.append({"code": "action_guard", "turn": current_turn})

	if result.final_reason.is_empty():
		result.final_reason = "complete" if controller.state.phase == "complete" else "invalid"
	return _finish(controller, result)


static func _new_result(
	seed: int, player_difficulty: String, opponent_difficulty: String, max_turns: int, content_diagnostics: Array
) -> Dictionary:
	return {
		"seed": seed,
		"player_difficulty": player_difficulty,
		"opponent_difficulty": opponent_difficulty,
		"max_turns": max_turns,
		"completed": false,
		"phase": "",
		"winner": "",
		"turns": 0,
		"action_count": 0,
		"illegal_actions": 0,
		"state_hash": "",
		"replay": {},
		"replay_integrity_hash": "",
		"replay_terminal_hash": "",
		"replay_matches": false,
		"max_effect_queue": 0,
		"max_events": 0,
		"max_visited_nodes": {"player": 0, "opponent": 0},
		"total_visited_nodes": {"player": 0, "opponent": 0},
		"max_search_metrics": {"player": _empty_search_metrics(), "opponent": _empty_search_metrics()},
		"total_search_metrics": {"player": _empty_search_metrics(), "opponent": _empty_search_metrics()},
		"final_reason": "",
		"diagnostics": [],
		"content_diagnostics": content_diagnostics,
	}


static func _run_setup(controller: MatchController, seed: int, result: Dictionary) -> bool:
	if not _submit(controller, GameAction.create("start_match", "system", "", [], {}, controller.state.sequence), result):
		return false
	var rng := RandomNumberGenerator.new()
	rng.seed = seed ^ 0x6d2b79f5
	for player_id in ["player", "opponent"]:
		var selection: Array[String] = []
		for card in controller.state.players[player_id].hand:
			if selection.size() < 2 and rng.randi_range(0, 3) == 0:
				selection.append(card.instance_id)
		if not _submit(controller, GameAction.create("mulligan", player_id, "", selection, {}, controller.state.sequence), result):
			return false
	for player_id in ["player", "opponent"]:
		if not _submit(controller, GameAction.create("confirm_mulligan", player_id, "", [], {}, controller.state.sequence), result):
			return false
	return true


static func _legal_end_turn(controller: MatchController, actor_id: String) -> GameAction:
	for action in controller.legal_actions(actor_id):
		if action.type == "end_turn":
			return action
	return null


static func _submit(controller: MatchController, action: GameAction, result: Dictionary) -> bool:
	result.action_count += 1
	var action_result = controller.submit_action(action)
	result.max_events = maxi(int(result.max_events), action_result.events.size())
	result.max_effect_queue = maxi(int(result.max_effect_queue), int(controller.effect_metrics().max_queue_size))
	if action_result.accepted:
		return true
	result.illegal_actions += 1
	result.diagnostics.append({
		"code": "action_rejected",
		"action": _action_data(action),
		"result": {
			"reason_code": action_result.reason_code,
			"message": action_result.message,
		},
		"state_hash": controller.state_hash(),
		"state": controller.state.snapshot_for("system"),
	})
	return false


static func _record_visited_nodes(result: Dictionary, actor_id: String, visited_nodes: int) -> void:
	var maximum: Dictionary = result.max_visited_nodes
	var total: Dictionary = result.total_visited_nodes
	maximum[actor_id] = maxi(int(maximum.get(actor_id, 0)), visited_nodes)
	total[actor_id] = int(total.get(actor_id, 0)) + visited_nodes
	result.max_visited_nodes = maximum
	result.total_visited_nodes = total


static func _record_search_metrics(result: Dictionary, actor_id: String, metrics: Dictionary) -> void:
	var maximums: Dictionary = result.max_search_metrics
	var totals: Dictionary = result.total_search_metrics
	var maximum: Dictionary = maximums[actor_id]
	var total: Dictionary = totals[actor_id]
	for field in SEARCH_METRIC_FIELDS:
		var value := int(metrics.get(field, 0))
		maximum[field] = maxi(int(maximum.get(field, 0)), value)
		total[field] = int(total.get(field, 0)) + value
	maximums[actor_id] = maximum
	totals[actor_id] = total
	result.max_search_metrics = maximums
	result.total_search_metrics = totals


static func _empty_search_metrics() -> Dictionary:
	var metrics := {}
	for field in SEARCH_METRIC_FIELDS:
		metrics[field] = 0
	return metrics


static func _finish(controller: MatchController, result: Dictionary) -> Dictionary:
	result.completed = controller.state.phase == "complete"
	result.phase = controller.state.phase
	result.winner = controller.state.winner_id
	result.turns = controller.state.turn
	result.state_hash = controller.state_hash()
	result.max_effect_queue = maxi(int(result.max_effect_queue), int(controller.effect_metrics().max_queue_size))
	if not controller.invalid_diagnostics.is_empty():
		result.diagnostics.append({"code": "controller_invalid", "details": controller.invalid_diagnostics.duplicate(true)})
	var replay_data: Dictionary = controller.replay_log.to_dict()
	var replayed = ReplayLog.from_dict(replay_data).replay(controller.card_definitions)
	result.replay = replay_data
	result.replay_integrity_hash = str(replay_data.get("integrity_hash", ""))
	result.replay_terminal_hash = replayed.state_hash()
	result.replay_matches = replayed.state_hash() == result.state_hash and replayed.state.winner_id == controller.state.winner_id
	return result


static func _action_data(action: GameAction) -> Dictionary:
	return {
		"type": action.type,
		"actor_id": action.actor_id,
		"source_id": action.source_id,
		"target_ids": action.target_ids.duplicate(),
		"payload": action.payload.duplicate(true),
		"expected_sequence": action.expected_sequence,
	}
