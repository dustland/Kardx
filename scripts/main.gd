class_name Main
extends Control

const ContentCatalogScript = preload("res://scripts/content/content_catalog.gd")
const ContentValidatorScript = preload("res://scripts/content/content_validator.gd")
const ThemeFactory = preload("res://scripts/ui/theme_factory.gd")
const ContentErrorViewScene = preload("res://scenes/ui/content_error_view.tscn")
const GameActionScript = preload("res://scripts/core/game_action.gd")
const MatchControllerScript = preload("res://scripts/core/match_controller.gd")
const AIPlayerScript = preload("res://scripts/ai/ai_player.gd")

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
var selected_player_deck: Dictionary = {}
var difficulty := "standard"
var controller: MatchController
var ai: AIPlayer
var screen_factory: Callable
var _match_rng := RandomNumberGenerator.new()
var _mulligan_submitted := false


func _ready() -> void:
	theme = ThemeFactory.create()
	_match_rng.randomize()
	_validate_and_start()


func show_screen(screen_name: String, payload: Dictionary = {}) -> void:
	assert(screen_name in VALID_SCREENS, "unknown screen: %s" % screen_name)
	_clear_screen()
	var scene_path: String = SCREEN_PATHS[screen_name]
	if screen_factory.is_valid():
		current_screen = screen_factory.call(scene_path)
	elif not ResourceLoader.exists(scene_path):
		current_screen = _pending_screen(screen_name)
	else:
		var packed_scene: PackedScene = load(scene_path)
		current_screen = packed_scene.instantiate()
	if current_screen == null:
		current_screen = _pending_screen(screen_name)
	screen_host.add_child(current_screen)
	if current_screen.has_method("initialize"):
		current_screen.initialize(self, payload)
	if screen_name == "deck_builder" and current_screen.has_signal("play_requested"):
		current_screen.play_requested.connect(_on_play_requested)
	if screen_name == "mulligan" and current_screen.has_signal("confirm_requested"):
		current_screen.confirm_requested.connect(_on_mulligan_confirmed)


func _on_play_requested(deck_id: String, selected_difficulty: String) -> void:
	selected_deck_id = deck_id
	difficulty = selected_difficulty
	selected_player_deck = {}
	if current_screen != null and current_screen.has_method("selected_deck"):
		selected_player_deck = (current_screen.selected_deck() as Dictionary).duplicate(true)
	start_mulligan(selected_player_deck, difficulty)


func start_mulligan(player_deck: Dictionary, selected_difficulty: String, seed: int = 0) -> void:
	if catalog == null or not player_deck.has("cards") or not (player_deck.cards is Array):
		return
	var opponent_deck := _shipped_opponent_deck(str(player_deck.get("id", "")))
	if opponent_deck.is_empty():
		return
	var match_seed := seed if seed != 0 else int(_match_rng.randi())
	difficulty = selected_difficulty
	selected_player_deck = player_deck.duplicate(true)
	controller = MatchControllerScript.create(
		catalog.cards_by_id,
		(player_deck.cards as Array).duplicate(),
		(opponent_deck.cards as Array).duplicate(),
		match_seed,
	)
	ai = AIPlayerScript.create(difficulty, match_seed)
	var started = controller.submit_action(GameActionScript.create("start_match", "system"))
	if not started.accepted or controller.state.phase != "mulligan":
		controller = null
		ai = null
		return
	_mulligan_submitted = false
	show_screen("mulligan", {
		"catalog": catalog,
		"deck_id": str(player_deck.get("id", "")),
		"difficulty": difficulty,
		"player_deck": player_deck.duplicate(true),
		"snapshot": controller.state.snapshot_for("player"),
	})


func ai_mulligan_selection(snapshot: Dictionary) -> Array[String]:
	var players: Dictionary = snapshot.get("players", {})
	var opponent: Dictionary = players.get("opponent", {})
	var hand: Array = opponent.get("hand", [])
	var seen_definitions := {}
	var selected: Array[String] = []
	for card_value in hand:
		if not (card_value is Dictionary):
			continue
		var card: Dictionary = card_value
		var definition_id := str(card.get("definition_id", ""))
		var duplicate := seen_definitions.has(definition_id)
		seen_definitions[definition_id] = true
		var unaffordable := int(card.get("deployment_cost", 0)) > 3
		if duplicate or unaffordable:
			selected.append(str(card.get("instance_id", "")))
	return selected


func _on_mulligan_confirmed(selected_ids: Array[String]) -> void:
	if _mulligan_submitted or controller == null or ai == null or controller.state.phase != "mulligan":
		return
	_mulligan_submitted = true
	var rendered_events: Array = []
	var no_targets: Array[String] = []
	var ai_ids := ai_mulligan_selection(controller.state.snapshot_for("opponent"))
	var steps := [
		["mulligan", "player", selected_ids],
		["mulligan", "opponent", ai_ids],
		["confirm_mulligan", "player", no_targets],
		["confirm_mulligan", "opponent", no_targets],
	]
	for step in steps:
		var targets: Array[String] = step[2]
		var action = GameActionScript.create(str(step[0]), str(step[1]), "", targets, {}, controller.state.sequence)
		var result = controller.submit_action(action)
		if not result.accepted:
			return
		rendered_events.append_array(result.events)
	if controller.state.phase != "action":
		return
	show_screen("match", {
		"controller": controller,
		"ai": ai,
		"difficulty": difficulty,
		"events": rendered_events,
		"snapshot": controller.state.snapshot_for("player"),
	})


func _shipped_opponent_deck(player_deck_id: String) -> Dictionary:
	var preferred_id := "su-starter" if player_deck_id != "su-starter" else "us-starter"
	if catalog.decks_by_id.has(preferred_id):
		return (catalog.decks_by_id[preferred_id] as Dictionary).duplicate(true)
	for deck_value in catalog.decks:
		if deck_value is Dictionary and str(deck_value.get("id", "")) != player_deck_id:
			return (deck_value as Dictionary).duplicate(true)
	return {}


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
		if current_screen.get_parent() == screen_host:
			screen_host.remove_child(current_screen)
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
