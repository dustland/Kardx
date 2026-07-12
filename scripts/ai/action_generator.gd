class_name ActionGenerator
extends RefCounted

const CombatRules = preload("res://scripts/core/combat_rules.gd")
const GameAction = preload("res://scripts/core/game_action.gd")


static func generate(controller, actor_id: String) -> Array[GameAction]:
	var actions: Array[GameAction] = []
	if controller == null or controller.state == null:
		return actions
	if controller.state.phase != "action" or controller.state.active_player_id != actor_id:
		return actions
	if not controller.state.players.has(actor_id):
		return actions

	var player = controller.state.players[actor_id]
	for card in player.hand:
		if card.category == "Unit":
			for slot in _open_slots(player.support_line):
				for target_ids in _target_variants(controller, actor_id, card, "deploy"):
					_append_if_legal(actions, controller, GameAction.create(
						"deploy_unit", actor_id, card.instance_id, target_ids,
						{"support_slot": slot}, controller.state.sequence
					))
		elif card.category == "Order":
			for target_ids in _target_variants(controller, actor_id, card, "play_order"):
				_append_if_legal(actions, controller, GameAction.create(
					"play_order", actor_id, card.instance_id, target_ids, {}, controller.state.sequence
				))
		elif card.category == "Countermeasure":
			_append_if_legal(actions, controller, GameAction.create(
				"toggle_countermeasure", actor_id, card.instance_id, [], {}, controller.state.sequence
			))

	for card in _battlefield_cards(controller, actor_id):
		if card.zone == "support_line":
			for slot in _open_slots(controller.state.frontline):
				_append_if_legal(actions, controller, GameAction.create(
					"move_unit", actor_id, card.instance_id, [], {"zone": "frontline", "slot": slot}, controller.state.sequence
				))
		for ability_value in card.abilities:
			var ability: Dictionary = ability_value
			if str(ability.get("trigger", "")) != "manual":
				continue
			for target_ids in _target_variants_for_ability(controller, actor_id, ability):
				_append_if_legal(actions, controller, GameAction.create(
					"activate_ability", actor_id, card.instance_id, target_ids,
					{"ability_id": str(ability.get("id", ""))}, controller.state.sequence
				))
		for target_id in CombatRules.legal_targets(controller.state, card.instance_id):
			var target = CombatRules.find_card(controller.state, target_id)
			if target == null:
				continue
			var action_type := "attack_hq" if target.category == "Headquarters" else "attack_unit"
			var payload := {"target_player_id": target.owner_id} if action_type == "attack_hq" else {}
			_append_if_legal(actions, controller, GameAction.create(
				action_type, actor_id, card.instance_id, [target.instance_id], payload, controller.state.sequence
			))

	_append_if_legal(actions, controller, GameAction.create("end_turn", actor_id, "", [], {}, controller.state.sequence))
	return _deduplicate_and_sort(actions)


static func _append_if_legal(actions: Array[GameAction], controller, action: GameAction) -> void:
	var clone = controller.clone_for_simulation(action.actor_id)
	if clone != null and clone.submit_action(action).accepted:
		actions.append(action)


static func _target_variants(controller, actor_id: String, card, trigger: String) -> Array[Array]:
	var direct_abilities: Array = []
	for ability_value in card.abilities:
		var ability: Dictionary = ability_value
		if str(ability.get("trigger", "")) == trigger:
			direct_abilities.append(ability)
	return _target_variants_for_abilities(controller, actor_id, direct_abilities)


static func _target_variants_for_ability(controller, actor_id: String, ability: Dictionary) -> Array[Array]:
	return _target_variants_for_abilities(controller, actor_id, [ability])


static func _target_variants_for_abilities(controller, actor_id: String, abilities: Array) -> Array[Array]:
	var counts := {}
	for ability_value in abilities:
		var ability: Dictionary = ability_value
		var target: Dictionary = ability.get("target", {})
		if _requires_action_targets(str(target.get("selector", "action_targets"))):
			counts[int(target.get("count", 1))] = true
	if counts.is_empty():
		return [[]]
	if counts.size() != 1:
		return []
	var target_count: int = int(counts.keys()[0])
	var candidates := _public_target_ids(controller, actor_id)
	return _combinations(candidates, target_count)


static func _requires_action_targets(selector: String) -> bool:
	return selector in ["action_targets", "enemy_unit_or_hq", "enemy_unit", "friendly_unit"]


static func _public_target_ids(controller, actor_id: String) -> Array[String]:
	var ids: Array[String] = []
	for player_id in controller.state.players:
		var player = controller.state.players[player_id]
		ids.append(player.headquarters.instance_id)
		for card in player.support_line:
			if card != null:
				ids.append(card.instance_id)
	for card in controller.state.frontline:
		if card != null:
			ids.append(card.instance_id)
	ids.sort()
	return ids


static func _combinations(values: Array[String], count: int) -> Array[Array]:
	var combinations: Array[Array] = []
	if count < 0 or count > values.size():
		return combinations
	_build_combinations(values, count, 0, [], combinations)
	return combinations


static func _build_combinations(values: Array[String], remaining: int, start: int, current: Array[String], output: Array[Array]) -> void:
	if remaining == 0:
		output.append(current.duplicate())
		return
	for index in range(start, values.size() - remaining + 1):
		var next := current.duplicate()
		next.append(values[index])
		_build_combinations(values, remaining - 1, index + 1, next, output)


static func _battlefield_cards(controller, actor_id: String) -> Array:
	var cards: Array = []
	var player = controller.state.players[actor_id]
	for card in player.support_line:
		if card != null:
			cards.append(card)
	for card in controller.state.frontline:
		if card != null and card.owner_id == actor_id:
			cards.append(card)
	return cards


static func _open_slots(cards: Array) -> Array[int]:
	var slots: Array[int] = []
	for slot in range(cards.size()):
		if cards[slot] == null:
			slots.append(slot)
	return slots


static func _deduplicate_and_sort(actions: Array[GameAction]) -> Array[GameAction]:
	var unique := {}
	for action in actions:
		unique[_action_key(action)] = action
	var sorted: Array[GameAction] = []
	for key in unique.keys():
		sorted.append(unique[key])
	sorted.sort_custom(func(left: GameAction, right: GameAction) -> bool: return _action_key(left) < _action_key(right))
	return sorted


static func _action_key(action: GameAction) -> String:
	return "%s|%s|%s|%s" % [
		action.type,
		action.source_id,
		",".join(action.target_ids),
		JSON.stringify(_canonicalize(action.payload)),
	]


static func _canonicalize(value: Variant) -> Variant:
	if value is Dictionary:
		var keys: Array = value.keys()
		keys.sort_custom(func(left, right) -> bool: return str(left) < str(right))
		var canonical := {}
		for key in keys:
			canonical[str(key)] = _canonicalize(value[key])
		return canonical
	if value is Array:
		var canonical_values: Array = []
		for item in value:
			canonical_values.append(_canonicalize(item))
		return canonical_values
	return {"type": typeof(value), "value": value}
