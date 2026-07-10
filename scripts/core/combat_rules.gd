class_name CombatRules
extends RefCounted

const CardInstance = preload("res://scripts/core/card_instance.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")

const TYPE_RULES := {
	"Infantry": {
		"long_range": false,
		"counterattack": true,
		"guard_restricted": true,
		"tank_advance": false,
	},
	"Tank": {
		"long_range": false,
		"counterattack": true,
		"guard_restricted": true,
		"tank_advance": true,
	},
	"Artillery": {
		"long_range": true,
		"counterattack": false,
		"guard_restricted": false,
		"tank_advance": false,
	},
	"Fighter": {
		"long_range": true,
		"counterattack": true,
		"guard_restricted": true,
		"tank_advance": false,
	},
	"Bomber": {
		"long_range": true,
		"counterattack": false,
		"guard_restricted": false,
		"tank_advance": false,
	},
}


static func legal_targets(state, attacker_id: String) -> Array[String]:
	var targets: Array[String] = []
	var attacker: CardInstance = find_card(state, attacker_id)
	if attacker == null or not _validate_operation(state, attacker).valid:
		return targets
	var defender_id := _other_player_id(attacker.owner_id)
	if not state.players.has(defender_id):
		return targets
	var defender = state.players[defender_id]
	for card in state.frontline:
		if card != null and validate_unit_attack(state, attacker_id, card.instance_id).valid:
			targets.append(card.instance_id)
	for card in defender.support_line:
		if card != null and validate_unit_attack(state, attacker_id, card.instance_id).valid:
			targets.append(card.instance_id)
	if validate_hq_attack(state, attacker_id, defender_id).valid:
		targets.append(defender.headquarters.instance_id)
	return targets


static func can_move_to_frontline(state, unit_id: String) -> Dictionary:
	var unit: CardInstance = find_card(state, unit_id)
	if unit == null:
		return _invalid("card_not_found")
	var operation := _validate_operation(state, unit)
	if not operation.valid:
		return operation
	if unit.zone != "support_line":
		return _invalid("invalid_origin")
	if not state.frontline_controller_id.is_empty() and state.frontline_controller_id != unit.owner_id:
		return _invalid("enemy_controls_frontline")
	if _occupied_count(state.frontline) >= GameConstants.FRONTLINE_SLOTS:
		return _invalid("frontline_full")
	return _valid()


static func validate_move_to_frontline(state, unit_id: String, slot: int) -> Dictionary:
	var result := can_move_to_frontline(state, unit_id)
	if not result.valid:
		return result
	if slot < 0 or slot >= GameConstants.FRONTLINE_SLOTS:
		return _invalid("invalid_slot")
	if state.frontline[slot] != null:
		return _invalid("slot_occupied")
	return _valid()


static func validate_unit_attack(state, attacker_id: String, defender_id: String) -> Dictionary:
	var attacker: CardInstance = find_card(state, attacker_id)
	if attacker == null:
		return _invalid("card_not_found")
	var operation := _validate_operation(state, attacker)
	if not operation.valid:
		return operation
	var defender: CardInstance = find_card(state, defender_id)
	if defender == null or defender.category != "Unit" or defender.owner_id == attacker.owner_id:
		return _invalid("invalid_target")
	if defender.zone != "support_line" and defender.zone != "frontline":
		return _invalid("invalid_target")
	if not _type_can_target(attacker, defender):
		return _invalid("invalid_target")
	if _is_smokescreened(defender):
		return _invalid("smokescreen")
	if not _passes_guard_adjacency(state, attacker, defender):
		return _invalid("guard_protected")
	return _valid()


static func validate_hq_attack(state, attacker_id: String, defender_player_id: String) -> Dictionary:
	var attacker: CardInstance = find_card(state, attacker_id)
	if attacker == null:
		return _invalid("card_not_found")
	var operation := _validate_operation(state, attacker)
	if not operation.valid:
		return operation
	if not state.players.has(defender_player_id) or defender_player_id == attacker.owner_id:
		return _invalid("invalid_target")
	var rules: Dictionary = TYPE_RULES.get(attacker.unit_type, {})
	if rules.is_empty():
		return _invalid("invalid_unit_type")
	if not bool(rules.long_range):
		if attacker.zone != "frontline" or state.frontline_controller_id != attacker.owner_id:
			return _invalid("invalid_target")
	if state.players[defender_player_id].headquarters.current_defense <= 0:
		return _invalid("invalid_target")
	return _valid()


