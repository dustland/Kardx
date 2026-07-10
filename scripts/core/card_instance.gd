class_name CardInstance
extends RefCounted

const GameConstants = preload("res://scripts/core/game_constants.gd")

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
var deployed_turn: int = -1
var modifiers: Array = []
var statuses: Dictionary = {}
var face_down: bool = false
var countermeasure_active: bool = false

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

func to_public_dict(reveal: bool) -> Dictionary:
	if not reveal:
		return {"instance_id": instance_id, "hidden": true, "zone": zone}
	return {
		"instance_id": instance_id,
		"definition_id": definition_id,
		"title": title,
		"category": category,
		"unit_type": unit_type,
		"attack": current_attack,
		"defense": current_defense,
		"operation_cost": operation_cost,
		"keywords": keywords.duplicate(),
		"statuses": statuses.duplicate(true),
		"zone": zone,
		"slot": slot,
	}
