extends RefCounted

const ContentCatalog = preload("res://scripts/content/content_catalog.gd")
const ContentValidator = preload("res://scripts/content/content_validator.gd")

static func run(t) -> void:
	_test_duplicate_diagnostics_are_structured_and_sorted(t)
	_test_clean_catalog_validates(t)
	_test_loader_reports_file_and_root_errors(t)
	_test_schema_mutations_produce_targeted_diagnostics(t)

static func _test_duplicate_diagnostics_are_structured_and_sorted(t) -> void:
	var catalog := ContentCatalog.new()
	catalog.cards = [
		{"id": "duplicate"},
		{"id": "duplicate"},
	]
	catalog.abilities = []
	catalog.decks = []
	catalog.rules = _rules()
	catalog.rebuild_indexes()

	var errors := ContentValidator.validate(catalog)
	t.assert_true(errors.any(func(error): return error.code == "duplicate_card_id"), "duplicate card IDs are reported")
	t.assert_eq(_paths_for(errors, "duplicate_card_id"), ["cards[0].id", "cards[1].id"], "every duplicate card path is reported")
	t.assert_true(errors.all(func(error): return error.has("code") and error.has("path") and error.has("message")), "diagnostics have structured fields")
	t.assert_eq(errors, _sorted(errors), "diagnostics are sorted by path, code, and message")

static func _test_clean_catalog_validates(t) -> void:
	var catalog := ContentCatalog.new()
	catalog.cards = [_card("us-rifle", ["opening-fire"]), _headquarters("us-hq")]
	catalog.abilities = [_ability("opening-fire")]
	catalog.decks = [{"id": "starter", "cards": ["us-rifle", "us-hq"]}]
	catalog.rules = _rules()
	catalog.rebuild_indexes()

	t.assert_eq(ContentValidator.validate(catalog), [], "well-formed in-memory content validates")

static func _test_loader_reports_file_and_root_errors(t) -> void:
	var abilities_path := "user://content_catalog_bad_abilities.json"
	var decks_path := "user://content_catalog_empty_decks.json"
	var rules_path := "user://content_catalog_bad_rules.json"
	var null_cards_path := "user://content_catalog_null_cards.json"
	_write(abilities_path, "{}")
	_write(decks_path, "[]")
	_write(rules_path, "[]")
	_write(null_cards_path, "null")

	var catalog = ContentCatalog.load_from_paths("user://content_catalog_missing_cards.json", abilities_path, decks_path, rules_path)
	var errors := ContentValidator.validate(catalog)
	t.assert_true(_codes(errors).has("missing_file"), "missing content files become diagnostics")
	t.assert_eq(_paths_for(errors, "invalid_root_type"), [abilities_path, rules_path], "wrong JSON root types are diagnosed deterministically")
	var null_catalog = ContentCatalog.load_from_paths(null_cards_path, abilities_path, decks_path, _rules_path())
	t.assert_true(_paths_for(ContentValidator.validate(null_catalog), "invalid_root_type").has(null_cards_path), "null JSON roots become diagnostics")
	_cleanup([abilities_path, decks_path, rules_path, null_cards_path])

