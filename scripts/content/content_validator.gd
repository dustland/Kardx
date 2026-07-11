class_name ContentValidator
extends RefCounted

const GameConstants = preload("res://scripts/core/game_constants.gd")

const NATIONS := ["UnitedStates", "SovietUnion"]
const CATEGORIES := ["Headquarters", "Unit", "Order", "Countermeasure"]
const UNIT_TYPES := ["Infantry", "Tank", "Artillery", "Fighter", "Bomber"]
const TRIGGERS := [
	"attack", "countermeasure_triggered", "damage", "death", "defend", "deploy", "discard", "draw",
	"frontline_gained", "frontline_lost", "hq_lethal", "manual", "move", "order_played", "play_order",
	"turn_end", "turn_start",
]
const TARGET_SELECTORS := ["none", "self", "action_targets", "enemy_hq", "friendly_hq", "enemy_unit_or_hq", "enemy_unit", "friendly_unit", "friendly_units", "friendly_infantry", "enemy_units", "enemy_air_units", "adjacent_enemy_units", "random_enemy_unit", "random_enemy_hand"]
const EFFECT_TYPES := ["damage", "repair", "buff", "debuff", "status", "draw", "discard", "create", "copy", "destroy", "return", "retreat", "credit", "credit_slots", "replace_event", "reveal"]
const TARGETLESS_EFFECTS := ["credit", "credit_slots", "draw", "create", "replace_event"]
const HQ_SAFE_EFFECTS := ["damage", "repair", "buff", "debuff", "status"]
const UNIT_ONLY_EFFECTS := ["copy", "discard", "destroy", "return", "retreat"]
const UNIT_TARGET_SELECTORS := ["enemy_unit", "random_enemy_unit"]

static func validate(catalog) -> Array[Dictionary]:
	var diagnostics: Array[Dictionary] = []
	for error_value in catalog.load_errors:
		if error_value is Dictionary:
			diagnostics.append((error_value as Dictionary).duplicate(true))
	_append_duplicate_id_diagnostics(diagnostics, catalog.cards, "card")
	_append_duplicate_id_diagnostics(diagnostics, catalog.abilities, "ability")
	_append_duplicate_id_diagnostics(diagnostics, catalog.decks, "deck")
	_validate_cards(diagnostics, catalog.cards, catalog.abilities_by_id)
	_validate_abilities(diagnostics, catalog.abilities, catalog.cards_by_id)
	_validate_decks(diagnostics, catalog.decks, catalog.cards_by_id)
	_validate_rules(diagnostics, catalog.rules)
	_sort(diagnostics)
	return diagnostics

static func _validate_cards(diagnostics: Array[Dictionary], cards: Array, abilities_by_id: Dictionary) -> void:
	for index in range(cards.size()):
		var card_value = cards[index]
		var path := "cards[%d]" % index
		if not card_value is Dictionary:
			_add(diagnostics, "invalid_card_entry", path, "card entry must be an object")
			continue
		var card: Dictionary = card_value
		_validate_required_fields(diagnostics, card, path, {
			"id": TYPE_STRING, "title": TYPE_STRING, "nation": TYPE_STRING, "category": TYPE_STRING,
			"unit_type": TYPE_STRING, "rarity": TYPE_STRING, "deployment_cost": TYPE_INT,
			"operation_cost": TYPE_INT, "attack": TYPE_INT, "defense": TYPE_INT,
			"keywords": TYPE_ARRAY, "ability_ids": TYPE_ARRAY, "image_path": TYPE_STRING,
		})
		_validate_id(diagnostics, card, path)
		var nation: String = str(card.get("nation", "")) if _has_type(card, "nation", TYPE_STRING) else ""
		var category: String = str(card.get("category", "")) if _has_type(card, "category", TYPE_STRING) else ""
		var unit_type: String = str(card.get("unit_type", "")) if _has_type(card, "unit_type", TYPE_STRING) else ""
		var rarity: String = str(card.get("rarity", "")) if _has_type(card, "rarity", TYPE_STRING) else ""
		if not NATIONS.has(nation):
			_add(diagnostics, "invalid_nation", "%s.nation" % path, "nation must be one of %s" % ", ".join(NATIONS))
		if not CATEGORIES.has(category):
			_add(diagnostics, "invalid_category", "%s.category" % path, "category must be one of %s" % ", ".join(CATEGORIES))
		if category == "Unit" and not UNIT_TYPES.has(unit_type):
			_add(diagnostics, "invalid_unit_type", "%s.unit_type" % path, "unit_type must be one of %s for Units" % ", ".join(UNIT_TYPES))
		if not GameConstants.COPY_LIMITS.has(rarity):
			_add(diagnostics, "invalid_rarity", "%s.rarity" % path, "rarity must be defined by copy limits")
		for field in ["deployment_cost", "operation_cost", "attack", "defense"]:
			var value = card.get(field, null)
			if value is int and value < 0:
				_add(diagnostics, "negative_value", "%s.%s" % [path, field], "%s must be nonnegative" % field)
		_validate_card_structure(diagnostics, card, path, category)
		_validate_card_ability_references(diagnostics, card, path, abilities_by_id)
		_validate_image_path(diagnostics, card, path)

