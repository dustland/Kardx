class_name AIPlayer
extends RefCounted

const ActionGenerator = preload("res://scripts/ai/action_generator.gd")
const BoardEvaluator = preload("res://scripts/ai/board_evaluator.gd")
const GameAction = preload("res://scripts/core/game_action.gd")

var difficulty: String = "standard"
var node_budget: int = 256
var max_depth: int = 4
var beam_width: int = 8
var visited_nodes: int = 0
var _rng := RandomNumberGenerator.new()


static func create(difficulty_name: String, seed: int) -> AIPlayer:
	var player: AIPlayer = load("res://scripts/ai/ai_player.gd").new()
	player._configure(difficulty_name)
	player._rng.seed = seed
	return player


func _configure(difficulty_name: String) -> void:
	match difficulty_name.to_lower():
		"easy":
			difficulty = "easy"
			node_budget = 32
			max_depth = 1
			beam_width = 4
		"hard":
			difficulty = "hard"
			node_budget = 1200
			max_depth = 8
			beam_width = 16
		_:
			difficulty = "standard"
			node_budget = 256
			max_depth = 4
			beam_width = 8


func choose_action(controller, actor_id: String) -> GameAction:
	visited_nodes = 0
	var snapshot := _snapshot_for(controller, actor_id)
	if not _is_action_turn(snapshot, actor_id):
		return _null_action()
	var actions: Array[GameAction] = ActionGenerator.generate(controller, actor_id)
	if actions.is_empty():
		return _null_action()
	var end_turn := _end_turn_action(actions)
	var lethal := _confirmed_immediate_lethal(controller, actor_id, snapshot, actions)
	if not lethal.is_empty():
		return _choose_tied_nodes(lethal).first_action
	var baseline := BoardEvaluator.score(snapshot, actor_id)
	var selected: GameAction = _choose_easy(controller, actor_id, baseline, actions) if difficulty == "easy" else _choose_beam(controller, actor_id, baseline, actions)
	if not selected.type.is_empty():
		return selected
	return end_turn if end_turn != null else _null_action()


func _choose_easy(controller, actor_id: String, baseline: float, actions: Array[GameAction]) -> GameAction:
	var candidates: Array[Dictionary] = []
	for action in _ordered_actions(actions):
		if action.type == "end_turn":
			continue
		var node := _simulate(controller, actor_id, action)
		if node.is_empty():
			break
		if float(node.score) <= baseline:
			continue
		node["priority"] = _easy_priority(action, float(node.score) - baseline)
		candidates.append(node)
	if candidates.is_empty():
		return _null_action()
	candidates.sort_custom(func(left: Dictionary, right: Dictionary) -> bool:
		if int(left.priority) != int(right.priority):
			return int(left.priority) < int(right.priority)
		if not is_equal_approx(float(left.score), float(right.score)):
			return float(left.score) > float(right.score)
		return _action_key(left.first_action) < _action_key(right.first_action)
	)
	var best_priority := int(candidates[0].priority)
	var best_score := float(candidates[0].score)
	var tied: Array[Dictionary] = []
	for candidate in candidates:
		if int(candidate.priority) != best_priority or not is_equal_approx(float(candidate.score), best_score):
			break
		tied.append(candidate)
	return _choose_tied_nodes(tied).first_action


func _choose_beam(controller, actor_id: String, baseline: float, actions: Array[GameAction]) -> GameAction:
	var best_nodes: Array[Dictionary] = []
	var best_score := baseline
	var frontier: Array[Dictionary] = []
	for action in _ordered_actions(actions):
		if action.type == "end_turn":
			continue
		var node := _simulate(controller, actor_id, action)
		if node.is_empty():
			break
		node["first_action"] = action
		best_score = _record_best(node, best_score, best_nodes)
		if bool(node.continues):
			frontier.append(node)
	frontier = _top_beam(frontier)
	var depth := 1
	while depth < max_depth and not frontier.is_empty() and visited_nodes < node_budget:
		var next_frontier: Array[Dictionary] = []
		for parent in frontier:
			var branch_actions: Array[GameAction] = ActionGenerator.generate(parent.controller, actor_id)
			for action in _ordered_actions(branch_actions):
				var node := _simulate(parent.controller, actor_id, action)
				if node.is_empty():
					break
				node["first_action"] = parent.first_action
				best_score = _record_best(node, best_score, best_nodes)
				if bool(node.continues):
					next_frontier.append(node)
			if visited_nodes >= node_budget:
				break
		frontier = _top_beam(next_frontier)
		depth += 1
	if best_nodes.is_empty():
		return _null_action()
	return _choose_tied_nodes(best_nodes).first_action


func _record_best(node: Dictionary, best_score: float, best_nodes: Array[Dictionary]) -> float:
	var score := float(node.score)
	if score > best_score and not is_equal_approx(score, best_score):
		best_nodes.clear()
		best_nodes.append(node)
		return score
	if is_equal_approx(score, best_score) and not best_nodes.is_empty():
		best_nodes.append(node)
	return best_score


func _confirmed_immediate_lethal(controller, actor_id: String, snapshot: Dictionary, actions: Array[GameAction]) -> Array[Dictionary]:
	var lethal: Array[Dictionary] = []
	for action in _ordered_actions(actions):
		if action.type != "attack_hq":
			continue
		var node := _simulate(controller, actor_id, action)
		if node.is_empty():
			break
		var result_snapshot: Dictionary = node.snapshot
		if str(result_snapshot.get("winner_id", "")) == actor_id:
			lethal.append(node)
	return lethal


