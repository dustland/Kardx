const Main = preload("res://scripts/main.gd")
const ThemeFactory = preload("res://scripts/ui/theme_factory.gd")
const MainScene = preload("res://scenes/main.tscn")
const ContentErrorViewScene = preload("res://scenes/ui/content_error_view.tscn")
const ActionBuilder = preload("res://scripts/ui/action_builder.gd")
const CardViewScene = preload("res://scenes/ui/card_view.tscn")
const ContentCatalog = preload("res://scripts/content/content_catalog.gd")
const DeckBuilderViewModel = preload("res://scripts/ui/deck_builder_view.gd")
const DeckBuilderScene = preload("res://scenes/ui/deck_builder_view.tscn")
const DeckStore = preload("res://scripts/ui/deck_store.gd")
const MulliganViewModel = preload("res://scripts/ui/mulligan_view.gd")
const MulliganScene = preload("res://scenes/ui/mulligan_view.tscn")
const ActionResult = preload("res://scripts/core/action_result.gd")
const MatchViewScript = preload("res://scripts/ui/match_view.gd")
const MatchViewScene = preload("res://scenes/ui/match_view.tscn")


class TestScreen:
	extends Control
	signal play_requested(deck_id: String, difficulty: String)

	var router: Main
	var payload: Dictionary
	var selected_definition: Dictionary = {}

	func initialize(main: Main, data: Dictionary) -> void:
		router = main
		payload = data.duplicate(true)

	func selected_deck() -> Dictionary:
		return selected_definition.duplicate(true)


static func run(t) -> void:
	_test_theme_and_screen_contract(t)
	_test_router_replaces_screen_and_initializes_payload(t)
	_test_main_routes_play_to_mulligan_fallback(t)
	_test_main_passes_selected_deck_to_mulligan(t)
	_test_missing_scene_uses_fallback(t)
	_test_content_errors_are_sorted_and_retryable(t)
	_test_main_scene_exposes_screen_host(t)
	_test_action_builders(t)
	_test_card_view_modes_and_geometry(t)
	_test_card_view_hidden_mode_redacts_data(t)
	_test_card_view_press_and_drag_share_instance_id(t)
	await _test_card_view_container_layout(t)
	_test_deck_builder_model(t)
	_test_deck_builder_reuses_existing_user_copy(t)
	_test_deck_builder_filters(t)
	_test_deck_store_round_trip_and_shipped_immutability(t)
	_test_deck_store_surfaces_corrupt_load(t)
	await _test_deck_builder_scene_contract(t)
	await _test_deck_builder_copy_selection_and_save_failure(t)
	_test_deck_builder_displays_corrupt_load_status(t)
	_test_mulligan_model_selection_and_lock(t)
	await _test_mulligan_scene_renders_and_confirms(t)
	await _test_mulligan_layout_is_responsive(t)
	await _test_main_completes_both_mulligans_and_routes(t)
	await run_task5(t)


static func run_task5(t) -> void:
	_test_match_selection_builds_only_legal_actions(t)
	_test_match_selection_cancels_and_reports_rejection(t)
	await _test_match_scene_contract_and_responsive_layout(t)


static func _test_match_selection_builds_only_legal_actions(t) -> void:
	var model := MatchViewScript.MatchInteractionModel.new()
	var attack = ActionBuilder.attack("p-unit", "o-unit", "player", 15)
	var hq_attack = GameAction.create("attack_hq", "player", "p-unit", ["o-hq"], {"target_player_id": "opponent"}, 15)
	model.set_legal_actions([attack, hq_attack])
	model.select_source("p-unit")
	t.assert_eq(model.highlighted_targets(), ["o-hq", "o-unit"], "legal targets exposed in stable order")
	t.assert_eq(model.choose_target("o-unit").type, "attack_unit", "selection resolves exact legal action")
	model.select_source("p-unit")
	t.assert_eq(model.choose_target("o-hq").type, "attack_hq", "HQ attack preserves controller action type")


static func _test_match_selection_cancels_and_reports_rejection(t) -> void:
	var model := MatchViewScript.MatchInteractionModel.new()
	model.set_legal_actions([ActionBuilder.deploy("p-card", 2, "player", 9)])
	model.select_source("p-card")
	t.assert_eq(model.highlighted_slots("support"), [2], "deploy slots exposed")
	model.cancel()
	t.assert_eq(model.selected_source_id, "", "cancel clears source")
	model.apply_rejection("insufficient_credit", "Not enough Credit")
	t.assert_eq(model.status_message, "Not enough Credit", "rejection visible")


