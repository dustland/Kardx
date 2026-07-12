const Main = preload("res://scripts/main.gd")
const ThemeFactory = preload("res://scripts/ui/theme_factory.gd")
const MainScene = preload("res://scenes/main.tscn")
const ContentErrorViewScene = preload("res://scenes/ui/content_error_view.tscn")
const ActionBuilder = preload("res://scripts/ui/action_builder.gd")
const CardViewScene = preload("res://scenes/ui/card_view.tscn")


class TestScreen:
	extends Control

	var router: Main
	var payload: Dictionary

	func initialize(main: Main, data: Dictionary) -> void:
		router = main
		payload = data.duplicate(true)


static func run(t) -> void:
	_test_theme_and_screen_contract(t)
	_test_router_replaces_screen_and_initializes_payload(t)
	_test_missing_scene_uses_fallback(t)
	_test_content_errors_are_sorted_and_retryable(t)
	_test_main_scene_exposes_screen_host(t)
	_test_action_builders(t)
	_test_card_view_modes_and_geometry(t)
	_test_card_view_hidden_mode_redacts_data(t)
	_test_card_view_press_and_drag_share_instance_id(t)
	await _test_card_view_container_layout(t)


static func _test_theme_and_screen_contract(t) -> void:
	var theme := ThemeFactory.create()
	t.assert_true(theme.has_color("font_color", "Label"), "theme defines label color")
	t.assert_eq(theme.get_color("font_color", "Label"), Color("e8e1d2"), "approved warm text")
	t.assert_eq(Main.VALID_SCREENS, ["deck_builder", "mulligan", "match", "result"], "complete flow")


static func _test_router_replaces_screen_and_initializes_payload(t) -> void:
	var main := Main.new()
	var host := Control.new()
	main.screen_host = host
	main.screen_factory = func(_path: String) -> Control: return TestScreen.new()
	main.show_screen("deck_builder", {"deck_id": "us-starter", "Credit": 3})
	var first_screen := main.current_screen as TestScreen
	t.assert_eq(first_screen.router, main, "screen receives router")
	t.assert_eq(first_screen.payload, {"deck_id": "us-starter", "Credit": 3}, "screen receives payload")
	main.show_screen("match", {"difficulty": "hard"})
	t.assert_true(first_screen.is_queued_for_deletion(), "replaced screen is freed")
	t.assert_eq(host.get_child_count(), 1, "screen host contains only active screen")
	t.assert_eq((main.current_screen as TestScreen).payload, {"difficulty": "hard"}, "replacement receives payload")
	first_screen.free()
	main.free()


static func _test_missing_scene_uses_fallback(t) -> void:
	var main := Main.new()
	var host := Control.new()
	main.screen_host = host
	main.show_screen("result", {})
	t.assert_true(main.current_screen is CenterContainer, "missing screen uses stable fallback")
	t.assert_eq((main.current_screen.get_child(0) as Label).text, "Result", "fallback names requested screen")
	main.free()


static func _test_content_errors_are_sorted_and_retryable(t) -> void:
	var retried := [false]
	var view := ContentErrorViewScene.instantiate()
	var diagnostics: Array[Dictionary] = [
		{"code": "zeta", "path": "cards[2]", "message": "last"},
		{"code": "alpha", "path": "cards[1]", "message": "first"},
	]
	view.initialize(diagnostics, func() -> void: retried[0] = true)
	var formatted: String = view.get_node("Center/Panel/Margin/Content/Scroll/Diagnostics").text
	t.assert_true(formatted.find("[alpha]") < formatted.find("[zeta]"), "diagnostics render in sorted order")
	t.assert_true("cards[1]\nfirst" in formatted, "diagnostics include path and message")
	view._on_retry_pressed()
	t.assert_true(retried[0], "retry invokes validation callback")
	view.free()


static func _test_main_scene_exposes_screen_host(t) -> void:
	var main := MainScene.instantiate()
	t.assert_true(main.has_node("ScreenHost"), "main scene exposes ScreenHost path")
	t.assert_true(main.get_node("ScreenHost") is Control, "ScreenHost is a Control")
	main.free()


static func _test_action_builders(t) -> void:
	var deploy := ActionBuilder.deploy("p-01", 2, "player", 9)
	t.assert_eq(deploy.type, "deploy_unit", "deploy action type")
	t.assert_eq(deploy.actor_id, "player", "deploy actor")
	t.assert_eq(deploy.source_id, "p-01", "deploy source")
	t.assert_eq(deploy.payload, {"support_slot": 2}, "deploy target")
	t.assert_eq(deploy.expected_sequence, 9, "deploy sequence")

	var move := ActionBuilder.move("p-02", 1, "player", 10)
	t.assert_eq(move.type, "move_unit", "move action type")
	t.assert_eq(move.payload, {"zone": "frontline", "slot": 1}, "move target")

	var attack := ActionBuilder.attack("p-02", "o-03", "player", 11)
	t.assert_eq(attack.type, "attack_unit", "attack action type")
	t.assert_eq(attack.target_ids, ["o-03"], "attack target")

	var order := ActionBuilder.play_order("p-04", ["o-01", "o-02"], "player", 12)
	t.assert_eq(order.type, "play_order", "order action type")
	t.assert_eq(order.target_ids, ["o-01", "o-02"], "order targets")

	var countermeasure := ActionBuilder.toggle_countermeasure("p-05", "player", 13)
	t.assert_eq(countermeasure.type, "toggle_countermeasure", "countermeasure action type")
	t.assert_eq(countermeasure.target_ids, [], "countermeasure has no targets")


