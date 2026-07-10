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
const TARGET_SELECTORS := ["none", "action_targets", "enemy_hq", "friendly_hq", "enemy_unit_or_hq", "enemy_unit", "random_enemy_unit"]
const EFFECT_TYPES := ["damage", "repair", "buff", "debuff", "status", "draw", "discard", "create", "copy", "destroy", "return", "retreat", "credit", "credit_slots", "replace_event"]
const TARGETLESS_EFFECTS := ["credit", "credit_slots", "draw", "create", "replace_event"]

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
			"keywords": TYPE_ARRAY, "abilities": TYPE_ARRAY, "image_path": TYPE_STRING,
		})
		if _has_type(card, "nation", TYPE_STRING) and not NATIONS.has(card.nation):
			_add(diagnostics, "invalid_nation", "%s.nation" % path, "nation must be one of %s" % ", ".join(NATIONS))
		if _has_type(card, "category", TYPE_STRING) and not CATEGORIES.has(card.category):
			_add(diagnostics, "invalid_category", "%s.category" % path, "category must be one of %s" % ", ".join(CATEGORIES))
		if _has_type(card, "unit_type", TYPE_STRING) and card.category == "Unit" and not UNIT_TYPES.has(card.unit_type):
			_add(diagnostics, "invalid_unit_type", "%s.unit_type" % path, "unit_type must be one of %s for Units" % ", ".join(UNIT_TYPES))
		if _has_type(card, "rarity", TYPE_STRING) and not GameConstants.COPY_LIMITS.has(card.rarity):
			_add(diagnostics, "invalid_rarity", "%s.rarity" % path, "rarity must be defined by copy limits")
		for field in ["deployment_cost", "operation_cost", "attack", "defense"]:
			if _has_type(card, field, TYPE_INT) and int(card[field]) < 0:
				_add(diagnostics, "negative_value", "%s.%s" % [path, field], "%s must be nonnegative" % field)
		_validate_card_structure(diagnostics, card, path)
		_validate_card_ability_references(diagnostics, card, path, abilities_by_id)
		_validate_image_path(diagnostics, card, path)

static func _validate_required_fields(diagnostics: Array[Dictionary], entry: Dictionary, path: String, fields: Dictionary) -> void:
	for field_value in fields:
		var field := str(field_value)
		if not entry.has(field):
			_add(diagnostics, "missing_field", "%s.%s" % [path, field], "%s is required" % field)
		elif typeof(entry[field]) != int(fields[field]):
			_add(diagnostics, "invalid_type", "%s.%s" % [path, field], "%s has the wrong type" % field)

static func _validate_card_structure(diagnostics: Array[Dictionary], card: Dictionary, path: String) -> void:
	if not _has_type(card, "category", TYPE_STRING):
		return
	var category: String = card.category
	if category == "Headquarters":
		if _field_differs(card, "unit_type", "") or _field_differs(card, "deployment_cost", 0) or _field_differs(card, "operation_cost", 0) or _field_differs(card, "attack", 0) or _field_differs(card, "defense", GameConstants.HQ_DEFENSE):
			_add(diagnostics, "invalid_headquarters_structure", path, "Headquarters must have zero costs and attack, empty unit_type, and HQ defense")
	elif category == "Order":
		if _field_differs(card, "unit_type", "") or _field_differs(card, "attack", 0) or _field_differs(card, "defense", 0):
			_add(diagnostics, "invalid_order_structure", path, "Orders cannot have unit stats or a unit_type")
	elif category == "Countermeasure":
		if _field_differs(card, "unit_type", "") or _field_differs(card, "attack", 0) or _field_differs(card, "defense", 0):
			_add(diagnostics, "invalid_countermeasure_structure", path, "Countermeasures cannot have unit stats or a unit_type")

static func _validate_card_ability_references(diagnostics: Array[Dictionary], card: Dictionary, path: String, abilities_by_id: Dictionary) -> void:
	if not _has_type(card, "abilities", TYPE_ARRAY):
		return
	for index in range(card.abilities.size()):
		var ability_id_value = card.abilities[index]
		var ability_path := "%s.abilities[%d]" % [path, index]
		if not ability_id_value is String:
			_add(diagnostics, "invalid_type", ability_path, "ability id must be a string")
		elif not abilities_by_id.has(ability_id_value):
			_add(diagnostics, "missing_ability_reference", ability_path, "ability '%s' does not exist" % ability_id_value)

