class_name ActionGenerator
extends RefCounted

const CardInstance = preload("res://scripts/core/card_instance.gd")
const CombatRules = preload("res://scripts/core/combat_rules.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const PlayerState = preload("res://scripts/core/player_state.gd")


static func generate(controller, actor_id: String, validate_actions: bool = true) -> Array[GameAction]:
	var actions: Array[GameAction] = []
	if not (controller is Object) or not controller.has_method("clone_for_simulation"):
		return actions
	var state: Variant = controller.get("state")
	if not (state is Object):
		return actions
	var players_value: Variant = state.get("players")
	if not (players_value is Dictionary):
		return actions
	var players: Dictionary = players_value
	if str(state.get("phase")) != "action" or str(state.get("active_player_id")) != actor_id:
		return actions
	if actor_id not in ["player", "opponent"] \
		or players.size() != 2 \
		or not players.has("player") \
		or not players.has("opponent") \
		or not _valid_generation_state(state, players):
		return actions

	var player: PlayerState = players[actor_id]
	for card in player.hand:
		if _requires_unavailable_credit(player, card):
			continue
		if card.category == "Unit":
			for slot in _open_slots(player.support_line):
				for target_values in _target_variants(controller, actor_id, card, "deploy"):
					_append_if_legal(actions, controller, GameAction.create(
						"deploy_unit", actor_id, card.instance_id, _string_ids(target_values),
						{"support_slot": slot}, int(state.get("sequence"))
					), validate_actions)
		elif card.category == "Order":
			for target_values in _target_variants(controller, actor_id, card, "play_order"):
				_append_if_legal(actions, controller, GameAction.create(
						"play_order", actor_id, card.instance_id, _string_ids(target_values), {}, int(state.get("sequence"))
					), validate_actions)
		elif card.category == "Countermeasure":
			_append_if_legal(actions, controller, GameAction.create(
				"toggle_countermeasure", actor_id, card.instance_id, [], {}, int(state.get("sequence"))
			), validate_actions)

	for card in _battlefield_cards(state, player, actor_id):
		if card.zone == "support_line":
			for slot in _open_slots(state.get("frontline")):
				_append_if_legal(actions, controller, GameAction.create(
					"move_unit", actor_id, card.instance_id, [], {"zone": "frontline", "slot": slot}, int(state.get("sequence"))
				), validate_actions)
		for ability_value in card.abilities:
			if not (ability_value is Dictionary):
				continue
			var ability: Dictionary = ability_value
			if str(ability.get("trigger", "")) != "manual":
				continue
			for target_values in _target_variants_for_ability(controller, actor_id, ability):
				_append_if_legal(actions, controller, GameAction.create(
						"activate_ability", actor_id, card.instance_id, _string_ids(target_values),
						{"ability_id": str(ability.get("id", ""))}, int(state.get("sequence"))
					), validate_actions)
		for target_id in CombatRules.legal_targets(state, card.instance_id):
			var target = CombatRules.find_card(state, target_id)
			if not (target is CardInstance):
				continue
			var action_type := "attack_hq" if target.category == "Headquarters" else "attack_unit"
			var payload := {"target_player_id": target.owner_id} if action_type == "attack_hq" else {}
			_append_if_legal(actions, controller, GameAction.create(
				action_type, actor_id, card.instance_id, [target.instance_id], payload, int(state.get("sequence"))
			), validate_actions)

	_append_if_legal(actions, controller, GameAction.create("end_turn", actor_id, "", [], {}, int(state.get("sequence"))), validate_actions)
	return _deduplicate_and_sort(actions)


static func _requires_unavailable_credit(player: PlayerState, card: CardInstance) -> bool:
	if card.category == "Countermeasure" and card.countermeasure_active:
		return false
	return card.category in ["Unit", "Order", "Countermeasure"] and player.credit < card.deployment_cost


static func _valid_generation_state(state, players: Dictionary) -> bool:
	var frontline: Variant = state.get("frontline")
	if not (frontline is Array) or players.is_empty():
		return false
	if not _valid_cards(frontline, true):
		return false
	for player_value in players.values():
		if not (player_value is PlayerState):
			return false
		var player: PlayerState = player_value
		if not (player.headquarters is CardInstance):
			return false
		for collection in [player.deck, player.hand, player.support_line, player.discard, player.active_countermeasures]:
			if not (collection is Array) or not _valid_cards(collection, collection == player.support_line):
				return false
	return true


static func _valid_cards(cards: Array, allow_null: bool) -> bool:
	for card in cards:
		if card == null and allow_null:
			continue
		if not (card is CardInstance):
			return false
	return true


static func _append_if_legal(actions: Array[GameAction], controller, action: GameAction, validate_action: bool) -> void:
	if not validate_action:
		actions.append(action)
		return
	var clone = controller.clone_for_simulation(action.actor_id)
	if clone is Object and clone.has_method("submit_action") and clone.submit_action(action).accepted:
		actions.append(action)


static func _target_variants(controller, actor_id: String, card: CardInstance, trigger: String) -> Array[Array]:
	var direct_abilities: Array = []
	for ability_value in card.abilities:
		if ability_value is Dictionary and str(ability_value.get("trigger", "")) == trigger:
			direct_abilities.append(ability_value)
	return _target_variants_for_abilities(controller, actor_id, direct_abilities)


static func _target_variants_for_ability(controller, actor_id: String, ability: Dictionary) -> Array[Array]:
	return _target_variants_for_abilities(controller, actor_id, [ability])


static func _target_variants_for_abilities(controller, actor_id: String, abilities: Array) -> Array[Array]:
	var counts := {}
	var candidates: Array[String] = []
	var candidates_initialized := false
	for ability_value in abilities:
		if not (ability_value is Dictionary):
			continue
		var ability: Dictionary = ability_value
		var target_value: Variant = ability.get("target", {})
		if not (target_value is Dictionary):
			return []
		var target: Dictionary = target_value
		var selector := str(target.get("selector", "action_targets"))
		if not _requires_action_targets(selector):
			continue
		counts[int(target.get("count", 1))] = true
		var selector_candidates := _selector_candidates(controller, actor_id, selector)
		candidates = selector_candidates if not candidates_initialized else _intersection(candidates, selector_candidates)
		candidates_initialized = true
	if counts.is_empty():
		return [[]]
	if counts.size() != 1:
		return []
	return _combinations(candidates, int(counts.keys()[0]))


static func _requires_action_targets(selector: String) -> bool:
	return selector in ["action_targets", "enemy_unit_or_hq", "enemy_unit", "friendly_unit", "adjacent_enemy_units"]


static func _selector_candidates(controller, actor_id: String, selector: String) -> Array[String]:
	if selector == "adjacent_enemy_units":
		return _enemy_battlefield_ids(controller.get("state"), actor_id)
	return _public_target_ids(controller.get("state"))


static func _public_target_ids(state) -> Array[String]:
	var ids: Array[String] = []
	var players: Dictionary = state.get("players")
	for player_value in players.values():
		var player: PlayerState = player_value
		ids.append(player.headquarters.instance_id)
		for card in player.support_line:
			if card != null:
				ids.append(card.instance_id)
	for card in state.get("frontline"):
		if card != null:
			ids.append(card.instance_id)
	ids.sort()
	return ids


static func _enemy_battlefield_ids(state, actor_id: String) -> Array[String]:
	var ids: Array[String] = []
	var players: Dictionary = state.get("players")
	for player_id in players:
		if str(player_id) == actor_id:
			continue
		var player: PlayerState = players[player_id]
		for card in player.support_line:
			if card != null and card.category == "Unit":
				ids.append(card.instance_id)
	for card in state.get("frontline"):
		if card != null and card.owner_id != actor_id and card.category == "Unit":
			ids.append(card.instance_id)
	ids.sort()
	return ids


static func _intersection(left: Array[String], right: Array[String]) -> Array[String]:
	var allowed := {}
	for value in right:
		allowed[value] = true
	var result: Array[String] = []
	for value in left:
		if allowed.has(value):
			result.append(value)
	return result


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


static func _battlefield_cards(state, player: PlayerState, actor_id: String) -> Array[CardInstance]:
	var cards: Array[CardInstance] = []
	for card in player.support_line:
		if card != null:
			cards.append(card)
	for card in state.get("frontline"):
		if card != null and card.owner_id == actor_id:
			cards.append(card)
	return cards


static func _open_slots(cards: Array) -> Array[int]:
	var slots: Array[int] = []
	for slot in range(cards.size()):
		if cards[slot] == null:
			slots.append(slot)
	return slots


static func _string_ids(values: Array) -> Array[String]:
	var ids: Array[String] = []
	for value in values:
		if not (value is String):
			return []
		ids.append(value)
	return ids


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
	return JSON.stringify(_canonicalize([
		action.type,
		action.source_id,
		action.target_ids,
		action.payload,
	]))


static func _canonicalize(value: Variant) -> Variant:
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
