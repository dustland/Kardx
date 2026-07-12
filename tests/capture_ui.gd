extends SceneTree

const MainScene = preload("res://scenes/main.tscn")
const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")

const CAPTURE_DIR := "res://builds/qa"
const CAPTURE_SEED := 90210
const CAPTURES := [
	["deck_builder", Vector2i(1280, 720), "deck-builder.png"],
	["mulligan", Vector2i(1280, 720), "mulligan.png"],
	["match", Vector2i(1280, 720), "match-1280.png"],
	["match", Vector2i(960, 640), "match-960.png"],
	["result", Vector2i(1280, 720), "result.png"],
]

var _failed := false


func _init() -> void:
	call_deferred("_run")


func _run() -> void:
	var capture_dir := ProjectSettings.globalize_path(CAPTURE_DIR)
	_check(
		DirAccess.make_dir_recursive_absolute(capture_dir) == OK,
		"creates capture directory %s" % capture_dir
	)
	var app = MainScene.instantiate()
	root.add_child(app)
	await _frames(3)

	await _capture_current_screen(app, CAPTURES[0])
	var player_deck: Dictionary = app.current_screen.selected_deck()
	_check(str(player_deck.get("id", "")) == "us-starter", "uses the fixed us-starter deck")
	app.start_mulligan(player_deck, "easy", CAPTURE_SEED)
	await _frames(2)
	_check(app.controller != null, "creates a match controller")
	_check(app.controller != null and app.controller.state.seed == CAPTURE_SEED, "uses capture seed %d" % CAPTURE_SEED)
	_check(app.controller != null and app.controller.state.phase == "mulligan", "reaches mulligan through start_match")
	await _capture_current_screen(app, CAPTURES[1])

	var match_events: Array = []
	for step in [["mulligan", "player"], ["mulligan", "opponent"], ["confirm_mulligan", "player"], ["confirm_mulligan", "opponent"]]:
		var action = GameAction.create(str(step[0]), str(step[1]), "", [], {}, app.controller.state.sequence)
		var result = app.controller.submit_action(action)
		_check(result.accepted, "accepts capture setup action %s for %s" % [step[0], step[1]])
		match_events.append_array(result.events)
	app.ai = null
	app.show_screen("match", {
		"controller": app.controller,
		"ai": null,
		"difficulty": app.difficulty,
		"events": match_events,
		"snapshot": app.controller.state.snapshot_for("player"),
	})
	await _frames(2)
	_check(app.controller != null and app.controller.state.phase == "action", "reaches match through mulligan actions")
	await _capture_current_screen(app, CAPTURES[2])
	await _capture_current_screen(app, CAPTURES[3])

	var terminal_controller: MatchController = CoreCards.scripted_full_match(CAPTURE_SEED)
	_check(terminal_controller != null, "creates terminal controller")
	_check(terminal_controller != null and terminal_controller.state.phase == "complete", "finishes through accepted actions")
	_check(terminal_controller != null and not terminal_controller.state.winner_id.is_empty(), "completed match has a winner")
	if terminal_controller != null:
		app.controller = terminal_controller
		app.show_screen("result", {
			"winner_id": terminal_controller.state.winner_id,
			"reason": Main.terminal_reason(
				terminal_controller.state.phase,
				terminal_controller.state.winner_id,
				terminal_controller.event_history,
				terminal_controller.replay_log.terminal_result,
			),
			"turns": terminal_controller.state.turn,
			"seed": terminal_controller.state.seed,
			"replay_log": terminal_controller.replay_log.to_dict(),
		})
		await _frames(2)
		await _capture_current_screen(app, CAPTURES[4])

	root.remove_child(app)
	app.free()
	await _frames(2)
	_check(not is_instance_valid(app), "frees capture app during teardown")
	if not _failed:
		print("UI captures passed: %s" % capture_dir)
	quit(1 if _failed else 0)


func _capture_current_screen(app, capture: Array) -> void:
	var screen_name := str(capture[0])
	var viewport_size: Vector2i = capture[1]
	var filename := str(capture[2])
	root.content_scale_size = Vector2i.ZERO
	root.size = viewport_size
	await _frames(2)
	_check(app.current_screen != null, "%s has a current screen" % filename)
	_check(_screen_name(app.current_screen) == screen_name, "%s renders %s" % [filename, screen_name])
	var image := root.get_texture().get_image()
	_check(image != null and not image.is_empty(), "%s viewport image is available" % filename)
	if image == null or image.is_empty():
		return
	_check(image.get_size() == viewport_size, "%s is %dx%d" % [filename, viewport_size.x, viewport_size.y])
	_check(_has_visible_pixels(image), "%s contains nontransparent pixels" % filename)
	_check(_has_pixel_variation(image), "%s is not blank" % filename)
	_check_artwork_regions(app.current_screen, image, filename)
	var path := "%s/%s" % [CAPTURE_DIR, filename]
	_check(image.save_png(path) == OK, "writes %s" % ProjectSettings.globalize_path(path))


func _screen_name(screen: Control) -> String:
	var script_path := str(screen.get_script().resource_path)
	return script_path.get_file().trim_suffix("_view.gd")


func _has_visible_pixels(image: Image) -> bool:
	for y in range(0, image.get_height(), 8):
		for x in range(0, image.get_width(), 8):
			if image.get_pixel(x, y).a > 0.0:
				return true
	return false


func _has_pixel_variation(image: Image) -> bool:
	var first := image.get_pixel(0, 0)
	for y in range(0, image.get_height(), 8):
		for x in range(0, image.get_width(), 8):
			if not image.get_pixel(x, y).is_equal_approx(first):
				return true
	return false


func _check_artwork_regions(screen: Control, image: Image, filename: String) -> void:
	var cards: Array[Node] = screen.find_children("*", "CardView", true, false)
	var checked := 0
	var onscreen := 0
	for node in cards:
		var card := node as Control
		if not card.is_visible_in_tree() or bool(card.card_data.get("hidden", false)):
			continue
		var artwork := card.get_node("Frame/Artwork") as TextureRect
		if not artwork.is_visible_in_tree() or artwork.texture == null:
			continue
		checked += 1
		var art_path := artwork.texture.resource_path
		_check(art_path.begins_with("res://game_assets/generated_cards/"), "%s card uses generated artwork" % filename)
		var card_rect := card.get_global_rect()
		var art_rect := artwork.get_global_rect()
		_check(card_rect.encloses(art_rect), "%s artwork stays within card bounds" % filename)
		var region := Rect2i(Vector2i(art_rect.position), Vector2i(art_rect.size)).intersection(Rect2i(Vector2i.ZERO, image.get_size()))
		if region.has_area():
			onscreen += 1
			_check(_has_pixel_variation(image.get_region(region)), "%s artwork region is nonblank" % filename)
	if filename != "result.png":
		_check(checked > 0, "%s checks at least one visible artwork region" % filename)
		_check(onscreen > 0, "%s checks at least one on-screen artwork region" % filename)


func _frames(count: int) -> void:
	for _index in range(count):
		await process_frame


func _check(condition: bool, message: String) -> void:
	if condition:
		return
	_failed = true
	push_error("UI capture: %s" % message)