static func _validate_image_path(diagnostics: Array[Dictionary], card: Dictionary, path: String) -> void:
	if not card.has("image_path") or not card.image_path is String or card.image_path.is_empty():
		_add(diagnostics, "missing_image_path", "%s.image_path" % path, "image_path is required")
	elif not ResourceLoader.exists(card.image_path):
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
		if _has_type(ability, "trigger", TYPE_STRING) and not TRIGGERS.has(ability.trigger):
			_add(diagnostics, "invalid_trigger", "%s.trigger" % path, "trigger is not supported by EffectEngine")
		_validate_conditions(diagnostics, ability.get("conditions", null), "%s.conditions" % path)
		_validate_target_and_effects(diagnostics, ability, path, cards_by_id)

static func _validate_conditions(diagnostics: Array[Dictionary], conditions: Variant, path: String) -> void:
	if not conditions is Dictionary:
		_add(diagnostics, "invalid_conditions", path, "conditions must be an object")
		return
	for key_value in conditions:
		var key := str(key_value)
		if key == "enemy" and not conditions[key] is bool:
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "enemy condition must be a boolean")
		elif key == "target_owner" and str(conditions[key]) != "owner":
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "target_owner condition must be owner")
		elif key not in ["enemy", "target_owner"]:
			_add(diagnostics, "invalid_conditions", "%s.%s" % [path, key], "condition is not supported by EffectEngine")

static func _validate_target_and_effects(diagnostics: Array[Dictionary], ability: Dictionary, path: String, cards_by_id: Dictionary) -> void:
	var selector := ""
	if ability.get("target", null) is Dictionary:
		var target: Dictionary = ability.target
		if not _has_type(target, "selector", TYPE_STRING):
			_add(diagnostics, "missing_target_field", "%s.target.selector" % path, "target selector is required")
		else:
			selector = target.selector
			if not TARGET_SELECTORS.has(selector):
				_add(diagnostics, "invalid_target_selector", "%s.target.selector" % path, "target selector is not supported by EffectEngine")
		if not _has_type(target, "count", TYPE_INT) or int(target.get("count", -1)) < 0:
			_add(diagnostics, "invalid_target_count", "%s.target.count" % path, "target count must be a nonnegative integer")
		elif selector == "none" and target.count != 0:
			_add(diagnostics, "invalid_target_count", "%s.target.count" % path, "none selector must have count zero")
	var effects_value = ability.get("effects", null)
	if not effects_value is Array:
		return
	if effects_value.is_empty():
		_add(diagnostics, "missing_effects", "%s.effects" % path, "ability needs at least one effect")
	for index in range(effects_value.size()):
		_validate_effect(diagnostics, effects_value[index], "%s.effects[%d]" % [path, index], selector, cards_by_id)

static func _validate_effect(diagnostics: Array[Dictionary], effect_value: Variant, path: String, selector: String, cards_by_id: Dictionary) -> void:
	if not effect_value is Dictionary:
		_add(diagnostics, "invalid_effect_entry", path, "effect must be an object")
		return
	var effect: Dictionary = effect_value
	if not _has_type(effect, "type", TYPE_STRING):
		_add(diagnostics, "missing_effect_field", "%s.type" % path, "effect type is required")
		return
	var effect_type: String = effect.type
	if not EFFECT_TYPES.has(effect_type):
		_add(diagnostics, "invalid_effect_type", "%s.type" % path, "effect type is not supported by EffectEngine")
		return
	if selector == "none" and not TARGETLESS_EFFECTS.has(effect_type):
		_add(diagnostics, "impossible_target_effect", path, "%s requires a target" % effect_type)
	if TARGETLESS_EFFECTS.has(effect_type) and selector != "none":
		_add(diagnostics, "impossible_target_effect", path, "%s does not use a card target" % effect_type)
	if effect_type in ["damage", "repair", "credit", "credit_slots"]:
		_validate_nonnegative_effect_integer(diagnostics, effect, path, "amount")
	elif effect_type == "draw":
		_validate_nonnegative_effect_integer(diagnostics, effect, path, "count")
	elif effect_type in ["buff", "debuff"]:
		_validate_effect_integer(diagnostics, effect, path, "attack")
		_validate_effect_integer(diagnostics, effect, path, "defense")
	elif effect_type == "status":
		if not _has_type(effect, "status", TYPE_STRING) or effect.status.is_empty():
			_add(diagnostics, "missing_effect_field", "%s.status" % path, "status effect needs a status")
		if effect.has("active") and not effect.active is bool:
			_add(diagnostics, "invalid_type", "%s.active" % path, "status active must be a boolean")
	elif effect_type == "create":
		if not _has_type(effect, "definition_id", TYPE_STRING) or effect.definition_id.is_empty():
			_add(diagnostics, "missing_effect_field", "%s.definition_id" % path, "create effect needs a definition_id")
		elif not cards_by_id.has(effect.definition_id):
			_add(diagnostics, "unknown_effect_definition", "%s.definition_id" % path, "create definition '%s' does not exist" % effect.definition_id)
	elif effect_type == "replace_event" and not _has_type(effect, "changes", TYPE_DICTIONARY):
		_add(diagnostics, "missing_effect_field", "%s.changes" % path, "replace_event needs changes")