static func _validate_required_fields(diagnostics: Array[Dictionary], entry: Dictionary, path: String, fields: Dictionary) -> void:
	for field_value in fields:
		var field := str(field_value)
		if not entry.has(field):
			_add(diagnostics, "missing_field", "%s.%s" % [path, field], "%s is required" % field)
		elif typeof(entry.get(field)) != int(fields.get(field)):
			_add(diagnostics, "invalid_type", "%s.%s" % [path, field], "%s has the wrong type" % field)

static func _validate_id(diagnostics: Array[Dictionary], entry: Dictionary, path: String) -> void:
	if _has_type(entry, "id", TYPE_STRING) and str(entry.get("id", "")).strip_edges().is_empty():
		_add(diagnostics, "blank_id", "%s.id" % path, "id cannot be empty or whitespace")

static func _validate_card_structure(diagnostics: Array[Dictionary], card: Dictionary, path: String, category: String) -> void:
	if category == "Headquarters":
		if _field_differs(card, "unit_type", "") or _field_differs(card, "deployment_cost", 0) or _field_differs(card, "operation_cost", 0) or _field_differs(card, "attack", 0) or _field_differs(card, "defense", GameConstants.HQ_DEFENSE):
			_add(diagnostics, "invalid_headquarters_structure", path, "Headquarters must have zero costs and attack, empty unit_type, and HQ defense")
	elif category == "Order":
		if _field_differs(card, "unit_type", "") or _field_differs(card, "operation_cost", 0) or _field_differs(card, "attack", 0) or _field_differs(card, "defense", 0):
			_add(diagnostics, "invalid_order_structure", path, "Orders must have zero operation cost and combat stats with no unit_type")
	elif category == "Countermeasure":
		if _field_differs(card, "unit_type", "") or _field_differs(card, "operation_cost", 0) or _field_differs(card, "attack", 0) or _field_differs(card, "defense", 0):
			_add(diagnostics, "invalid_countermeasure_structure", path, "Countermeasures must have zero operation cost and combat stats with no unit_type")