static func _test_match_scene_contract_and_responsive_layout(t) -> void:
	for viewport_size in [Vector2(1280, 720), Vector2(1024, 720)]:
		var root_control := Control.new()
		root_control.size = viewport_size
		Engine.get_main_loop().root.add_child(root_control)
		var view = MatchViewScene.instantiate()
		root_control.add_child(view)
		view.render_snapshot(_match_snapshot())
		await Engine.get_main_loop().process_frame
		t.assert_true(view.has_node("%OpponentSupport"), "opponent support exists")
		t.assert_true(view.has_node("%Frontline"), "shared frontline exists")
		t.assert_true(view.has_node("%PlayerSupport"), "player support exists")
		t.assert_true(view.has_node("%PlayerHand"), "player hand exists")
		t.assert_true(view.has_node("%Timeline"), "timeline exists")
		t.assert_eq(view.get_node("%Frontline").get_child_count(), 5, "frontline reserves five slots")
		t.assert_eq(view.get_node("%OpponentSupport").get_child_count(), 4, "opponent reserves four slots")
		t.assert_eq(view.get_node("%PlayerSupport").get_child_count(), 4, "player reserves four slots")
		t.assert_true(view.size.x <= viewport_size.x, "match fits viewport width")
		root_control.free()


static func _match_snapshot() -> Dictionary:
	var hq := {"instance_id": "p-hq", "title": "HQ", "category": "Headquarters", "attack": 0, "defense": 20, "deployment_cost": 0, "operation_cost": 0}
	return {"players": {"player": {"id": "player", "nation": "US", "headquarters": hq, "hq_defense": 20, "deck_count": 34, "hand": [], "support_line": [null, null, null, null], "credit": 3, "credit_slots": 3}, "opponent": {"id": "opponent", "nation": "SU", "headquarters": hq.merged({"instance_id": "o-hq"}, true), "hq_defense": 20, "deck_count": 34, "hand": [{"instance_id": "o-hidden", "hidden": true}], "support_line": [null, null, null, null], "credit": 3, "credit_slots": 3}}, "frontline": [null, null, null, null, null], "active_player_id": "player", "turn": 1, "phase": "action", "sequence": 5, "winner_id": ""}


static func run_task4(t) -> void:
	_test_mulligan_model_selection_and_lock(t)
	_test_ai_mulligan_exact_fixture(t)
	await _test_mulligan_scene_renders_and_confirms(t)
	await _test_mulligan_layout_is_responsive(t)
	await _test_mulligan_prevalidates_selection(t)
	await _test_mulligan_rejections_are_transactional(t)
	await _test_mulligan_renders_before_route(t)


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


static func _test_main_routes_play_to_mulligan_fallback(t) -> void:
	var main := Main.new()
	var host := Control.new()
	main.screen_host = host
	main.catalog = ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json")
	main.screen_factory = func(path: String) -> Control:
		return TestScreen.new() if path.ends_with("deck_builder_view.tscn") else null
	main.show_screen("deck_builder", {})
	var deck: Dictionary = main.catalog.decks_by_id["us-starter"].duplicate(true)
	deck["id"] = "user-us-starter"
	deck["meta"] = {"unsaved": true}
	(main.current_screen as TestScreen).selected_definition = deck
	(main.current_screen as TestScreen).play_requested.emit("user-us-starter", "hard")
	t.assert_eq(main.selected_deck_id, "user-us-starter", "Play stores selected deck")
	t.assert_eq(main.difficulty, "hard", "Play stores difficulty")
	t.assert_eq(main.selected_player_deck, deck, "Play captures full unsaved deck before freeing builder")
	t.assert_true(main.current_screen is CenterContainer, "missing mulligan scene uses fallback")
	t.assert_eq((main.current_screen.get_child(0) as Label).text, "Mulligan", "Play visibly advances to mulligan fallback")
	main.free()


