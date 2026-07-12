extends SceneTree

const MainScene = preload("res://scenes/main.tscn")

var _failed := false


func _init() -> void:
	call_deferred("_run")


func _run() -> void:
	for viewport_size in [Vector2i(1280, 720), Vector2i(1024, 720)]:
		root.content_scale_size = Vector2i.ZERO
		root.size = viewport_size
		await process_frame
		await _run_complete_flow(viewport_size)
	await process_frame
	await process_frame
	if not _failed:
		print("UI smoke passed at 1280x720 and 1024x720")
	quit(1 if _failed else 0)


func _run_complete_flow(viewport_size: Vector2i) -> void:
	var app = MainScene.instantiate()
	root.add_child(app)
	await _frames(3)
	_check(app.current_screen != null, "Deck Builder is instantiated")
	_check_controls(app.current_screen, viewport_size, ["%DeckSelector", "%Difficulty", "%Filters", "%Catalog", "%DeckPanel", "%PlayButton"])

	var complete_deck: Dictionary = app.current_screen.selected_deck()
	app.selected_deck_id = str(complete_deck.get("id", ""))
	app.selected_player_deck = complete_deck.duplicate(true)
	app.start_mulligan(complete_deck, "easy", 440)
	await _frames(2)
	_check_controls(app.current_screen, viewport_size, ["%HandRow", "%DifficultyLabel", "%StatusLabel", "%ConfirmButton"])
	app.current_screen.confirm_requested.emit([] as Array[String])
	await _frames(5)
	_check(app.controller != null and app.controller.state.phase == "action", "Mulligan reaches Match")
	_check_controls(app.current_screen, viewport_size, ["%OpponentHQ", "%OpponentSupport", "%Frontline", "%PlayerHQ", "%PlayerSupport", "%CreditLabel", "%EndTurnButton", "%HandScroll", "%TimelinePanel"])
	_check_perspective(app.controller.state.snapshot_for("player"))

	app.controller.state.phase = "complete"
	app.controller.state.winner_id = "player"
	app._route_match_result_if_complete()
	await _frames(2)
	_check_controls(app.current_screen, viewport_size, ["%OutcomeLabel", "%WinnerLabel", "%ReasonLabel", "%TurnsLabel", "%RematchButton", "%DeckBuilderButton"])
	_check(app.current_screen.result_payload.has("replay_log"), "Result retains replay data")
	var old_seed: int = app.controller.state.seed
	app._match_rng.seed = 991
	app.current_screen.rematch_requested.emit()
	await _frames(2)
	_check(app.controller.state.seed != old_seed, "Rematch uses a fresh deterministic seed")
	_check(app.selected_player_deck == complete_deck and app.difficulty == "easy", "Rematch preserves complete deck and difficulty")
	_check_controls(app.current_screen, viewport_size, ["%HandRow", "%ConfirmButton"])

	app.show_screen("result", {"winner_id": "", "reason": "draw", "turns": 0, "seed": app.controller.state.seed, "replay_log": {}})
	await _frames(1)
	app.current_screen.deck_builder_requested.emit()
	await _frames(2)
	_check_controls(app.current_screen, viewport_size, ["%DeckSelector", "%Difficulty", "%PlayButton"])
	_check(app.selected_deck_id == str(complete_deck.id) and app.difficulty == "easy", "Deck Builder return preserves preference")
	app.queue_free()
	await _frames(3)


func _check_controls(screen: Control, viewport_size: Vector2i, paths: Array[String]) -> void:
	_check(screen.size == Vector2(viewport_size), "%s fills %s" % [screen.name, viewport_size])
	var bounds := Rect2(Vector2.ZERO, viewport_size)
	var controls: Array[Control] = []
	for path in paths:
		_check(screen.has_node(path), "%s exposes %s" % [screen.name, path])
		if not screen.has_node(path):
			continue
		var control := screen.get_node(path) as Control
		controls.append(control)
		_check(control.is_visible_in_tree(), "%s is visible" % path)
		_check(bounds.encloses(control.get_global_rect()), "%s stays inside %s" % [path, viewport_size])
	for index in range(controls.size()):
		for other_index in range(index + 1, controls.size()):
			var first := controls[index]
			var second := controls[other_index]
			if first.is_ancestor_of(second) or second.is_ancestor_of(first):
				continue
			_check(not first.get_global_rect().intersects(second.get_global_rect()), "%s and %s do not overlap" % [paths[index], paths[other_index]])


func _check_perspective(snapshot: Dictionary) -> void:
	var opponent_hand: Array = snapshot.get("players", {}).get("opponent", {}).get("hand", [])
	for card_value in opponent_hand:
		var card: Dictionary = card_value
		_check(bool(card.get("hidden", false)), "opponent hand remains hidden")
		_check(not card.has("owner_id") and not card.has("title") and not card.has("definition_id"), "hidden cards expose no private identity")


func _check(condition: bool, message: String) -> void:
	if condition:
		return
	_failed = true
	push_error("UI smoke: %s" % message)


func _frames(count: int) -> void:
	for _index in range(count):
		await process_frame
