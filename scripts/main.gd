class_name Main
extends Control

const ContentCatalogScript = preload("res://scripts/content/content_catalog.gd")
const ContentValidatorScript = preload("res://scripts/content/content_validator.gd")
const ThemeFactory = preload("res://scripts/ui/theme_factory.gd")
const ContentErrorViewScene = preload("res://scenes/ui/content_error_view.tscn")

const VALID_SCREENS := ["deck_builder", "mulligan", "match", "result"]
const SCREEN_PATHS := {
	"deck_builder": "res://scenes/ui/deck_builder_view.tscn",
	"mulligan": "res://scenes/ui/mulligan_view.tscn",
	"match": "res://scenes/ui/match_view.tscn",
	"result": "res://scenes/ui/result_view.tscn",
}
const CONTENT_PATHS := [
	"res://data/cards.json",
	"res://data/abilities.json",
	"res://data/decks.json",
	"res://data/rules.json",
]

@onready var screen_host: Control = %ScreenHost

var current_screen: Control
var catalog: ContentCatalog
var selected_deck_id := "us-starter"
var difficulty := "standard"
var controller: MatchController
var ai: AIPlayer


func _ready() -> void:
	theme = ThemeFactory.create()
	_validate_and_start()


func show_screen(screen_name: String, payload: Dictionary = {}) -> void:
	assert(screen_name in VALID_SCREENS, "unknown screen: %s" % screen_name)
	_clear_screen()
	var scene_path: String = SCREEN_PATHS[screen_name]
	if not ResourceLoader.exists(scene_path):
		current_screen = _pending_screen(screen_name)
	else:
		var packed_scene: PackedScene = load(scene_path)
		current_screen = packed_scene.instantiate()
	screen_host.add_child(current_screen)
	if current_screen.has_method("initialize"):
		current_screen.initialize(self, payload)


func _validate_and_start() -> void:
	catalog = ContentCatalogScript.load_from_paths(
		CONTENT_PATHS[0], CONTENT_PATHS[1], CONTENT_PATHS[2], CONTENT_PATHS[3]
	)
	var diagnostics: Array[Dictionary] = ContentValidatorScript.validate(catalog)
	if not diagnostics.is_empty():
		_show_content_errors(diagnostics)
		return
	show_screen("deck_builder", {"catalog": catalog, "deck_id": selected_deck_id, "difficulty": difficulty})


func _show_content_errors(diagnostics: Array[Dictionary]) -> void:
	_clear_screen()
	current_screen = ContentErrorViewScene.instantiate()
	screen_host.add_child(current_screen)
	current_screen.initialize(diagnostics, _validate_and_start)


func _clear_screen() -> void:
	if current_screen != null and is_instance_valid(current_screen):
		current_screen.queue_free()
	current_screen = null


func _pending_screen(screen_name: String) -> Control:
	var panel := CenterContainer.new()
	panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var label := Label.new()
	label.text = screen_name.replace("_", " ").capitalize()
	label.add_theme_font_size_override("font_size", 24)
	panel.add_child(label)
	return panel