static func _test_main_passes_selected_deck_to_mulligan(t) -> void:
	var main := Main.new()
	var host := Control.new()
	main.screen_host = host
	main.catalog = ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json")
	main.screen_factory = func(_path: String) -> Control: return TestScreen.new()
	main.show_screen("deck_builder", {})
	var deck: Dictionary = main.catalog.decks_by_id["us-starter"].duplicate(true)
	deck["id"] = "user-us-starter"
	deck["meta"] = {"unsaved": true}
	(main.current_screen as TestScreen).selected_definition = deck
	(main.current_screen as TestScreen).play_requested.emit("user-us-starter", "standard")
	var payload: Dictionary = (main.current_screen as TestScreen).payload
	t.assert_eq(payload.get("player_deck"), deck, "mulligan payload receives complete selected deck")
	var expected_cards: Array = deck.cards.duplicate()
	deck.cards.clear()
	t.assert_eq((payload.player_deck as Dictionary).cards, expected_cards, "mulligan payload owns a deep copy")
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


static func _test_mulligan_model_selection_and_lock(t) -> void:
	var model := MulliganViewModel.new(["p-1", "p-2", "p-3", "p-4"])
	model.toggle("p-2")
	model.toggle("p-4")
	t.assert_eq(model.selected_ids(), ["p-2", "p-4"], "mulligan preserves hand selection order")
	model.toggle("p-2")
	t.assert_eq(model.selected_ids(), ["p-4"], "mulligan click toggles replacement off")
	model.lock()
	model.toggle("p-1")
	t.assert_eq(model.selected_ids(), ["p-4"], "locked mulligan selection is immutable")
	model.free()


static func _test_ai_mulligan_exact_fixture(t) -> void:
	var main := Main.new()
	var ai_hand := [
		{"instance_id": "o-low", "definition_id": "low", "deployment_cost": 2},
		{"instance_id": "o-duplicate", "definition_id": "low", "deployment_cost": 2},
		{"instance_id": "o-high", "definition_id": "high", "deployment_cost": 5},
		{"instance_id": "o-keep", "definition_id": "keep", "deployment_cost": 3},
	]
	var snapshot := {"players": {"player": {"hand": [{"hidden": true}]}, "opponent": {"hand": ai_hand}}}
	var selected: Array[String] = main.ai_mulligan_selection(snapshot)
	t.assert_eq(selected, ["o-duplicate", "o-high"], "AI replaces exact duplicate and high-cost fixture cards")
	var hand_ids: Array[String] = []
	for card in ai_hand:
		hand_ids.append(card.instance_id)
	for instance_id in selected:
		t.assert_true(instance_id in hand_ids, "AI replacement ID belongs to AI hand")
	var changed_player_hand := snapshot.duplicate(true)
	changed_player_hand.players.player.hand = [{"instance_id": "p-secret-a"}, {"instance_id": "p-secret-b"}]
	t.assert_eq(main.ai_mulligan_selection(changed_player_hand), selected, "player hidden-hand variation cannot affect AI mulligan")
	main.free()


static func _test_mulligan_scene_renders_and_confirms(t) -> void:
	var view = MulliganScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	var hand := [_card_data(), _card_data().merged({"instance_id": "p-02", "title": "Combat Engineers"}, true)]
	view.initialize(null, {"snapshot": {"players": {"player": {"hand": hand}}}, "difficulty": "hard"})
	await Engine.get_main_loop().process_frame
	t.assert_eq(view.get_node("%HandRow").get_child_count(), 2, "mulligan renders player hand with CardView")
	t.assert_eq(view.get_node("%DifficultyLabel").text, "HARD", "mulligan displays selected difficulty")
	var first_card = view.get_node("%HandRow").get_child(0)
	first_card.card_pressed.emit("p-01")
	t.assert_true(first_card.button_pressed, "selected replacement has clear toggled state")
	var confirmations: Array = []
	view.confirm_requested.connect(func(ids: Array[String]) -> void: confirmations.append(ids))
	view.get_node("%ConfirmButton").pressed.emit()
	view.get_node("%ConfirmButton").pressed.emit()
	t.assert_eq(confirmations, [["p-01"]], "confirm emits once with selected IDs")
	t.assert_true(view.get_node("%ConfirmButton").disabled, "confirm locks input immediately")
	view.queue_free()
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame


static func _test_main_completes_both_mulligans_and_routes(t) -> void:
	var main := Main.new()
	var host := Control.new()
	main.screen_host = host
	main.catalog = ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json")
	main.screen_factory = func(path: String) -> Control:
		return MulliganScene.instantiate() if path.ends_with("mulligan_view.tscn") else TestScreen.new()
	var player_deck: Dictionary = main.catalog.decks_by_id["us-starter"].duplicate(true)
	main.start_mulligan(player_deck, "hard", 88)
	await Engine.get_main_loop().process_frame
	t.assert_eq(main.controller.state.phase, "mulligan", "main starts controller in mulligan phase")
	t.assert_eq(main.ai.difficulty, "hard", "main constructs AI with selected difficulty")
	var opponent_snapshot: Dictionary = main.controller.state.snapshot_for("opponent")
	var expected_ai_ids: Array[String] = main.ai_mulligan_selection(opponent_snapshot)
	t.assert_eq(main.ai_mulligan_selection(opponent_snapshot), expected_ai_ids, "AI mulligan selection is deterministic")
	var no_replacements: Array[String] = []
	main.current_screen.confirm_requested.emit(no_replacements)
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame
	t.assert_eq(main.controller.state.phase, "action", "both action-contract confirmations start match")
	t.assert_true(main.controller.state.players.player.mulligan_used, "player mulligan action submitted")
	t.assert_true(main.controller.state.players.opponent.mulligan_used, "AI mulligan action submitted")
	t.assert_true(main.controller.state.players.player.mulligan_confirmed, "player mulligan confirmed")
	t.assert_true(main.controller.state.players.opponent.mulligan_confirmed, "AI mulligan confirmed")
	t.assert_eq((main.current_screen as TestScreen).payload.get("controller"), main.controller, "match receives live controller")
	t.assert_eq((main.current_screen as TestScreen).payload.get("ai"), main.ai, "match receives configured AI")
	var ai_action: Dictionary = main.controller.replay_log.actions[2].action
	t.assert_eq(ai_action.type, "mulligan", "AI submits through mulligan action contract")
	t.assert_eq(ai_action.target_ids, expected_ai_ids, "AI submits deterministic own-hand selections")
	main.free()
	host.free()


static func _test_mulligan_rejections_are_transactional(t) -> void:
	for rejected_step in range(4):
		var harness := _task4_main()
		var main: Main = harness.main
		var player_deck: Dictionary = main.catalog.decks_by_id["us-starter"].duplicate(true)
		main.start_mulligan(player_deck, "standard", 88)
		await Engine.get_main_loop().process_frame
		if not main.has_method("set_mulligan_action_submitter"):
			t.assert_true(false, "main exposes injectable mulligan submission boundary")
			await _free_task4_harness(harness)
			return
		var original_hash := main.controller.state_hash()
		main.set_mulligan_action_submitter(func(candidate, action, step_index: int):
			if step_index == rejected_step:
				return ActionResult.reject("injected_rejection", "Choose a different opening hand")
			return candidate.submit_action(action)
		)
		var no_replacements: Array[String] = []
		main.current_screen.confirm_requested.emit(no_replacements)
		await Engine.get_main_loop().process_frame
		t.assert_eq(main.controller.state_hash(), original_hash, "step %d rejection preserves original controller" % rejected_step)
		t.assert_eq(main.controller.state.phase, "mulligan", "step %d rejection remains in mulligan" % rejected_step)
		t.assert_true(not main.current_screen.get_node("%ConfirmButton").disabled, "step %d rejection unlocks confirm" % rejected_step)
		t.assert_true("Choose a different" in main.current_screen.get_node("%StatusLabel").text, "step %d rejection renders actionable error" % rejected_step)
		await _free_task4_harness(harness)


static func _test_mulligan_prevalidates_selection(t) -> void:
	var harness := _task4_main()
	var main: Main = harness.main
	var player_deck: Dictionary = main.catalog.decks_by_id["us-starter"].duplicate(true)
	main.start_mulligan(player_deck, "standard", 88)
	await Engine.get_main_loop().process_frame
	var original_hash := main.controller.state_hash()
	var submit_calls := [0]
	main.set_mulligan_action_submitter(func(candidate, action, step_index: int):
		submit_calls[0] += 1
		return candidate.submit_action(action)
	)
	var invalid_selection: Array[String] = ["p-not-in-hand"]
	main.current_screen.confirm_requested.emit(invalid_selection)
	await Engine.get_main_loop().process_frame
	t.assert_eq(submit_calls[0], 0, "invalid player selection is rejected before setup actions")
	t.assert_eq(main.controller.state_hash(), original_hash, "prevalidation preserves live controller")
	t.assert_true(not main.current_screen.get_node("%ConfirmButton").disabled, "prevalidation failure unlocks view")
	t.assert_true("opening hand" in main.current_screen.get_node("%StatusLabel").text, "prevalidation gives actionable error")
	await _free_task4_harness(harness)


