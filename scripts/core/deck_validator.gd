class_name DeckValidator
extends RefCounted

const GameConstants = preload("res://scripts/core/game_constants.gd")

static func validate(deck_ids: Array, card_definitions: Dictionary) -> Dictionary:
	var errors: Array[String] = []
	var definitions := _definitions_by_id(card_definitions)
	var copy_counts := {}
	var nation_counts := {}
	var headquarters: Array[Dictionary] = []

	if deck_ids.size() != GameConstants.DECK_SIZE:
		errors.append("deck_size")

	for deck_id_value in deck_ids:
		var deck_id := str(deck_id_value)
		if not definitions.has(deck_id):
			_add_error(errors, "unknown_id")
			continue
		var definition: Dictionary = definitions[deck_id]
		copy_counts[deck_id] = int(copy_counts.get(deck_id, 0)) + 1
		var nation := str(definition.get("nation", ""))
		if not nation.is_empty():
			nation_counts[nation] = int(nation_counts.get(nation, 0)) + 1
		if str(definition.get("category", "")) == "Headquarters":
			headquarters.append(definition)

	for deck_id in copy_counts:
		var definition: Dictionary = definitions[deck_id]
		var limit := int(GameConstants.COPY_LIMITS.get(str(definition.get("rarity", "")), 0))
		if limit == 0 or int(copy_counts[deck_id]) > limit:
			_add_error(errors, "copy_limit")

	if headquarters.size() != 1:
		_add_error(errors, "headquarters_count")

	var main_nation := ""
	var ally_nation := ""
	if headquarters.size() == 1:
		main_nation = str(headquarters[0].get("nation", ""))

	if nation_counts.size() > 2:
		_add_error(errors, "nation_limit")
	if not main_nation.is_empty():
		if int(nation_counts.get(main_nation, 0)) < 28:
			_add_error(errors, "main_nation_minimum")
		for nation_value in nation_counts:
			var nation := str(nation_value)
			if nation == main_nation:
				continue
			if ally_nation.is_empty():
				ally_nation = nation
			if int(nation_counts[nation]) > 12:
				_add_error(errors, "ally_limit")

	return {
		"valid": errors.is_empty(),
		"errors": errors,
		"main_nation": main_nation,
		"ally_nation": ally_nation,
	}

static func _definitions_by_id(card_definitions: Dictionary) -> Dictionary:
	var definitions := {}
	for key in card_definitions:
		var definition_value = card_definitions[key]
		if definition_value is Dictionary:
			var definition: Dictionary = definition_value
			definitions[str(definition.get("id", key))] = definition
	return definitions

static func _add_error(errors: Array[String], code: String) -> void:
	if code not in errors:
		errors.append(code)
