class_name CoreCards
extends RefCounted

static func build_valid_fixture() -> Dictionary:
	var definitions := {}
	definitions["us-hq"] = _definition("us-hq", "UnitedStates", "Headquarters", "Elite")
	definitions["su-hq"] = _definition("su-hq", "SovietUnion", "Headquarters", "Elite")
	for index in range(13):
		definitions["us-%02d" % index] = _definition("us-%02d" % index, "UnitedStates", "Unit", "Standard")
		definitions["su-%02d" % index] = _definition("su-%02d" % index, "SovietUnion", "Unit", "Standard")

	return {
		"definitions": definitions,
		"player_deck": _starter_deck("us"),
		"enemy_deck": _starter_deck("su"),
	}

static func _starter_deck(nation_prefix: String) -> Array[String]:
	var deck: Array[String] = []
	for index in range(9):
		for copy_index in range(4):
			deck.append("%s-%02d" % [nation_prefix, index])
	for copy_index in range(3):
		deck.append("%s-09" % nation_prefix)
	deck.append("%s-hq" % nation_prefix)
	return deck

static func _definition(id: String, nation: String, category: String, rarity: String) -> Dictionary:
	return {
		"id": id,
		"title": id,
		"nation": nation,
		"category": category,
		"rarity": rarity,
		"unit_type": "Infantry",
		"attack": 1,
		"defense": 1,
	}