static func _test_mulligan_renders_before_route(t) -> void:
	var harness := _task4_main()
	var main: Main = harness.main
	var mulligan_view := [null]
	var rendered_before_route := [false]
	main.screen_factory = func(path: String) -> Control:
		if path.ends_with("mulligan_view.tscn"):
			mulligan_view[0] = MulliganScene.instantiate()
			return mulligan_view[0]
		rendered_before_route[0] = mulligan_view[0] != null and mulligan_view[0].has_rendered_result()
		return TestScreen.new()
	var player_deck: Dictionary = main.catalog.decks_by_id["us-starter"].duplicate(true)
	main.start_mulligan(player_deck, "hard", 88)
	await Engine.get_main_loop().process_frame
	var no_replacements: Array[String] = []
	main.current_screen.confirm_requested.emit(no_replacements)
	t.assert_true(main.current_screen == mulligan_view[0], "mulligan remains visible immediately after confirm")
	if mulligan_view[0] == null or not mulligan_view[0].has_method("has_rendered_result"):
		t.assert_true(false, "mulligan exposes rendered-result state")
		await _free_task4_harness(harness)
		return
	await Engine.get_main_loop().process_frame
	t.assert_true(mulligan_view[0].has_rendered_result(), "result state renders before route")
	t.assert_true("Opening hand ready" in mulligan_view[0].get_node("%StatusLabel").text, "rendered event summary is visible before route")
	t.assert_true(mulligan_view[0].get_node("%HandRow").get_child_count() in [4, 5], "resulting hand state renders before route")
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame
	t.assert_true(rendered_before_route[0], "route observes rendered mulligan result")
	t.assert_true(main.current_screen is TestScreen, "route occurs after visible result frame")
	await _free_task4_harness(harness)


static func _test_mulligan_layout_is_responsive(t) -> void:
	for viewport_size in [Vector2(1280, 720), Vector2(1024, 576)]:
		var view = MulliganScene.instantiate()
		view.set_anchors_and_offsets_preset(Control.PRESET_TOP_LEFT)
		view.size = viewport_size
		Engine.get_main_loop().root.add_child(view)
		var hand: Array = []
		for index in range(5):
			hand.append(_card_data().merged({"instance_id": "p-%02d" % index}, true))
		view.initialize(null, {"snapshot": {"players": {"player": {"hand": hand}}}})
		await Engine.get_main_loop().process_frame
		await Engine.get_main_loop().process_frame
		var panel_rect: Rect2 = view.get_node("Margin/Page/HandPanel").get_global_rect()
		var footer_rect: Rect2 = view.get_node("Margin/Page/Footer").get_global_rect()
		for card in view.get_node("%HandRow").get_children():
			t.assert_true(panel_rect.encloses(card.get_global_rect()), "%s mulligan card stays inside hand panel" % viewport_size.x)
			t.assert_true(not card.get_global_rect().intersects(footer_rect), "%s mulligan card avoids commands" % viewport_size.x)
		view.queue_free()
		await Engine.get_main_loop().process_frame
		await Engine.get_main_loop().process_frame


static func _task4_main() -> Dictionary:
	var main := Main.new()
	var host := Control.new()
	main.screen_host = host
	main.catalog = ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json")
	main.screen_factory = func(path: String) -> Control:
		return MulliganScene.instantiate() if path.ends_with("mulligan_view.tscn") else TestScreen.new()
	Engine.get_main_loop().root.add_child(host)
	return {"main": main, "host": host}


static func _free_task4_harness(harness: Dictionary) -> void:
	var main: Main = harness.main
	main.free()
	var host: Control = harness.host
	host.queue_free()
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame


