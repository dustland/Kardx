extends RefCounted

const ContentCatalog = preload("res://scripts/content/content_catalog.gd")
const ASSET_DIRECTORY := "res://game_assets/generated_cards"
const EXPECTED_FILES := [
	"su-armor.png",
	"su-artillery.png",
	"su-bomber.png",
	"su-fighter.png",
	"su-infantry.png",
	"su-support.png",
	"us-armor.png",
	"us-artillery.png",
	"us-bomber.png",
	"us-fighter.png",
	"us-infantry.png",
	"us-support.png",
]
const EXPECTED_CARD_IMAGES := {
	"us-hq": "us-support.png",
	"us-rifle-platoon": "us-infantry.png",
	"us-combat-engineers": "us-support.png",
	"us-field-hospital": "us-support.png",
	"us-supply-column": "us-support.png",
	"us-forward-observers": "us-artillery.png",
	"us-p40-patrol": "us-fighter.png",
	"us-rapid-resupply": "us-support.png",
	"us-signal-watch": "us-support.png",
	"us-ranger-company": "us-infantry.png",
	"us-armored-group": "us-armor.png",
	"us-field-battery": "us-artillery.png",
	"us-emergency-repairs": "us-support.png",
	"us-tank-hunters": "us-artillery.png",
	"us-b25-strike-group": "us-bomber.png",
	"us-air-superiority": "us-fighter.png",
	"us-combined-arms": "us-infantry.png",
	"su-hq": "su-support.png",
	"su-guards-rifle": "su-infantry.png",
	"su-siberian-volunteers": "su-infantry.png",
	"su-combat-sappers": "su-support.png",
	"su-medical-battalion": "su-support.png",
	"su-rail-convoy": "su-support.png",
	"su-partisan-scouts": "su-support.png",
	"su-massed-assault": "su-infantry.png",
	"su-maskirovka": "su-support.png",
	"su-t34-spearhead": "su-armor.png",
	"su-heavy-breakthrough": "su-armor.png",
	"su-katyusha-battery": "su-artillery.png",
	"su-hold-the-line": "su-support.png",
	"su-yak-patrol": "su-fighter.png",
	"su-pe2-bomber-wing": "su-bomber.png",
	"su-deep-battle": "su-armor.png",
	"su-artillery-preparation": "su-artillery.png",
}

static func run(t) -> void:
	var actual_files := _png_files_in(ASSET_DIRECTORY)
	t.assert_eq(actual_files, EXPECTED_FILES, "generated card art has the exact expected PNG file set")

	for filename in EXPECTED_FILES:
		var path := "%s/%s" % [ASSET_DIRECTORY, filename]
		t.assert_true(FileAccess.file_exists(path), "%s exists" % path)
		var image := Image.new()
		var decode_error := image.load_png_from_buffer(FileAccess.get_file_as_bytes(path))
		t.assert_eq(decode_error, OK, "%s decodes" % path)
		if decode_error != OK:
			continue
		t.assert_true(
			image.get_width() >= 1024 and image.get_height() >= 1536,
			"%s is at least 1024x1536" % path,
		)
		var ratio := float(image.get_width()) / float(image.get_height())
		t.assert_true(absf(ratio - (2.0 / 3.0)) < 0.08, "%s is near 2:3" % path)

	var catalog = ContentCatalog.load_from_paths(
		"res://data/cards.json",
		"res://data/abilities.json",
		"res://data/decks.json",
		"res://data/rules.json",
	)
	t.assert_eq(catalog.cards.size(), EXPECTED_CARD_IMAGES.size(), "every starter card has an expected art mapping")
	for card in catalog.cards:
		var card_id: String = card.get("id", "")
		var image_path: String = card.get("image_path", "")
		var expected_path := "%s/%s" % [ASSET_DIRECTORY, EXPECTED_CARD_IMAGES.get(card_id, "")]
		t.assert_true(image_path.begins_with(ASSET_DIRECTORY + "/"), "%s uses generated art" % card_id)
		t.assert_true(FileAccess.file_exists(image_path), "%s art exists" % card_id)
		t.assert_eq(image_path, expected_path, "%s uses its locked nation/role art" % card_id)

static func _png_files_in(path: String) -> Array[String]:
	var files: Array[String] = []
	var directory := DirAccess.open(path)
	if directory == null:
		return files
	for filename in directory.get_files():
		if filename.get_extension().to_lower() == "png":
			files.append(filename)
	files.sort()
	return files
