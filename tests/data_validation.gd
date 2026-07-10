extends SceneTree

func _init() -> void:
	var cards = _load_array("res://data/cards.json")
	var abilities = _load_array("res://data/abilities.json")
	assert(not cards.is_empty(), "cards.json must contain cards")
	assert(not abilities.is_empty(), "abilities.json must contain abilities")
	for card in cards:
		assert(card.has("id"), "every card needs an id")
		assert(card.has("title"), "every card needs a title")
	for ability in abilities:
		assert(ability.has("id"), "every ability needs an id")
	print("Validated %d cards and %d abilities" % [cards.size(), abilities.size()])
	quit()

func _load_array(path: String) -> Array:
	var value = JSON.parse_string(FileAccess.get_file_as_string(path))
	return value if value is Array else []