static func _test_deck_builder_model(t) -> void:
	var catalog = _catalog()
	var model = DeckBuilderViewModel.DeckBuilderViewModel.new(catalog)
	model.select_deck("us-starter")
	t.assert_eq(model.card_count(), 40, "starter deck count")
	t.assert_true(model.validation().valid, "starter deck playable")
	model.add_card("su-yak-patrol")
	t.assert_true(not model.validation().valid, "invalid edit disables play")
	t.assert_true(model.selected_deck_id.begins_with("user-"), "editing shipped deck creates user copy")
	t.assert_eq((catalog.decks_by_id["us-starter"] as Dictionary).cards.size(), 40, "shipped deck remains immutable")
	model.remove_card("su-yak-patrol")
	t.assert_true(model.validation().valid, "removing edit restores validity")
	var selected: Dictionary = model.selected_deck()
	selected.cards.clear()
	t.assert_eq(model.card_count(), 40, "selected deck accessor returns a deep copy")


static func _test_deck_builder_filters(t) -> void:
	var model = DeckBuilderViewModel.DeckBuilderViewModel.new(_catalog())
	model.set_filter("search", "yak")
	model.set_filter("nation", "SovietUnion")
	model.set_filter("category", "Unit")
	model.set_filter("unit_type", "Fighter")
	model.set_filter("rarity", "Special")
	model.set_filter("cost", 5)
	var cards: Array = model.filtered_cards()
	t.assert_eq(cards.size(), 1, "all catalog filters combine")
	t.assert_eq((cards[0] as Dictionary).id, "su-yak-patrol", "filters retain matching card")


static func _test_deck_builder_reuses_existing_user_copy(t) -> void:
	var model = DeckBuilderViewModel.DeckBuilderViewModel.new(_catalog())
	model.select_deck("us-starter")
	model.add_card("su-yak-patrol")
	var copy_id: String = model.selected_deck_id
	model.select_deck("us-starter")
	model.add_card("su-yak-patrol")
	t.assert_eq(model.selected_deck_id, copy_id, "editing shipped deck reselects existing user copy")
	t.assert_eq(model.user_decks().size(), 1, "repeated shipped edits do not create hidden duplicate copies")


static func _test_deck_store_round_trip_and_shipped_immutability(t) -> void:
	var path := "user://test-decks-task3.json"
	var temp_path := path + ".tmp"
	DirAccess.remove_absolute(ProjectSettings.globalize_path(path))
	DirAccess.remove_absolute(ProjectSettings.globalize_path(temp_path))
	var shipped: Array = [{"id": "us-starter", "cards": ["us-hq"]}]
	var store = DeckStore.new(path)
	t.assert_true(store.save_user_decks([{"id": "user-alpha", "cards": ["su-hq"]}]), "user decks save atomically")
	t.assert_true(store.save_user_decks([{"id": "user-alpha", "cards": ["us-hq", "su-hq"]}]), "atomic save replaces existing data")
	t.assert_true(not FileAccess.file_exists(temp_path), "atomic save leaves no temporary file")
	var loaded: Dictionary = store.load_all(shipped)
	t.assert_eq((loaded["us-starter"] as Dictionary).cards, ["us-hq"], "shipped deck loads")
	t.assert_eq((loaded["user-alpha"] as Dictionary).cards, ["us-hq", "su-hq"], "user deck round trips")
	t.assert_true(not store.save_user_decks([{"id": "us-starter", "cards": []}], shipped), "shipped deck cannot be overwritten")
	DirAccess.remove_absolute(ProjectSettings.globalize_path(path))


static func _test_deck_store_surfaces_corrupt_load(t) -> void:
	var path := "user://test-decks-task3-corrupt.json"
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_string("{not valid json")
	file.close()
	var store = DeckStore.new(path)
	var loaded: Dictionary = store.load_all([{"id": "us-starter", "cards": ["us-hq"]}])
	t.assert_true(loaded.has("us-starter"), "corrupt user data does not hide shipped decks")
	t.assert_true(not store.last_error.is_empty(), "corrupt user data surfaces a load error")
	t.assert_true("corrupt" in store.last_error.to_lower(), "load error is actionable")
	DirAccess.remove_absolute(ProjectSettings.globalize_path(path))