func _simulate(controller, actor_id: String, action: GameAction) -> Dictionary:
	if visited_nodes >= node_budget or not (controller is Object) or not controller.has_method("clone_for_simulation"):
		return {}
	visited_nodes += 1
	var clone = controller.clone_for_simulation(actor_id)
	if not (clone is Object) or not clone.has_method("submit_action"):
		return {}
	var result = clone.submit_action(action)
	if result == null or not bool(result.accepted):
		return {}
	var snapshot := _snapshot_for(clone, actor_id)
	if snapshot.is_empty():
		return {}
	var terminal := _is_terminal(snapshot)
	return {
		"controller": clone,
		"first_action": action,
		"score": BoardEvaluator.score(snapshot, actor_id),
		"snapshot": snapshot,
		"continues": not terminal and action.type != "end_turn" and _is_action_turn(snapshot, actor_id),
	}


func _top_beam(nodes: Array[Dictionary]) -> Array[Dictionary]:
	nodes.sort_custom(func(left: Dictionary, right: Dictionary) -> bool:
		if not is_equal_approx(float(left.score), float(right.score)):
			return float(left.score) > float(right.score)
		return _action_key(left.first_action) < _action_key(right.first_action)
	)
	var limited: Array[Dictionary] = []
	var represented_types := {}
	for node in nodes:
		if limited.size() >= beam_width:
			break
		var action_type := str(node.first_action.type)
		if not represented_types.has(action_type):
			represented_types[action_type] = true
			limited.append(node)
	for node in nodes:
		if limited.size() >= beam_width:
			break
		if not limited.has(node):
			limited.append(node)
	return limited


func _choose_tied_nodes(nodes: Array[Dictionary]) -> Dictionary:
	if nodes.is_empty():
		return {}
	var ordered := nodes.duplicate()
	ordered.sort_custom(func(left: Dictionary, right: Dictionary) -> bool:
		return _action_key(left.first_action) < _action_key(right.first_action)
	)
	return ordered[_rng.randi_range(0, ordered.size() - 1)]


func _ordered_actions(actions: Array[GameAction]) -> Array[GameAction]:
	var ordered := actions.duplicate()
	ordered.sort_custom(func(left: GameAction, right: GameAction) -> bool:
		var left_rank := _action_rank(left)
		var right_rank := _action_rank(right)
		if left_rank != right_rank:
			return left_rank < right_rank
		return _action_key(left) < _action_key(right)
	)
	return ordered


func _action_rank(action: GameAction) -> int:
	match action.type:
		"attack_hq":
			return 0
		"attack_unit":
			return 1
		"activate_ability", "play_order":
			return 2
		"deploy_unit":
			return 3
		"move_unit":
			return 4
		"toggle_countermeasure":
			return 5
		"end_turn":
			return 9
	return 8


func _easy_priority(action: GameAction, delta: float) -> int:
	if action.type == "attack_hq" and delta > 0.0:
		return 0
	if action.type == "attack_unit" and delta > 0.0:
		return 1
	if action.type == "deploy_unit":
		return 2
	return 3 if delta > 0.0 else 8


func _end_turn_action(actions: Array[GameAction]) -> GameAction:
	for action in actions:
		if action.type == "end_turn":
			return action
	return null


func _snapshot_for(controller, actor_id: String) -> Dictionary:
	if not (controller is Object):
		return {}
	var state: Variant = controller.get("state")
	if not (state is Object) or not state.has_method("snapshot_for"):
		return {}
	var snapshot: Variant = state.snapshot_for(actor_id)
	return snapshot if snapshot is Dictionary else {}


func _is_action_turn(snapshot: Dictionary, actor_id: String) -> bool:
	if actor_id not in ["player", "opponent"] or snapshot.is_empty() or _is_terminal(snapshot):
		return false
	var players: Variant = snapshot.get("players", null)
	return players is Dictionary and players.size() == 2 and players.has("player") and players.has("opponent") \
		and str(snapshot.get("phase", "")) == "action" and str(snapshot.get("active_player_id", "")) == actor_id


func _is_terminal(snapshot: Dictionary) -> bool:
	return str(snapshot.get("phase", "")) in ["complete", "invalid"] or not str(snapshot.get("winner_id", "")).is_empty()


func _null_action() -> GameAction:
	return GameAction.create("", "")


func _action_key(action: GameAction) -> String:
	return JSON.stringify(_canonicalize([action.type, action.source_id, action.target_ids, action.payload]))


func _canonicalize(value: Variant) -> Variant:
	if value is Dictionary:
		var entries: Array = []
		for key in value:
			entries.append({"key": _canonicalize(key), "value": _canonicalize(value[key])})
		entries.sort_custom(func(left: Dictionary, right: Dictionary) -> bool:
			return JSON.stringify(left.key) < JSON.stringify(right.key)
		)
		return {"type": typeof(value), "entries": entries}
	if value is Array:
		var items: Array = []
		for item in value:
			items.append(_canonicalize(item))
		return {"type": typeof(value), "items": items}
	return {"type": typeof(value), "value": value}
