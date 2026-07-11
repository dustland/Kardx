extends SceneTree

const ContentCatalog = preload("res://scripts/content/content_catalog.gd")
const ContentValidator = preload("res://scripts/content/content_validator.gd")
const DeckValidator = preload("res://scripts/core/deck_validator.gd")


func _init() -> void:
	var catalog = ContentCatalog.load_from_paths(
		"res://data/cards.json",
		"res://data/abilities.json",
		"res://data/decks.json",
		"res://data/rules.json"
	)
	var diagnostics := ContentValidator.validate(catalog)
	assert(catalog.cards.size() == 34, "expected exactly 34 original definitions")
	assert(catalog.decks.size() == 2, "expected two starter decks")
	assert(diagnostics.is_empty(), "content diagnostics: %s" % [diagnostics])
	for deck_id in ["us-starter", "su-starter"]:
		assert(catalog.decks_by_id.has(deck_id), "missing %s deck" % deck_id)
		assert(DeckValidator.validate(catalog.decks_by_id[deck_id].get("cards", []), catalog.cards_by_id).valid, "%s deck must validate" % deck_id)
	print("Validated %d cards, %d decks, and %d abilities" % [catalog.cards.size(), catalog.decks.size(), catalog.abilities.size()])
	quit()