static func _test_deck_builder_scene_contract(t) -> void:
	var view = DeckBuilderScene.instantiate()
	view.theme = ThemeFactory.create()
	Engine.get_main_loop().root.add_child(view)
	view.initialize(null, {"catalog": _catalog(), "deck_id": "us-starter", "difficulty": "standard", "store_path": "user://test-decks-task3.json"})
	for path in ["DeckSelector", "Difficulty", "Search", "NationFilter", "CategoryFilter", "UnitTypeFilter", "RarityFilter", "CostFilter", "CatalogGrid", "DeckList", "NationDistribution", "CardCount", "Validation", "SaveButton", "PlayButton"]:
		t.assert_true(view.has_node("%%%s" % path), "deck builder exposes %s" % path)
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame
	t.assert_eq((view.get_node("%CatalogGrid") as GridContainer).columns, 3, "catalog uses three columns")
	t.assert_true(not (view.get_node("%PlayButton") as Button).disabled, "valid shipped deck enables play")
	await _assert_builder_layout(t, view, Vector2(1280, 720))
	await _assert_builder_layout(t, view, Vector2(1024, 720))
	view.free()


static func _assert_builder_layout(t, view: Control, viewport_size: Vector2) -> void:
	view.set_anchors_and_offsets_preset(Control.PRESET_TOP_LEFT)
	view.size = viewport_size
	await Engine.get_main_loop().process_frame
	await Engine.get_main_loop().process_frame
	t.assert_eq(view.size, viewport_size, "%s-wide builder preserves assigned viewport size" % int(viewport_size.x))
	if viewport_size.x == 1024:
		t.assert_true(view.get_combined_minimum_size().x <= 1024.0, "builder combined minimum genuinely fits 1024")
		t.assert_true((view.get_node("Margin/Page") as Control).get_combined_minimum_size().x <= 992.0, "builder content minimum fits inside 1024 margins")
		t.assert_true((view.get_node("%Filters") as Control).size.x <= 160.0, "1024 layout compacts filter rail")
		t.assert_true((view.get_node("%DeckPanel") as Control).size.x <= 245.0, "1024 layout compacts deck rail")
	var columns := [view.get_node("%Filters") as Control, view.get_node("%Catalog") as Control, view.get_node("%DeckPanel") as Control]
	for index in range(columns.size() - 1):
		t.assert_true(columns[index].get_global_rect().end.x <= columns[index + 1].get_global_rect().position.x, "%s-wide builder columns do not overlap" % int(viewport_size.x))
	var viewport_rect := view.get_global_rect()
	for path in ["DeckSelector", "Difficulty", "Search", "NationFilter", "CategoryFilter", "UnitTypeFilter", "RarityFilter", "CostFilter", "Catalog", "DeckList", "NationDistribution", "CardCount", "Validation", "SaveButton", "PlayButton"]:
		var control := view.get_node("%%%s" % path) as Control
		t.assert_true(viewport_rect.encloses(control.get_global_rect()), "%s-wide viewport contains %s" % [int(viewport_size.x), path])


static func _test_deck_builder_copy_selection_and_save_failure(t) -> void:
	var path := "user://test-decks-task3-blocker"
	var blocker := FileAccess.open(path, FileAccess.WRITE)
	blocker.store_string("file blocks directory creation")
	blocker.close()
	var view = DeckBuilderScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	view.initialize(null, {"catalog": _catalog(), "deck_id": "us-starter", "difficulty": "standard", "store_path": path + "/decks.json"})
	view._on_add_card("su-yak-patrol")
	var selector := view.get_node("%DeckSelector") as OptionButton
	t.assert_eq(str(selector.get_item_metadata(selector.selected)), view.model.selected_deck_id, "copy-on-edit visibly selects user deck")
	t.assert_eq(selector.item_count, view.model.decks.size(), "selector rebuild includes user copy exactly once")
	view._on_save_pressed()
	t.assert_true("failed" in (view.get_node("%Validation") as Label).text.to_lower(), "failed persistence never claims Saved")
	t.assert_true(not view.store.last_error.is_empty(), "save failure exposes actionable detail")
	view.free()
	DirAccess.remove_absolute(ProjectSettings.globalize_path(path))


static func _test_deck_builder_displays_corrupt_load_status(t) -> void:
	var path := "user://test-decks-task3-ui-corrupt.json"
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_string("broken")
	file.close()
	var view = DeckBuilderScene.instantiate()
	view.initialize(null, {"catalog": _catalog(), "deck_id": "us-starter", "difficulty": "standard", "store_path": path})
	t.assert_true("corrupt" in (view.get_node("%Validation") as Label).text.to_lower(), "builder displays corrupt persistence status")
	view.free()
	DirAccess.remove_absolute(ProjectSettings.globalize_path(path))


static func _catalog():
	return ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json")