static func find_card(state, instance_id: String) -> CardInstance:
	for player_id in state.players:
		var player = state.players[player_id]
		if player.headquarters.instance_id == instance_id:
			return player.headquarters
		for collection in [player.deck, player.hand, player.support_line, player.discard]:
			for card in collection:
				if card != null and card.instance_id == instance_id:
					return card
	for card in state.frontline:
		if card != null and card.instance_id == instance_id:
			return card
	return null


static func attack_uses_tank_chain(attacker: CardInstance) -> bool:
	return opens_tank_advance(attacker) \
		and attacker.zone == "frontline" \
		and attacker.operation_chain == CardInstance.OperationChain.TANK_ADVANCE


static func opens_tank_advance(unit: CardInstance) -> bool:
	var rules: Dictionary = TYPE_RULES.get(unit.unit_type, {})
	return bool(rules.get("tank_advance", false))


static func receives_counterattack(attacker: CardInstance) -> bool:
	var rules: Dictionary = TYPE_RULES.get(attacker.unit_type, {})
	return bool(rules.get("counterattack", false))


static func is_ambush(card: CardInstance) -> bool:
	return card.has_keyword_or_status("Ambush")


static func reduces_damage(card: CardInstance) -> bool:
	return card.has_keyword_or_status("Heavy Armor")


static func _validate_operation(state, unit: CardInstance) -> Dictionary:
	if state.phase == "complete" or not state.winner_id.is_empty():
		return _invalid("match_complete")
	if state.phase != "action":
		return _invalid("invalid_phase")
	if unit.category != "Unit" or not TYPE_RULES.has(unit.unit_type):
		return _invalid("invalid_unit_type")
	if unit.owner_id != state.active_player_id:
		return _invalid("not_active_player")
	if unit.zone != "support_line" and unit.zone != "frontline":
		return _invalid("invalid_origin")
	if unit.has_keyword_or_status("Pinned"):
		return _invalid("pinned")
	if unit.deployed_turn == state.turn and not unit.has_keyword_or_status("Blitz"):
		return _invalid("deployment_sickness")
	if not attack_uses_tank_chain(unit) and unit.operations_used >= unit.operation_limit():
		return _invalid("operation_limit")
	if not attack_uses_tank_chain(unit) and state.players[unit.owner_id].credit < unit.operation_cost:
		return _invalid("insufficient_credit")
	return _valid()


static func _type_can_target(attacker: CardInstance, defender: CardInstance) -> bool:
	var rules: Dictionary = TYPE_RULES.get(attacker.unit_type, {})
	if bool(rules.get("long_range", false)):
		return true
	if attacker.zone == "support_line":
		return defender.zone == "frontline"
	return attacker.zone == "frontline" and defender.zone == "support_line"


static func _passes_guard_adjacency(state, attacker: CardInstance, defender: CardInstance) -> bool:
	var rules: Dictionary = TYPE_RULES.get(attacker.unit_type, {})
	if not bool(rules.get("guard_restricted", false)) or defender.has_keyword_or_status("Guard"):
		return true
	var line: Array = state.frontline if defender.zone == "frontline" else state.players[defender.owner_id].support_line
	for adjacent_slot in [defender.slot - 1, defender.slot + 1]:
		if adjacent_slot < 0 or adjacent_slot >= line.size():
			continue
		var adjacent = line[adjacent_slot]
		if adjacent != null \
			and adjacent.owner_id == defender.owner_id \
			and adjacent.has_keyword_or_status("Guard"):
			return false
	return true


static func _is_smokescreened(defender: CardInstance) -> bool:
	return defender.has_keyword_or_status("Smokescreen") and not defender.smokescreen_revealed


static func _occupied_count(cards: Array) -> int:
	var count := 0
	for card in cards:
		if card != null:
			count += 1
	return count


static func _other_player_id(player_id: String) -> String:
	return "opponent" if player_id == "player" else "player"


static func _valid() -> Dictionary:
	return {"valid": true}


static func _invalid(code: String) -> Dictionary:
	return {"valid": false, "code": code}