static func _validate_card_ability_references(diagnostics: Array[Dictionary], card: Dictionary, path: String, abilities_by_id: Dictionary) -> void:
	if card.has("abilities"):
		_add(diagnostics, "embedded_abilities_not_allowed", "%s.abilities" % path, "cards must reference abilities through ability_ids")
	var ability_ids_value = card.get("ability_ids", null)
	if not ability_ids_value is Array:
		return
	var paths_by_id := {}
	for index in range(ability_ids_value.size()):
		var ability_id_value = ability_ids_value[index]
		var ability_path := "%s.ability_ids[%d]" % [path, index]
		if not ability_id_value is String:
			_add(diagnostics, "invalid_type", ability_path, "ability id must be a string")
			continue
		var ability_id: String = ability_id_value
		if ability_id.strip_edges().is_empty():
			_add(diagnostics, "blank_ability_reference", ability_path, "ability id cannot be empty or whitespace")
			continue
		if not abilities_by_id.has(ability_id):
			_add(diagnostics, "missing_ability_reference", ability_path, "ability '%s' does not exist" % ability_id)
		if not paths_by_id.has(ability_id):
			paths_by_id[ability_id] = []
		paths_by_id[ability_id].append(ability_path)
	for ability_id_value in paths_by_id:
		var duplicate_paths: Array = paths_by_id[ability_id_value]
		if duplicate_paths.size() > 1:
			for duplicate_path in duplicate_paths:
				_add(diagnostics, "duplicate_ability_reference", duplicate_path, "ability '%s' is referenced more than once" % ability_id_value)

static func _validate_image_path(diagnostics: Array[Dictionary], card: Dictionary, path: String) -> void:
	var image_path_value = card.get("image_path", null)
	if not image_path_value is String or str(image_path_value).is_empty():
		_add(diagnostics, "missing_image_path", "%s.image_path" % path, "image_path is required")
	elif not ResourceLoader.exists(image_path_value):
		_add(diagnostics, "missing_image", "%s.image_path" % path, "image_path does not resolve to a resource")

static func _validate_abilities(diagnostics: Array[Dictionary], abilities: Array, cards_by_id: Dictionary) -> void:
	for index in range(abilities.size()):
		var ability_value = abilities[index]
		var path := "abilities[%d]" % index
		if not ability_value is Dictionary:
			_add(diagnostics, "invalid_ability_entry", path, "ability entry must be an object")
			continue
		var ability: Dictionary = ability_value
		_validate_required_fields(diagnostics, ability, path, {
			"id": TYPE_STRING, "trigger": TYPE_STRING, "conditions": TYPE_DICTIONARY,
			"target": TYPE_DICTIONARY, "effects": TYPE_ARRAY,
		})
		_validate_id(diagnostics, ability, path)
		var trigger: String = str(ability.get("trigger", "")) if _has_type(ability, "trigger", TYPE_STRING) else ""
		if not TRIGGERS.has(trigger):
			_add(diagnostics, "invalid_trigger", "%s.trigger" % path, "trigger is not supported by EffectEngine")
		_validate_conditions(diagnostics, ability.get("conditions", null), "%s.conditions" % path)
		var target_info := _validate_target(diagnostics, ability.get("target", null), "%s.target" % path)
		var effects_value = ability.get("effects", null)
		if not effects_value is Array:
			continue
		if effects_value.is_empty():
			_add(diagnostics, "missing_effects", "%s.effects" % path, "ability needs at least one effect")
		for effect_index in range(effects_value.size()):
			_validate_effect(diagnostics, effects_value[effect_index], "%s.effects[%d]" % [path, effect_index], target_info, cards_by_id)

static func _validate_conditions(diagnostics: Array[Dictionary], conditions: Variant, path: String) -> void:
	if not conditions is Dictionary:
		_add(diagnostics, "invalid_conditions", path, "conditions must be an object")
		return
	for key_value in conditions:
		var key := str(key_value)
		var value = conditions.get(key_value, null)
		if key == "enemy" and not value is bool:
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "enemy condition must be a boolean")
		elif key == "target_owner" and str(value) != "owner":
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "target_owner condition must be owner")
		elif key == "target_unit_type" and not UNIT_TYPES.has(str(value)):
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "target_unit_type must be a supported Unit type")
		elif key == "source_damaged" and not value is bool:
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "source_damaged condition must be a boolean")
		elif key == "source_lacks_status" and (not value is String or str(value).strip_edges().is_empty()):
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "source_lacks_status must be a nonblank status")
		elif key not in ["enemy", "target_owner", "target_unit_type", "source_damaged", "source_lacks_status"]:
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "condition is not supported by EffectEngine")

