class_name ContentCatalog
extends RefCounted

var cards: Array = []
var abilities: Array = []
var decks: Array = []
var rules: Dictionary = {}
var cards_by_id: Dictionary = {}
var abilities_by_id: Dictionary = {}
var decks_by_id: Dictionary = {}
var load_errors: Array[Dictionary] = []

static func load_from_paths(cards_path: String, abilities_path: String, decks_path: String, rules_path: String):
	var catalog: Variant = load("res://scripts/content/content_catalog.gd").new()
	catalog.cards = catalog._load_array(cards_path, "cards")
	catalog.abilities = catalog._load_array(abilities_path, "abilities")
	catalog.decks = catalog._load_array(decks_path, "decks")
	catalog.rules = catalog._load_dictionary(rules_path, "rules")
	catalog.rebuild_indexes()
	return catalog

func rebuild_indexes() -> void:
	abilities_by_id = _index_by_id(abilities)
	decks_by_id = _index_by_id(decks)
	cards_by_id = _engine_cards_by_id()

func _load_array(path: String, label: String) -> Array:
	var value: Variant = _load_json(path, label)
	if value is Array:
		return value
	if value != null:
		_add_load_error("invalid_root_type", path, "%s JSON root must be an array" % label)
	return []

func _load_dictionary(path: String, label: String) -> Dictionary:
	var value: Variant = _load_json(path, label)
	if value is Dictionary:
		return value
	if value != null:
		_add_load_error("invalid_root_type", path, "%s JSON root must be an object" % label)
	return {}

func _load_json(path: String, label: String) -> Variant:
	if not FileAccess.file_exists(path):
		_add_load_error("missing_file", path, "%s file does not exist" % label)
		return null
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		_add_load_error("unreadable_file", path, "%s file could not be read" % label)
		return null
	var json := JSON.new()
	if json.parse(file.get_as_text()) != OK:
		_add_load_error("invalid_json", path, "%s JSON is malformed: %s" % [label, json.get_error_message()])
		return null
	var data: Variant = json.data
	if data == null:
		_add_load_error("invalid_root_type", path, "%s JSON root cannot be null" % label)
		return null
	return _normalize_json_numbers(data)

func _normalize_json_numbers(value: Variant) -> Variant:
	if value is float and value == floori(value):
		return int(value)
	if value is Array:
		var normalized: Array = []
		for entry in value:
			normalized.append(_normalize_json_numbers(entry))
		return normalized
	if value is Dictionary:
		var normalized := {}
		for key in value:
			normalized[key] = _normalize_json_numbers(value[key])
		return normalized
	return value

func _index_by_id(entries: Array) -> Dictionary:
	var indexed := {}
	for entry_value in entries:
		if not entry_value is Dictionary:
			continue
		var entry: Dictionary = entry_value
		var id_value = entry.get("id", null)
		if not id_value is String or id_value.strip_edges().is_empty() or indexed.has(id_value):
			continue
		indexed[id_value] = entry.duplicate(true)
	return indexed

func _engine_cards_by_id() -> Dictionary:
	var indexed := {}
	for card_value in cards:
		if not card_value is Dictionary:
			continue
		var card: Dictionary = card_value
		var id_value = card.get("id", null)
		if not id_value is String or id_value.strip_edges().is_empty() or indexed.has(id_value):
			continue
		var definition: Dictionary = card.duplicate(true)
		definition.erase("abilities")
		var resolved_abilities: Array = []
		var ability_ids_value = definition.get("ability_ids", [])
		if ability_ids_value is Array:
			for ability_id_value in ability_ids_value:
				if ability_id_value is String and abilities_by_id.has(ability_id_value):
					resolved_abilities.append((abilities_by_id[ability_id_value] as Dictionary).duplicate(true))
		definition["abilities"] = resolved_abilities
		indexed[id_value] = definition
	return indexed

func _add_load_error(code: String, path: String, message: String) -> void:
	load_errors.append({"code": code, "path": path, "message": message})