static func _test_card_view_modes_and_geometry(t) -> void:
	var card := _card_data()
	var expected_sizes := {
		"catalog": Vector2(180, 252),
		"hand": Vector2(116, 162),
		"battlefield": Vector2(84, 118),
	}
	for mode in expected_sizes:
		var view = CardViewScene.instantiate()
		view.bind(card, mode)
		t.assert_eq(view.custom_minimum_size, expected_sizes[mode], "%s mode has stable geometry" % mode)
		t.assert_eq(view.get_node("Frame/Title").text, "Rifle Platoon", "%s mode renders title" % mode)
		t.assert_eq(view.get_node("Frame/Stats/Attack").text, "1", "%s mode renders attack" % mode)
		t.assert_eq(view.get_node("Frame/Stats/Defense").text, "2", "%s mode renders defense" % mode)
		t.assert_true(view.get_node("Frame/Artwork").texture != null, "%s mode has fallback artwork" % mode)
		view.free()


static func _test_card_view_hidden_mode_redacts_data(t) -> void:
	var view = CardViewScene.instantiate()
	view.bind(_card_data(), "hidden")
	t.assert_eq(view.custom_minimum_size, Vector2(116, 162), "hidden mode uses hand geometry")
	t.assert_eq(view.card_data, {"instance_id": "p-01", "hidden": true}, "hidden mode retains only identity metadata")
	t.assert_eq(view.get_node("Frame/Title").text, "", "hidden mode redacts title")
	t.assert_eq(view.tooltip_text, "", "hidden mode redacts description")
	t.assert_true(view.get_node("CardBack").visible, "hidden mode shows card back")
	t.assert_true(not view.get_node("Frame").visible, "hidden mode hides face")
	view.free()


static func _test_card_view_press_and_drag_share_instance_id(t) -> void:
	var view = CardViewScene.instantiate()
	view.bind(_card_data(), "hand")
	var pressed_ids: Array[String] = []
	var dragged_ids: Array[String] = []
	view.card_pressed.connect(func(instance_id: String) -> void: pressed_ids.append(instance_id))
	view.card_drag_started.connect(func(instance_id: String) -> void: dragged_ids.append(instance_id))
	view._on_pressed()
	var drag_data: Dictionary = view._get_drag_data(Vector2.ZERO)
	t.assert_eq(pressed_ids, ["p-01"], "press emits bound instance ID")
	t.assert_eq(dragged_ids, ["p-01"], "drag emits bound instance ID")
	t.assert_eq(drag_data.get("instance_id"), "p-01", "drag data maps the same instance ID")
	view.free()


static func _test_card_view_container_layout(t) -> void:
	var expected_sizes := {
		"catalog": Vector2(180, 252),
		"hand": Vector2(116, 162),
		"battlefield": Vector2(84, 118),
		"hidden": Vector2(116, 162),
	}
	for mode in expected_sizes:
		var container := CenterContainer.new()
		container.size = Vector2(420, 360)
		Engine.get_main_loop().root.add_child(container)
		var view = CardViewScene.instantiate()
		container.add_child(view)
		view.bind(_card_data(), mode)
		await Engine.get_main_loop().process_frame
		await Engine.get_main_loop().process_frame
		t.assert_eq(view.size, expected_sizes[mode], "%s stays exact inside a larger container" % mode)
		_assert_visible_layout(t, view, mode)
		container.free()


static func _assert_visible_layout(t, view: Control, mode: String) -> void:
	var paths := [
		"CardBack/Mark",
		"Frame/Artwork",
		"Frame/Title",
		"Frame/Type",
		"Frame/Costs/Deployment",
		"Frame/Costs/Operation",
		"Frame/Description",
		"Frame/Keywords",
		"Frame/Stats/Attack",
		"Frame/Stats/Defense",
	]
	var visible_controls: Array[Control] = []
	var card_rect := view.get_global_rect()
	for path in paths:
		var control := view.get_node(path) as Control
		if not control.is_visible_in_tree():
			continue
		visible_controls.append(control)
		var rect := control.get_global_rect()
		t.assert_true(card_rect.encloses(rect), "%s %s stays within card bounds" % [mode, path])
	for index in range(visible_controls.size()):
		for other_index in range(index + 1, visible_controls.size()):
			var first := visible_controls[index]
			var second := visible_controls[other_index]
			t.assert_true(
				not first.get_global_rect().intersects(second.get_global_rect()),
				"%s %s does not overlap %s" % [mode, first.get_path(), second.get_path()]
			)


static func _card_data() -> Dictionary:
	return {
		"instance_id": "p-01",
		"title": "Rifle Platoon",
		"description": "Reliable infantry for holding ground.",
		"category": "Unit",
		"unit_type": "Infantry",
		"deployment_cost": 1,
		"operation_cost": 1,
		"attack": 1,
		"defense": 2,
		"keywords": ["Guard"],
		"image_path": "res://missing-card-art.png",
	}
