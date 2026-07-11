class_name CardInstance
extends RefCounted

const GameConstants = preload("res://scripts/core/game_constants.gd")

enum OperationChain {
	NONE,
	TANK_ADVANCE,
}

var definition_id: String
var instance_id: String
var owner_id: String
var title: String
var category: String
var unit_type: String
var base_attack: int
var current_attack: int
var base_defense: int
var current_defense: int
var deployment_cost: int
var operation_cost: int
var keywords: Array
var abilities: Array
var zone: String = "deck"
var slot: int = -1
var operations_used: int = 0
var operation_chain: OperationChain = OperationChain.NONE
var smokescreen_revealed: bool = false
var deployed_turn: int = -1
var modifiers: Array = []
var statuses: Dictionary = {}
var revealed_to: Dictionary = {}
var face_down: bool = false
var countermeasure_active: bool = false
var countermeasure_activation_cost: int = 0

static func from_definition(definition: Dictionary, card_owner_id: String, card_instance_id: String) -> CardInstance:
	var card: CardInstance = load("res://scripts/core/card_instance.gd").new()
	card.definition_id = str(definition.get("id", ""))
	card.instance_id = card_instance_id
	card.owner_id = card_owner_id
	card.title = str(definition.get("title", ""))
	card.category = str(definition.get("category", ""))
	card.unit_type = str(definition.get("unit_type", definition.get("subtype", "")))
	card.base_attack = int(definition.get("attack", definition.get("baseAttack", 0)))
	card.current_attack = card.base_attack
	card.base_defense = int(definition.get("defense", definition.get("baseDefense", 0)))
	card.current_defense = card.base_defense
	card.deployment_cost = int(definition.get("deployment_cost", definition.get("deploymentCost", 0)))
	card.operation_cost = int(definition.get("operation_cost", definition.get("operationCost", 0)))
	card.keywords = (definition.get("keywords", []) as Array).duplicate(true)
	card.abilities = (definition.get("abilities", []) as Array).duplicate(true)
	return card

static func headquarters(definition_id_value: String, card_owner_id: String, card_instance_id: String) -> CardInstance:
	var card: CardInstance = load("res://scripts/core/card_instance.gd").new()
	card.definition_id = definition_id_value
	card.instance_id = card_instance_id
	card.owner_id = card_owner_id
	card.title = "Headquarters"
	card.category = "Headquarters"
	card.unit_type = ""
	card.base_attack = 0
	card.current_attack = 0
	card.base_defense = GameConstants.HQ_DEFENSE
	card.current_defense = GameConstants.HQ_DEFENSE
	card.deployment_cost = 0
	card.operation_cost = 0
	card.keywords = []
	card.abilities = []
	card.zone = "headquarters"
	return card

func operation_limit() -> int:
	return 2 if has_keyword_or_status("Fury") else 1

func reset_operation_state() -> void:
	operations_used = 0
	operation_chain = OperationChain.NONE

func has_keyword_or_status(keyword_name: String) -> bool:
	for keyword in keywords:
		if str(keyword) == keyword_name:
			return true
	return bool(statuses.get(keyword_name, false))

func add_temporary_modifier(
	attack_delta: int,
	defense_delta: int,
	duration: Variant,
	expires_on_turn_end_player_id: String,
	source_id: String
) -> void:
	modifiers.append({
		"attack_delta": attack_delta,
		"defense_delta": defense_delta,
		"duration": duration,
		"expires_on_turn_end_player_id": expires_on_turn_end_player_id,
		"source_id": source_id,
	})

func expire_temporary_modifiers(turn_player_id: String) -> Array[Dictionary]:
	var remaining: Array = []
	var expired: Array[Dictionary] = []
	for modifier_value in modifiers:
		var modifier: Dictionary = modifier_value
		if str(modifier.get("expires_on_turn_end_player_id", "")) != turn_player_id:
			remaining.append(modifier)
	for index in range(modifiers.size() - 1, -1, -1):
		var modifier: Dictionary = modifiers[index]
		if str(modifier.get("expires_on_turn_end_player_id", "")) != turn_player_id:
			continue
		current_attack -= int(modifier.get("attack_delta", 0))
		current_defense = maxi(0, current_defense - int(modifier.get("defense_delta", 0)))
		expired.append(modifier)
	modifiers = remaining
	return expired

func to_public_dict(reveal: bool) -> Dictionary:
	if not reveal:
		return {"hidden": true, "zone": zone}
	return {
		"instance_id": instance_id,
		"definition_id": definition_id,
		"title": title,
		"category": category,
		"unit_type": unit_type,
		"attack": current_attack,
		"defense": current_defense,
		"operation_cost": operation_cost,
		"operations_used": operations_used,
		"operation_chain": operation_chain,
		"smokescreen_revealed": smokescreen_revealed,
		"deployed_turn": deployed_turn,
		"keywords": keywords.duplicate(true),
		"statuses": statuses.duplicate(true),
		"zone": zone,
		"slot": slot,
		"face_down": face_down,
		"countermeasure_active": countermeasure_active,
		"countermeasure_activation_cost": countermeasure_activation_cost,
	}
