extends RefCounted

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
