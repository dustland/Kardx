const Main = preload("res://scripts/main.gd")
const ThemeFactory = preload("res://scripts/ui/theme_factory.gd")
const MainScene = preload("res://scenes/main.tscn")
const ContentErrorViewScene = preload("res://scenes/ui/content_error_view.tscn")


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