static func _validate_target(diagnostics: Array[Dictionary], target_value: Variant, path: String) -> Dictionary:
	var target_info := {"selector": "", "count": 0, "valid": false}
	if not target_value is Dictionary:
		return target_info
	if not _has_type(target_value, "selector", TYPE_STRING):
		_add(diagnostics, "missing_target_field", "%s.selector" % path, "target selector is required")
		return target_info
	var selector: String = str(target_value.get("selector", ""))
	target_info["selector"] = selector
	if not TARGET_SELECTORS.has(selector):
		_add(diagnostics, "invalid_target_selector", "%s.selector" % path, "target selector is not supported by EffectEngine")
		return target_info
	var default_count := 0 if selector == "none" else 1
	var count := default_count
	if target_value.has("count"):
		var count_value = target_value.get("count", null)
		if not count_value is int or count_value < 0:
			_add(diagnostics, "invalid_target_count", "%s.count" % path, "target count must be a nonnegative integer")
			return target_info
		count = count_value
	if selector == "none" and count != 0:
		_add(diagnostics, "invalid_target_count", "%s.count" % path, "none selector must have count zero")
		return target_info
	if selector != "none" and count < 1:
		_add(diagnostics, "invalid_target_count", "%s.count" % path, "card target selectors need at least one target")
		return target_info
	if selector in ["enemy_hq", "friendly_hq", "self"] and count != 1:
		_add(diagnostics, "invalid_hq_target_count", "%s.count" % path, "HQ selectors must target exactly one Headquarters")
		return target_info
	target_info["count"] = count
	target_info["valid"] = true
	return target_info

static func _validate_effect(diagnostics: Array[Dictionary], effect_value: Variant, path: String, target_info: Dictionary, cards_by_id: Dictionary) -> void:
	if not effect_value is Dictionary:
		_add(diagnostics, "invalid_effect_entry", path, "effect must be an object")
		return
	var effect: Dictionary = effect_value
	if not _has_type(effect, "type", TYPE_STRING):
		_add(diagnostics, "missing_effect_field", "%s.type" % path, "effect type is required")
		return
	var effect_type: String = str(effect.get("type", ""))
	if not EFFECT_TYPES.has(effect_type):
		_add(diagnostics, "invalid_effect_type", "%s.type" % path, "effect type is not supported by EffectEngine")
		return
	_validate_target_effect_pair(diagnostics, path, effect_type, target_info)
	if effect_type in ["damage", "repair"]:
		_validate_nonnegative_effect_integer(diagnostics, effect, path, "amount")
	elif effect_type in ["credit", "credit_slots"]:
		_validate_effect_integer(diagnostics, effect, path, "amount")
	elif effect_type == "draw":
		_validate_nonnegative_effect_integer(diagnostics, effect, path, "count")
	elif effect_type in ["buff", "debuff"]:
		_validate_modifier(diagnostics, effect, path)
	elif effect_type == "status":
		var status_value = effect.get("status", null)
		if not status_value is String or str(status_value).strip_edges().is_empty():
			_add(diagnostics, "missing_effect_field", "%s.status" % path, "status effect needs a status")
		if effect.has("active") and not effect.get("active") is bool:
			_add(diagnostics, "invalid_type", "%s.active" % path, "status active must be a boolean")
	elif effect_type == "create":
		var definition_id_value = effect.get("definition_id", null)
		if not definition_id_value is String or str(definition_id_value).strip_edges().is_empty():
			_add(diagnostics, "missing_effect_field", "%s.definition_id" % path, "create effect needs a definition_id")
		elif not cards_by_id.has(definition_id_value):
			_add(diagnostics, "unknown_effect_definition", "%s.definition_id" % path, "create definition '%s' does not exist" % definition_id_value)
		elif str(cards_by_id[definition_id_value].get("category", "")) == "Headquarters":
			_add(diagnostics, "invalid_create_definition", "%s.definition_id" % path, "create definition cannot be a Headquarters")
		if effect.has("destination") and (not effect.destination is String or not ["hand", "deck"].has(effect.destination)):
			_add(diagnostics, "invalid_create_destination", "%s.destination" % path, "create destination must be hand or deck")
	elif effect_type == "replace_event" and not _has_type(effect, "changes", TYPE_DICTIONARY):
		_add(diagnostics, "missing_effect_field", "%s.changes" % path, "replace_event needs changes")