static func _test_schema_mutations_produce_targeted_diagnostics(t) -> void:
	var catalog := ContentCatalog.new()
	var invalid_card := _card("invalid", ["unknown-ability"])
	invalid_card.nation = "Unknown"
	invalid_card.unit_type = "Naval"
	invalid_card.rarity = "Mythic"
	invalid_card.deployment_cost = -1
	invalid_card.attack = "two"
	invalid_card.image_path = "res://missing-art.png"
	var invalid_order := _card("invalid-order", [])
	invalid_order.category = "Order"
	invalid_order.unit_type = "Infantry"
	invalid_order.defense = 1
	var invalid_countermeasure := _card("invalid-countermeasure", [])
	invalid_countermeasure.category = "Countermeasure"
	invalid_countermeasure.attack = 1
	var invalid_hq := _headquarters("invalid-hq")
	invalid_hq.defense = 19
	catalog.cards = [invalid_card, invalid_order, invalid_countermeasure, invalid_hq]
	catalog.abilities = [
		_ability("broken"),
		_ability("broken"),
		{
			"id": "malformed",
			"trigger": "unknown_trigger",
			"conditions": [],
			"target": {"selector": "none", "count": 0},
			"effects": [{"type": "damage"}, {"type": "retreat"}, {"type": "create", "definition_id": "unknown-definition"}],
		},
	]
	catalog.decks = [{"id": "bad-deck", "cards": ["invalid", 4, "missing-card"]}, {"id": "no-cards"}]
	catalog.rules = _rules()
	catalog.rules.max_credits = 99
	catalog.rules.display_terms.credit = "Credits"
	catalog.rebuild_indexes()

	var codes := _codes(ContentValidator.validate(catalog))
	for code in [
		"invalid_nation", "invalid_unit_type", "invalid_rarity", "negative_value", "invalid_type",
		"missing_ability_reference", "missing_image", "invalid_order_structure", "invalid_countermeasure_structure",
		"invalid_headquarters_structure", "duplicate_ability_id", "invalid_trigger", "invalid_conditions",
		"missing_effect_field", "impossible_target_effect", "invalid_deck_entry", "unknown_deck_card",
		"missing_deck_cards", "unknown_effect_definition", "rules_constant_mismatch", "rules_display_term_mismatch",
	]:
		t.assert_true(codes.has(code), "schema mutation reports %s" % code)

static func _write(path: String, contents: String) -> void:
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_string(contents)

static func _cleanup(paths: Array) -> void:
	for path_value in paths:
		DirAccess.remove_absolute(ProjectSettings.globalize_path(str(path_value)))

static func _rules_path() -> String:
	return "res://data/rules.json"

static func _codes(errors: Array) -> Array:
	var codes: Array = []
	for error in errors:
		codes.append(error.code)
	return codes

static func _paths_for(errors: Array, code: String) -> Array:
	var paths: Array = []
	for error in errors:
		if error.code == code:
			paths.append(error.path)
	return paths

static func _card(id: String, abilities: Array) -> Dictionary:
	return {
		"id": id,
		"title": "Rifle Platoon",
		"nation": "UnitedStates",
		"category": "Unit",
		"unit_type": "Infantry",
		"rarity": "Standard",
		"deployment_cost": 2,
		"operation_cost": 1,
		"attack": 2,
		"defense": 3,
		"keywords": [],
		"abilities": abilities,
		"image_path": "res://scripts/core/game_constants.gd",
	}

static func _headquarters(id: String) -> Dictionary:
	return {
		"id": id,
		"title": "Command Post",
		"nation": "UnitedStates",
		"category": "Headquarters",
		"unit_type": "",
		"rarity": "Standard",
		"deployment_cost": 0,
		"operation_cost": 0,
		"attack": 0,
		"defense": 20,
		"keywords": [],
		"abilities": [],
		"image_path": "res://scripts/core/game_constants.gd",
	}

static func _ability(id: String) -> Dictionary:
	return {
		"id": id,
		"trigger": "play_order",
		"conditions": {},
		"target": {"selector": "enemy_unit", "count": 1},
		"effects": [{"type": "damage", "amount": 2}],
	}

static func _rules() -> Dictionary:
	return {
		"deck_size": 40,
		"playable_deck_size": 39,
		"max_hand_size": 9,
		"max_credits": 12,
		"copy_limits": {"Standard": 4, "Limited": 3, "Special": 2, "Elite": 1},
		"display_terms": {"credit": "Credit", "frontline": "Frontline", "support_line": "Support Line"},
	}

static func _sorted(errors: Array) -> Array:
	var sorted := errors.duplicate(true)
	sorted.sort_custom(func(left, right):
		var left_key := "%s\u001f%s\u001f%s" % [left.path, left.code, left.message]
		var right_key := "%s\u001f%s\u001f%s" % [right.path, right.code, right.message]
		return left_key < right_key
	)
	return sorted