static func _validate_effect_integer(diagnostics: Array[Dictionary], effect: Dictionary, path: String, field: String) -> void:
	if not _has_type(effect, field, TYPE_INT):
		_add(diagnostics, "missing_effect_field", "%s.%s" % [path, field], "%s effect needs %s" % [effect.type, field])

static func _validate_nonnegative_effect_integer(diagnostics: Array[Dictionary], effect: Dictionary, path: String, field: String) -> void:
	_validate_effect_integer(diagnostics, effect, path, field)
	if _has_type(effect, field, TYPE_INT) and int(effect[field]) < 0:
		_add(diagnostics, "negative_value", "%s.%s" % [path, field], "%s must be nonnegative" % field)

static func _validate_decks(diagnostics: Array[Dictionary], decks: Array, cards_by_id: Dictionary) -> void:
	for index in range(decks.size()):
		var deck_value = decks[index]
		var path := "decks[%d]" % index
		if not deck_value is Dictionary:
			_add(diagnostics, "invalid_deck_entry", path, "deck entry must be an object")
			continue
		var deck: Dictionary = deck_value
		if not _has_type(deck, "id", TYPE_STRING):
			_add(diagnostics, "missing_field", "%s.id" % path, "id is required")
		if not deck.has("cards"):
			_add(diagnostics, "missing_deck_cards", "%s.cards" % path, "deck needs a cards array")
			continue
		if not deck.cards is Array:
			_add(diagnostics, "invalid_deck_entry", "%s.cards" % path, "deck cards must be an array")
			continue
		for card_index in range(deck.cards.size()):
			var card_id_value = deck.cards[card_index]
			var card_path := "%s.cards[%d]" % [path, card_index]
			if not card_id_value is String:
				_add(diagnostics, "invalid_deck_entry", card_path, "deck card id must be a string")
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
		if not rules.has(field):
			_add(diagnostics, "missing_rules_field", "rules.%s" % field, "%s is required" % field)
		elif rules[field] != expected[field]:
			_add(diagnostics, "rules_constant_mismatch", "rules.%s" % field, "%s must match GameConstants" % field)
	if not rules.has("copy_limits"):
		_add(diagnostics, "missing_rules_field", "rules.copy_limits", "copy_limits is required")
	elif rules.copy_limits != GameConstants.COPY_LIMITS:
		_add(diagnostics, "rules_constant_mismatch", "rules.copy_limits", "copy_limits must match GameConstants")
	var terms := {"credit": "Credit", "frontline": "Frontline", "support_line": "Support Line"}
	if not rules.get("display_terms", null) is Dictionary:
		_add(diagnostics, "rules_display_term_mismatch", "rules.display_terms", "display_terms must be an object")
		return
	for key_value in terms:
		var key := str(key_value)
		if rules.display_terms.get(key, null) != terms[key]:
			_add(diagnostics, "rules_display_term_mismatch", "rules.display_terms.%s" % key, "%s display term must be %s" % [key, terms[key]])

static func _append_duplicate_id_diagnostics(diagnostics: Array[Dictionary], entries: Array, label: String) -> void:
	var paths_by_id := {}
	for index in range(entries.size()):
		var entry_value = entries[index]
		if entry_value is Dictionary and _has_type(entry_value, "id", TYPE_STRING) and not entry_value.id.is_empty():
			var id: String = entry_value.id
			if not paths_by_id.has(id):
				paths_by_id[id] = []
			paths_by_id[id].append("%ss[%d].id" % [label, index])
	for id_value in paths_by_id:
		var id := str(id_value)
		var paths: Array = paths_by_id[id]
		if paths.size() < 2:
			continue
		for path_value in paths:
			_add(diagnostics, "duplicate_%s_id" % label, path_value, "duplicate %s id '%s'" % [label, id])

static func _has_type(entry: Dictionary, field: String, expected_type: int) -> bool:
	return entry.has(field) and typeof(entry[field]) == expected_type

static func _field_differs(entry: Dictionary, field: String, expected: Variant) -> bool:
	return not entry.has(field) or entry[field] != expected

static func _add(diagnostics: Array[Dictionary], code: String, path: String, message: String) -> void:
	diagnostics.append({"code": code, "path": path, "message": message})

static func _sort(diagnostics: Array[Dictionary]) -> void:
	diagnostics.sort_custom(func(left, right):
		var left_key := "%s\u001f%s\u001f%s" % [left.path, left.code, left.message]
		var right_key := "%s\u001f%s\u001f%s" % [right.path, right.code, right.message]
		return left_key < right_key
	)