static func _validate_target_effect_pair(diagnostics: Array[Dictionary], path: String, effect_type: String, target_info: Dictionary) -> void:
	if not bool(target_info.get("valid", false)):
		return
	var selector: String = str(target_info.get("selector", ""))
	if TARGETLESS_EFFECTS.has(effect_type):
		if selector != "none":
			_add(diagnostics, "invalid_target_effect_pair", path, "%s does not use a card target" % effect_type)
		return
	if selector == "none":
		_add(diagnostics, "invalid_target_effect_pair", path, "%s requires a target" % effect_type)
		return
	if selector in ["enemy_hq", "friendly_hq", "enemy_unit_or_hq", "action_targets"] and not HQ_SAFE_EFFECTS.has(effect_type):
		_add(diagnostics, "invalid_target_effect_pair", path, "%s cannot target a Headquarters" % effect_type)
		return
	if UNIT_ONLY_EFFECTS.has(effect_type) and not UNIT_TARGET_SELECTORS.has(selector):
		_add(diagnostics, "invalid_target_effect_pair", path, "%s requires a unit-safe selector" % effect_type)

static func _validate_modifier(diagnostics: Array[Dictionary], effect: Dictionary, path: String) -> void:
	var meaningful := false
	for field in ["attack", "defense"]:
		if not effect.has(field):
			continue
		var value = effect.get(field, null)
		if not value is int:
			_add(diagnostics, "invalid_type", "%s.%s" % [path, field], "%s must be an integer" % field)
			continue
		if value != 0:
			meaningful = true
	if not meaningful:
		_add(diagnostics, "meaningless_modifier", path, "modifier needs a nonzero attack or defense change")

static func _validate_effect_integer(diagnostics: Array[Dictionary], effect: Dictionary, path: String, field: String) -> void:
	if not effect.has(field):
		_add(diagnostics, "missing_effect_field", "%s.%s" % [path, field], "%s effect needs %s" % [str(effect.get("type", "effect")), field])
	elif not effect.get(field) is int:
		_add(diagnostics, "invalid_type", "%s.%s" % [path, field], "%s must be an integer" % field)

static func _validate_nonnegative_effect_integer(diagnostics: Array[Dictionary], effect: Dictionary, path: String, field: String) -> void:
	_validate_effect_integer(diagnostics, effect, path, field)
	var value = effect.get(field, null)
	if value is int and value < 0:
		_add(diagnostics, "negative_value", "%s.%s" % [path, field], "%s must be nonnegative" % field)

static func _validate_decks(diagnostics: Array[Dictionary], decks: Array, cards_by_id: Dictionary) -> void:
	for index in range(decks.size()):
		var deck_value = decks[index]
		var path := "decks[%d]" % index
		if not deck_value is Dictionary:
			_add(diagnostics, "invalid_deck_entry", path, "deck entry must be an object")
			continue
		var deck: Dictionary = deck_value
		_validate_id(diagnostics, deck, path)
		if not deck.has("id"):
			_add(diagnostics, "missing_field", "%s.id" % path, "id is required")
		elif not _has_type(deck, "id", TYPE_STRING):
			_add(diagnostics, "invalid_type", "%s.id" % path, "id has the wrong type")
		var cards_value = deck.get("cards", null)
		if cards_value == null:
			_add(diagnostics, "missing_deck_cards", "%s.cards" % path, "deck needs a cards array")
			continue
		if not cards_value is Array:
			_add(diagnostics, "invalid_deck_entry", "%s.cards" % path, "deck cards must be an array")
			continue
		for card_index in range(cards_value.size()):
			var card_id_value = cards_value[card_index]
			var card_path := "%s.cards[%d]" % [path, card_index]
			if not card_id_value is String:
				_add(diagnostics, "invalid_deck_entry", card_path, "deck card id must be a string")
			elif str(card_id_value).strip_edges().is_empty():
				_add(diagnostics, "blank_card_reference", card_path, "deck card id cannot be empty or whitespace")
			elif not cards_by_id.has(card_id_value):
				_add(diagnostics, "unknown_deck_card", card_path, "deck card '%s' does not exist" % card_id_value)

static func _validate_rules(diagnostics: Array[Dictionary], rules: Dictionary) -> void:
	var expected := {
		"deck_size": GameConstants.DECK_SIZE,
		"playable_deck_size": GameConstants.PLAYABLE_DECK_SIZE,
		"max_hand_size": GameConstants.MAX_HAND_SIZE,
		"max_credits": GameConstants.MAX_CREDITS,
	}
	for field_value in expected:
		var field := str(field_value)
		var value = rules.get(field, null)
		if value == null:
			_add(diagnostics, "missing_rules_field", "rules.%s" % field, "%s is required" % field)
		elif not value is int:
			_add(diagnostics, "invalid_type", "rules.%s" % field, "%s must be an integer" % field)
		elif value != expected.get(field):
			_add(diagnostics, "rules_constant_mismatch", "rules.%s" % field, "%s must match GameConstants" % field)
	var copy_limits_value = rules.get("copy_limits", null)
	if copy_limits_value == null:
		_add(diagnostics, "missing_rules_field", "rules.copy_limits", "copy_limits is required")
	elif not copy_limits_value is Dictionary:
		_add(diagnostics, "invalid_type", "rules.copy_limits", "copy_limits must be an object")
	elif copy_limits_value != GameConstants.COPY_LIMITS:
		_add(diagnostics, "rules_constant_mismatch", "rules.copy_limits", "copy_limits must match GameConstants")
	var terms := {"credit": "Credit", "frontline": "Frontline", "support_line": "Support Line"}
	var display_terms_value = rules.get("display_terms", null)
	if not display_terms_value is Dictionary:
		_add(diagnostics, "rules_display_term_mismatch", "rules.display_terms", "display_terms must be an object")
		return
	for key_value in terms:
		var key := str(key_value)
		if display_terms_value.get(key, null) != terms.get(key):
			_add(diagnostics, "rules_display_term_mismatch", "rules.display_terms.%s" % key, "%s display term must be %s" % [key, terms.get(key)])

static func _append_duplicate_id_diagnostics(diagnostics: Array[Dictionary], entries: Array, label: String) -> void:
	var paths_by_id := {}
	for index in range(entries.size()):
		var entry_value = entries[index]
		if not entry_value is Dictionary:
			continue
		var entry: Dictionary = entry_value
		var id_value = entry.get("id", null)
		if not id_value is String or str(id_value).strip_edges().is_empty():
			continue
		if not paths_by_id.has(id_value):
			paths_by_id[id_value] = []
		paths_by_id[id_value].append("%ss[%d].id" % [label, index])
	for id_value in paths_by_id:
		var paths: Array = paths_by_id[id_value]
		if paths.size() < 2:
			continue
		for path_value in paths:
			_add(diagnostics, "duplicate_%s_id" % label, path_value, "duplicate %s id '%s'" % [label, id_value])

static func _has_type(entry: Dictionary, field: String, expected_type: int) -> bool:
	return entry.has(field) and typeof(entry.get(field)) == expected_type

static func _field_differs(entry: Dictionary, field: String, expected: Variant) -> bool:
	return not entry.has(field) or entry.get(field) != expected

static func _add(diagnostics: Array[Dictionary], code: String, path: String, message: String) -> void:
	diagnostics.append({"code": code, "path": path, "message": message})

static func _sort(diagnostics: Array[Dictionary]) -> void:
	diagnostics.sort_custom(func(left, right):
		var left_key := "%s\u001f%s\u001f%s" % [left.path, left.code, left.message]
		var right_key := "%s\u001f%s\u001f%s" % [right.path, right.code, right.message]
		return left_key < right_key
	)
