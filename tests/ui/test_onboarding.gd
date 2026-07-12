const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const MatchCoachModel = preload("res://scripts/ui/match_coach_model.gd")
const OnboardingStore = preload("res://scripts/ui/onboarding_store.gd")
const ContentCatalog = preload("res://scripts/content/content_catalog.gd")
const DeckBuilderScene = preload("res://scenes/ui/deck_builder_view.tscn")
const MatchViewScene = preload("res://scenes/ui/match_view.tscn")
const CardMotionDirector = preload("res://scripts/ui/card_motion_director.gd")
const AIPlayer = preload("res://scripts/ai/ai_player.gd")


static func run(t) -> void:
	_test_coach_priority_and_exact_copy(t)
	_test_milestone_objective_progression(t)
	_test_real_first_turn_credit_fixture(t)
	_test_real_active_countermeasure_is_legal(t)
	_test_source_reasons(t)
	_test_end_turn_requires_the_sole_complete_action(t)
	_test_persistence(t)
	_test_persistence_rejects_non_user_paths(t)
	await _test_deck_builder_starter_readiness(t)
	await _test_deck_builder_edited_validation_and_restoration(t)
	await _test_deck_builder_readiness_containment(t)
	await _test_match_coach_and_card_states(t)
	await _test_rejection_refresh_ordering(t)
	await _test_end_turn_semantic_states(t)
	await _test_unavailable_card_does_not_submit(t)
	await _test_target_highlights_preserve_geometry(t)
	await _test_stable_battlefield_grid(t)
	await _test_visible_card_registry(t)
	await _test_event_animation_queue(t)
	await _test_real_main_milestone_submissions(t)


static func _test_deck_builder_starter_readiness(t) -> void:
	var onboarding_path := "user://test-onboarding-deck-builder.json"
	_cleanup(onboarding_path)
	var view = await _create_deck_builder(onboarding_path, Vector2(1280, 720))
	t.assert_eq(view.get_node("%StarterStatus").text, "Starter deck ready - 40 valid cards.", "shipped deck has exact readiness copy")
	t.assert_eq(view.get_node("%StarterHint").text, "Choose a difficulty and start battle, or click cards to edit.", "starter hint uses exact explanatory copy")
	t.assert_true(view.get_node("%StarterHint").visible, "starter hint begins visible")
	t.assert_eq(view.get_node("%PlayButton").text, "Start Battle", "launch command names the battle")
	var dismiss := view.get_node("%DismissHint") as Button
	t.assert_eq(dismiss.tooltip_text, "Hide starter hint", "icon close button explains itself")
	dismiss.pressed.emit()
	await view.get_tree().process_frame
	t.assert_true(not view.get_node("%StarterHint").visible, "dismissal hides only the explanatory sentence")
	t.assert_true(view.get_node("%StarterStatus").visible, "dismissal preserves live readiness")
	view.queue_free()
	await Engine.get_main_loop().process_frame

	view = await _create_deck_builder(onboarding_path, Vector2(1280, 720))
	t.assert_true(not view.get_node("%StarterHint").visible, "dismissal reloads from injected onboarding path")
	t.assert_eq(view.get_node("%StarterStatus").text, "Starter deck ready - 40 valid cards.", "readiness remains after persisted dismissal")
	view.queue_free()
	await Engine.get_main_loop().process_frame
	_cleanup(onboarding_path)


static func _test_deck_builder_edited_validation_and_restoration(t) -> void:
	var onboarding_path := "user://test-onboarding-edits.json"
	_cleanup(onboarding_path)
	var view = await _create_deck_builder(onboarding_path, Vector2(1280, 720))
	var shipped_cards: Array = view.model._cards().duplicate()
	view._on_add_card(str(shipped_cards[0]))
	t.assert_eq(view.get_node("%StarterStatus").text, "Deck Size", "added card shows the first concrete validator error")
	t.assert_true(view.get_node("%PlayButton").disabled, "added card disables Start Battle")
	t.assert_true("Starter deck ready" not in view.get_node("%StarterStatus").text, "edited copy loses preset readiness")

	view.model.select_deck("us-starter")
	view._refresh_deck()
	t.assert_eq(view.get_node("%StarterStatus").text, "Starter deck ready - 40 valid cards.", "returning to shipped deck restores readiness")
	t.assert_true(not view.get_node("%PlayButton").disabled, "restored shipped deck enables Start Battle")
	view.queue_free()
	await Engine.get_main_loop().process_frame

	view = await _create_deck_builder(onboarding_path + "-remove", Vector2(1280, 720))
	shipped_cards = view.model._cards().duplicate()
	view._on_remove_card(str(shipped_cards[0]))
	t.assert_eq(view.get_node("%StarterStatus").text, "Deck Size", "removed card shows the first concrete validator error")
	t.assert_true(view.get_node("%PlayButton").disabled, "removed card disables Start Battle")
	view.model.select_deck("us-starter")
	view._refresh_deck()
	t.assert_eq(view.get_node("%StarterStatus").text, "Starter deck ready - 40 valid cards.", "shipped deck readiness restores after removal edit")
	view.queue_free()
	await Engine.get_main_loop().process_frame
	_cleanup(onboarding_path)


static func _test_deck_builder_readiness_containment(t) -> void:
	for viewport_size in [Vector2(1280, 720), Vector2(1024, 720)]:
		var path := "user://test-onboarding-layout-%d.json" % int(viewport_size.x)
		_cleanup(path)
		var view = await _create_deck_builder(path, viewport_size)
		var strip := view.get_node("%StarterReadiness") as Control
		var viewport_rect := Rect2(Vector2.ZERO, viewport_size)
		t.assert_true(viewport_rect.encloses(strip.get_global_rect()), "readiness strip stays contained at %d" % int(viewport_size.x))
		t.assert_true(strip.get_global_rect().end.y <= (view.get_node("%Workspace") as Control).get_global_rect().position.y, "readiness strip stays above workspace at %d" % int(viewport_size.x))
		t.assert_true(strip.size.y <= 56.0, "readiness strip remains compact at %d" % int(viewport_size.x))
		view.queue_free()
		await Engine.get_main_loop().process_frame
		_cleanup(path)


static func _create_deck_builder(onboarding_path: String, viewport_size: Vector2):
	var root := Engine.get_main_loop().root as Window
	var view = DeckBuilderScene.instantiate()
	root.add_child(view)
	view.set_anchors_preset(Control.PRESET_TOP_LEFT)
	view.size = viewport_size
	var deck_store_path := onboarding_path.replace("onboarding", "decks")
	_cleanup(deck_store_path)
	view.initialize(null, {
		"catalog": ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json"),
		"deck_id": "us-starter",
		"difficulty": "standard",
		"store_path": deck_store_path,
		"onboarding_path": onboarding_path,
	})
	await view.get_tree().process_frame
	return view


static func _test_match_coach_and_card_states(t) -> void:
	var view = MatchViewScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	Engine.get_main_loop().root.size = Vector2i(1280, 720)
	view.render_snapshot(_snapshot())
	view.set_legal_actions([_action("deploy_unit", "one-cost", [], {"support_slot": 0}), _action("end_turn")])
	view.set_onboarding_state(OnboardingStore.defaults())
	await view.get_tree().process_frame
	t.assert_eq(view.get_node("%CoachObjective").text, "Select a highlighted card to deploy. You have 1 Credit.", "coach renders first-turn deploy objective")
	var cards := view.get_node("%PlayerHand").get_children()
	t.assert_eq(cards[0].action_state, "legal", "legal hand source is highlighted")
	t.assert_eq(cards[1].action_state, "unavailable", "illegal hand source is unavailable")
	t.assert_eq(cards[1].tooltip_text, "Not enough Credit", "unavailable source exposes concrete reason")
	view._on_card_pressed("one-cost")
	t.assert_eq(cards[0].action_state, "selected", "selected source has selected semantics")
	t.assert_eq(view.get_node("%CoachObjective").text, "Choose a highlighted Support Line slot.", "selection refreshes coach immediately")
	t.assert_eq(view.get_node("%StatusLabel").text, "", "precise coach copy replaces legacy selection status")
	var objective_height := (view.get_node("%CoachObjective") as Control).size.y
	t.assert_eq(view.get_node("%AnimationButton").text, "Animation: On", "match exposes animation preference")
	view.get_node("%AnimationButton").pressed.emit()
	t.assert_eq(view.animation_mode, "reduced", "animation command switches to reduced mode")
	view.show_rejection("stale_action", "That action is no longer legal.")
	t.assert_eq(view.get_node("%CoachObjective").text, "That action is no longer legal.", "rejection takes coach precedence")
	t.assert_eq((view.get_node("%CoachObjective") as Control).size.y, objective_height, "rejection does not resize coach strip")
	view.queue_free()
	await Engine.get_main_loop().process_frame


static func _test_rejection_refresh_ordering(t) -> void:
	var view = MatchViewScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	var snapshot := _snapshot().merged({"sequence": 12}, true)
	var actions := [_action("deploy_unit", "one-cost", [], {"support_slot": 0}), _action("end_turn")]
	view.render_snapshot(snapshot)
	view.set_legal_actions(actions)
	view.show_rejection("stale_action", "State changed")
	view.set_legal_actions(actions)
	t.assert_eq(view.get_node("%CoachObjective").text, "State changed", "legal refresh does not erase rejection")
	view.render_snapshot(snapshot)
	t.assert_eq(view.get_node("%CoachObjective").text, "State changed", "same-sequence rerender does not erase rejection")
	var advanced := snapshot.duplicate(true)
	advanced.sequence = 13
	view.render_snapshot(advanced)
	t.assert_eq(view.get_node("%CoachObjective").text, "Select a highlighted card to deploy. You have 1 Credit.", "authoritative sequence advance clears rejection")
	t.assert_eq(view.get_node("%StatusLabel").text, "", "authoritative sequence advance clears rejection status")
	view.show_rejection("stale_action", "State changed")
	view._on_card_pressed("one-cost")
	t.assert_eq(view.get_node("%CoachObjective").text, "Choose a highlighted Support Line slot.", "selection change clears rejection")
	view.show_rejection("stale_action", "State changed")
	view._on_cancel_pressed()
	t.assert_eq(view.get_node("%CoachObjective").text, "Select a highlighted card to deploy. You have 1 Credit.", "cancellation clears rejection")
	view.queue_free()
	await Engine.get_main_loop().process_frame


static func _test_end_turn_semantic_states(t) -> void:
	var view = MatchViewScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	view.render_snapshot(_snapshot())
	var button := view.get_node("%EndTurnButton") as Button
	var end := _action("end_turn")
	var deploy := _action("deploy_unit", "one-cost", [], {"support_slot": 0})
	view.set_legal_actions([])
	t.assert_true(button.disabled, "empty legal list disables End Turn")
	t.assert_eq(button.get_meta("action_state", ""), "disabled", "empty legal list has disabled semantics")
	view.set_legal_actions([deploy])
	t.assert_true(button.disabled, "non-End-Turn legal list disables End Turn")
	view.set_legal_actions([deploy, end])
	t.assert_true(not button.disabled, "exact End Turn candidate enables command")
	t.assert_eq(button.get_meta("action_state", ""), "normal", "mixed legal actions keep normal End Turn emphasis")
	view.set_legal_actions([end, end])
	t.assert_true(not button.disabled, "End Turn remains enabled whenever an exact candidate exists")
	t.assert_eq(button.get_meta("action_state", ""), "normal", "duplicate candidates are not sole-action emphasis")
	view.set_legal_actions([end])
	t.assert_true(not button.disabled, "sole End Turn remains enabled")
	t.assert_eq(button.get_meta("action_state", ""), "strong", "sole End Turn receives strong semantic emphasis")
	t.assert_true(button.get_theme_stylebox("normal").get_border_width(SIDE_LEFT) >= 3, "strong End Turn state has visible border emphasis")
	view.queue_free()
	await Engine.get_main_loop().process_frame


static func _test_unavailable_card_does_not_submit(t) -> void:
	var view = MatchViewScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	view.render_snapshot(_snapshot())
	view.set_legal_actions([_action("end_turn")])
	view.set_onboarding_state(OnboardingStore.defaults())
	var submitted := 0
	view.action_requested.connect(func(_action_value) -> void: submitted += 1)
	view._on_card_pressed("three-cost")
	t.assert_eq(submitted, 0, "unavailable click never submits an action")
	t.assert_eq(view.get_node("%StatusLabel").text, "Not enough Credit", "unavailable click displays concrete reason")
	view.queue_free()
	await Engine.get_main_loop().process_frame


static func _test_target_highlights_preserve_geometry(t) -> void:
	var view = MatchViewScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	Engine.get_main_loop().root.size = Vector2i(1024, 720)
	view.render_snapshot(_snapshot())
	await view.get_tree().process_frame
	var slot := view.get_node("%PlayerSupport").get_child(0) as Control
	var before := slot.get_global_rect()
	view.get_node("%PlayerSupport").set_highlights([0], [])
	await view.get_tree().process_frame
	t.assert_eq(slot.get_global_rect(), before, "strong slot highlight does not shift layout")
	t.assert_true(slot.get_theme_stylebox("normal").get_border_width(SIDE_LEFT) >= 3, "highlight uses a strong slot border")
	view.queue_free()
	await Engine.get_main_loop().process_frame


static func _test_stable_battlefield_grid(t) -> void:
	for viewport_width in [1280, 1024]:
		var view = MatchViewScene.instantiate()
		Engine.get_main_loop().root.add_child(view)
		Engine.get_main_loop().root.size = Vector2i(viewport_width, 720)
		view.render_snapshot(_populated_grid_snapshot())
		await view.get_tree().process_frame
		await view.get_tree().process_frame
		var expected_centers: Array[float] = []
		for zone_name in ["OpponentSupport", "Frontline", "PlayerSupport"]:
			var zone := view.get_node("%%%s" % zone_name) as Control
			var centers: Array[float] = zone.grid_column_centers()
			if expected_centers.is_empty(): expected_centers = centers
			t.assert_eq(centers, expected_centers, "%s shares stable five-column centers at %d" % [zone_name, viewport_width])
			t.assert_eq(centers.size(), 5, "%s exposes five grid cells at %d" % [zone_name, viewport_width])
			for slot in zone.get_children():
				var slot_control := slot as Control
				t.assert_true(slot_control.size.x >= 90.0 and slot_control.size.x <= 112.0, "slot width remains bounded at %d" % viewport_width)
				if slot_control.get_child_count() > 0:
					t.assert_true(slot_control.get_global_rect().encloses((slot_control.get_child(0) as Control).get_global_rect()), "card fills without escaping its stable slot")
		var grid_left: float = (view.get_node("%Frontline").get_child(0) as Control).get_global_rect().position.x
		t.assert_true((view.get_node("%OpponentHQ") as Control).get_global_rect().end.x < grid_left, "opponent HQ stays outside grid")
		t.assert_true((view.get_node("%PlayerHQ") as Control).get_global_rect().end.x < grid_left, "player HQ stays outside grid")
		var hand := view.get_node("%PlayerHand") as Control
		if hand.get_child_count() > 1:
			var hand_gap := (hand.get_child(1) as Control).position.x - (hand.get_child(0) as Control).position.x
			t.assert_eq(snappedf(hand_gap, 0.01), 124.0, "hand uses fixed card spacing")
		view.queue_free()
		await Engine.get_main_loop().process_frame


static func _test_visible_card_registry(t) -> void:
	var view = MatchViewScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	Engine.get_main_loop().root.size = Vector2i(1280, 720)
	var before := _populated_grid_snapshot()
	view.render_snapshot(before)
	await view.get_tree().process_frame
	var first_registry: Dictionary = view.visible_card_rects()
	t.assert_true(first_registry.has("shared-unit"), "public battlefield instance enters registry")
	t.assert_true(not first_registry.has("hidden-opponent-card"), "hidden opponent hand identity never enters registry")
	var after := before.duplicate(true)
	after.players.player.support_line[0] = null
	after.frontline[0] = before.players.player.support_line[0]
	view.render_snapshot(after)
	await view.get_tree().process_frame
	var second_registry: Dictionary = view.visible_card_rects()
	t.assert_eq(second_registry.keys().count("shared-unit"), 1, "reconciliation leaves one visible view per public instance")
	t.assert_true(second_registry["shared-unit"] is Rect2, "registry publicly exposes rectangles")
	view.queue_free()
	await Engine.get_main_loop().process_frame


static func _test_event_animation_queue(t) -> void:
	var view = MatchViewScene.instantiate()
	Engine.get_main_loop().root.add_child(view)
	Engine.get_main_loop().root.size = Vector2i(1280, 720)
	var before := _populated_grid_snapshot()
	var after := before.duplicate(true)
	view.render_snapshot(before)
	await view.get_tree().process_frame
	var events := [
		{"type": "card_drawn", "instance_id": "one-cost", "player_id": "player"},
		{"type": "card_deployed", "instance_id": "one-cost", "zone": "support_line", "slot": 0},
		{"type": "unit_moved", "instance_id": "shared-unit", "to_zone": "frontline", "to_slot": 0},
		{"type": "attack_started", "attacker_id": "front-0", "defender_id": "front-1"},
		{"type": "damage_dealt", "source_id": "front-0", "target_id": "front-1", "damage": 2},
		{"type": "card_destroyed", "instance_id": "front-1"},
		{"type": "order_played", "order_id": "targetless-order", "target_ids": ["front-0"]},
		{"type": "countermeasure_activated", "instance_id": "hidden-opponent-card", "player_id": "opponent"},
	]
	var director = CardMotionDirector.new()
	director.speed_scale = 0.01
	await director.play(events, before, after, view)
	t.assert_eq(director.processed_event_types, ["card_drawn", "card_deployed", "unit_moved", "attack_started", "damage_dealt", "card_destroyed", "order_played", "countermeasure_activated"], "accepted events animate in emitted order")
	t.assert_eq(view.get_tree().get_nodes_in_group("card_motion_proxy").size(), 0, "animation queue cleans motion proxies")
	t.assert_eq(view.get_tree().get_nodes_in_group("card_damage_indicator").size(), 0, "animation queue cleans damage indicators")
	view.set_animation_mode("reduced")
	await director.play([{"type": "unit_moved", "instance_id": "shared-unit"}], before, after, view)
	t.assert_true(director.last_duration_ms <= 80.0, "reduced mode caps animation duration")
	view.queue_free()
	await Engine.get_main_loop().process_frame


static func _test_real_main_milestone_submissions(t) -> void:
	var path := "user://test-onboarding-main-actions.json"
	_cleanup(path)
	var store = OnboardingStore.new(path)

	var deploy_controller := _real_action_controller("player")
	var deploy_action = _first_legal(deploy_controller, "player", "deploy_unit")
	t.assert_true(deploy_action != null, "real controller exposes accepted deploy candidate")
	var deploy_runtime := await _submit_through_main(deploy_controller, deploy_action, store)
	t.assert_true(deploy_runtime.controller.state.players.player.support_line.any(func(card) -> bool: return card != null), "Main submits deploy through real controller")
	t.assert_true(store.load().deployed_unit, "accepted Main deploy records milestone")
	await _free_runtime(deploy_runtime)

	var move_controller := _real_action_controller("player")
	var moving = move_controller.state.players.player.hand.pop_front()
	moving.zone = "support_line"
	moving.slot = 0
	moving.deployed_turn = move_controller.state.turn - 1
	moving.operations_used = 0
	move_controller.state.players.player.support_line[0] = moving
	var move_action = _first_legal(move_controller, "player", "move_unit")
	t.assert_true(move_action != null, "real controller exposes accepted Frontline move candidate")
	if move_action != null:
		var move_runtime := await _submit_through_main(move_controller, move_action, store)
		t.assert_true(store.load().moved_to_frontline, "accepted Main Frontline move records milestone")
		await _free_runtime(move_runtime)

	var attack_controller := _real_action_controller("player")
	var attacker = attack_controller.state.players.player.hand.pop_front()
	attacker.zone = "frontline"
	attacker.slot = 0
	attacker.deployed_turn = attack_controller.state.turn - 1
	attacker.operations_used = 0
	attack_controller.state.frontline[0] = attacker
	attack_controller.state.frontline_controller_id = "player"
	var attack_action = _first_legal(attack_controller, "player", "attack_hq")
	t.assert_true(attack_action != null, "real controller exposes accepted HQ attack candidate")
	if attack_action != null:
		var attack_runtime := await _submit_through_main(attack_controller, attack_action, store)
		t.assert_true(store.load().completed_attack, "accepted Main attack records milestone")
		await _free_runtime(attack_runtime)

	var rejected_path := "user://test-onboarding-main-rejected.json"
	_cleanup(rejected_path)
	var rejected_store = OnboardingStore.new(rejected_path)
	var rejected_controller := _real_action_controller("player")
	var stale = _first_legal(rejected_controller, "player", "deploy_unit")
	stale.expected_sequence += 99
	var rejected_runtime := await _submit_through_main(rejected_controller, stale, rejected_store)
	t.assert_eq(rejected_store.load(), OnboardingStore.defaults(), "rejected player submission records no milestone")
	t.assert_eq(rejected_runtime.main.current_screen.get_node("%CoachObjective").text, "State changed", "rejection survives Main snapshot/legal/unlock refresh ordering")
	var fresh_deploy = _first_legal(rejected_controller, "player", "deploy_unit")
	rejected_runtime.main.submit_player_action(fresh_deploy)
	for _frame in range(12):
		await Engine.get_main_loop().process_frame
		if not rejected_runtime.main._match_submission_active:
			break
	t.assert_true(rejected_runtime.main.current_screen.get_node("%CoachObjective").text != "State changed", "accepted state advance clears orchestrated rejection")
	await _free_runtime(rejected_runtime)

	var ai_path := "user://test-onboarding-main-ai.json"
	_cleanup(ai_path)
	var ai_store = OnboardingStore.new(ai_path)
	var ai_controller := _real_action_controller("opponent")
	var ai_runtime := _main_runtime(ai_controller, ai_store)
	ai_runtime.main.ai = AIPlayer.create("easy", 811)
	ai_runtime.main._match_generation = 1
	await ai_runtime.main._drive_ai_turn(1)
	t.assert_eq(ai_store.load(), OnboardingStore.defaults(), "accepted AI actions record no player milestones")
	await _free_runtime(ai_runtime)

	var reloaded = OnboardingStore.new(path)
	t.assert_true(reloaded.load().deployed_unit and reloaded.load().moved_to_frontline and reloaded.load().completed_attack, "all accepted milestones survive store reload")
	var rematch_controller := _real_action_controller("player")
	var rematch_runtime := _main_runtime(rematch_controller, reloaded)
	rematch_runtime.main.catalog = ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json")
	rematch_runtime.main.selected_player_deck = rematch_runtime.main.catalog.decks_by_id["us-starter"].duplicate(true)
	rematch_runtime.main.difficulty = "easy"
	rematch_runtime.main._on_rematch_requested()
	t.assert_true(reloaded.load().deployed_unit and reloaded.load().moved_to_frontline and reloaded.load().completed_attack, "real rematch initialization preserves onboarding milestones")
	await _free_runtime(rematch_runtime)
	_cleanup(path)
	_cleanup(rejected_path)
	_cleanup(ai_path)


static func _test_coach_priority_and_exact_copy(t) -> void:
	var snapshot := _snapshot()
	var deploy := _action("deploy_unit", "one-cost", [], {"support_slot": 0})
	var move := _action("move_unit", "support-unit", [], {"zone": "frontline", "slot": 0})
	var attack := _action("attack_unit", "front-unit", ["enemy-unit"])
	var order := _action("play_order", "order", ["enemy-unit"])
	var counter := _action("toggle_countermeasure", "counter")
	var ability := _action("activate_ability", "ability-unit")
	var end := _action("end_turn")

	_assert_coach(t, snapshot.merged({"active_player_id": "opponent"}, true), [deploy, end], {},
		"Opponent is acting.", ["one-cost"], "opponent_turn")
	_assert_coach(t, snapshot, [deploy, move, attack, order, counter, end], {"selected_source_id": "one-cost"},
		"Choose a highlighted Support Line slot.", ["counter", "front-unit", "one-cost", "order", "support-unit"], "support_slot")
	_assert_coach(t, snapshot, [move, attack, end], {"selected_source_id": "support-unit", "selected_zone": "frontline"},
		"Choose a highlighted Frontline slot.", ["front-unit", "support-unit"], "frontline_slot")
	_assert_coach(t, snapshot, [order, end], {"selected_source_id": "order"},
		"Choose a highlighted target.", ["order"], "target")
	var compound_order := _action("play_order", "order", ["enemy-unit", "enemy-hq"])
	_assert_coach(t, snapshot, [compound_order, end], {"selected_source_id": "order", "selected_targets": ["enemy-unit"]},
		"Choose a highlighted target.", ["order"], "target")
	_assert_coach(t, snapshot, [deploy, move, attack, order, counter, end], {},
		"Select a highlighted card to deploy. You have 1 Credit.", ["counter", "front-unit", "one-cost", "order", "support-unit"], "deploy")
	_assert_coach(t, snapshot, [move, attack, order, counter, end], {},
		"Select a ready unit, then choose a highlighted Frontline slot.", ["counter", "front-unit", "order", "support-unit"], "move")
	_assert_coach(t, snapshot, [attack, order, counter, end], {},
		"Select a ready unit, then choose a highlighted target.", ["counter", "front-unit", "order"], "attack")
	_assert_coach(t, snapshot, [order, counter, end], {},
		"Select a highlighted Order card to play.", ["counter", "order"], "order")
	_assert_coach(t, snapshot, [counter, end], {},
		"Select a highlighted Countermeasure card to activate or deactivate.", ["counter"], "countermeasure")
	_assert_coach(t, snapshot, [ability, end], {},
		"Select a ready unit to use an ability.", ["ability-unit"], "ability")
	_assert_coach(t, snapshot, [end], {},
		"No other actions are available. End the turn to gain another Credit slot.", [], "end_turn")
	_assert_coach(t, snapshot, [], {}, "No legal action is available.", [], "none")


static func _test_milestone_objective_progression(t) -> void:
	var snapshot := _snapshot()
	var actions := [
		_action("deploy_unit", "one-cost", [], {"support_slot": 0}),
		_action("move_unit", "support-unit", [], {"zone": "frontline", "slot": 0}),
		_action("attack_unit", "front-unit", ["enemy-unit"]),
		_action("end_turn"),
	]
	var onboarding := OnboardingStore.defaults()
	var expected_sources := ["front-unit", "one-cost", "support-unit"]
	var result := MatchCoachModel.derive(snapshot, actions, {}, onboarding)
	var expected_reasons: Dictionary = result.source_reasons.duplicate(true)
	t.assert_eq(result.next_kind, "deploy", "first incomplete milestone teaches deploy")
	t.assert_eq(result.legal_source_ids, expected_sources, "deploy teaching keeps every legal source")
	t.assert_true(not result.end_turn_only, "combined legal fixture is never End-Turn-only")
	onboarding.deployed_unit = true
	result = MatchCoachModel.derive(snapshot, actions, {}, onboarding)
	t.assert_eq(result.next_kind, "move", "completed deploy advances objective to move")
	t.assert_eq(result.legal_source_ids, expected_sources, "move teaching keeps every legal source")
	t.assert_eq(result.source_reasons, expected_reasons, "move teaching keeps complete-list source reasons")
	t.assert_true(not result.end_turn_only, "move teaching keeps complete-list End Turn semantics")
	onboarding.moved_to_frontline = true
	result = MatchCoachModel.derive(snapshot, actions, {}, onboarding)
	t.assert_eq(result.next_kind, "attack", "completed move advances objective to attack")
	t.assert_eq(result.legal_source_ids, expected_sources, "attack teaching keeps every legal source")
	t.assert_eq(result.source_reasons, expected_reasons, "attack teaching keeps complete-list source reasons")
	t.assert_true(not result.end_turn_only, "attack teaching keeps complete-list End Turn semantics")
	onboarding.completed_attack = true
	result = MatchCoachModel.derive(snapshot, actions, {}, onboarding)
	t.assert_eq(result.next_kind, "deploy", "completed milestones fall back to truthful normal priority")
	t.assert_eq(result.objective, "Select a highlighted card to deploy. You have 1 Credit.", "fallback objective describes an available action")
	t.assert_eq(result.legal_source_ids, expected_sources, "fallback keeps every legal source")
	t.assert_eq(result.source_reasons, expected_reasons, "fallback keeps complete-list source reasons")
	t.assert_true(not result.end_turn_only, "fallback keeps complete-list End Turn semantics")


static func _test_real_first_turn_credit_fixture(t) -> void:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 301)
	for action in [_action("start_match", "", [], {}, "system"), _action("mulligan"), _action("mulligan", "", [], {}, "opponent"), _action("confirm_mulligan"), _action("confirm_mulligan", "", [], {}, "opponent")]:
		controller.submit_action(action)
	if controller.state.active_player_id != "player":
		controller.submit_action(_action("end_turn", "", [], {}, "opponent"))
	var active_id: String = controller.state.active_player_id
	var active = controller.state.players[active_id]
	t.assert_eq(active.credit, 1, "real first turn starts with 1 Credit")
	active.hand[0].deployment_cost = 1
	active.hand[1].deployment_cost = 3
	var public_snapshot: Dictionary = controller.state.snapshot_for(active_id)
	var actions: Array[GameAction] = controller.legal_actions(active_id)
	var result := MatchCoachModel.derive(public_snapshot, actions, {}, {})
	t.assert_true(result.legal_source_ids.has(active.hand[0].instance_id), "one-Credit unit is a legal source")
	t.assert_true(not result.legal_source_ids.has(active.hand[1].instance_id), "three-Credit unit is not a legal source")
	t.assert_eq(result.objective, "Select a highlighted card to deploy. You have 1 Credit.", "real first-turn fixture uses exact deploy copy")


static func _test_real_active_countermeasure_is_legal(t) -> void:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 317)
	_start_player_turn(controller)
	var player = controller.state.players.player
	player.credit = 0
	var counter = player.hand[0]
	counter.category = "Countermeasure"
	counter.deployment_cost = 0
	counter.countermeasure_active = true
	controller.card_definitions[counter.definition_id]["category"] = "Countermeasure"
	controller.card_definitions[counter.definition_id]["deployment_cost"] = 0
	controller._definitions[counter.definition_id]["category"] = "Countermeasure"
	controller._definitions[counter.definition_id]["deployment_cost"] = 0
	player.active_countermeasures.append(counter)
	var legal_actions: Array[GameAction] = controller.legal_actions("player")
	var toggle_actions := legal_actions.filter(func(action: GameAction) -> bool:
		return action.type == "toggle_countermeasure" and action.source_id == counter.instance_id
	)
	var result := MatchCoachModel.derive(controller.state.snapshot_for("player"), legal_actions, {}, {})
	t.assert_eq(toggle_actions.size(), 1, "real active Countermeasure retains its legal deactivation toggle")
	t.assert_true(result.legal_source_ids.has(counter.instance_id), "active Countermeasure remains a legal coach source")
	t.assert_true(not result.source_reasons.has(counter.instance_id), "legal active Countermeasure is never unavailable")
	t.assert_eq(result.objective, "Select a highlighted Countermeasure card to activate or deactivate.", "active Countermeasure objective names both toggle directions")


static func _test_source_reasons(t) -> void:
	var snapshot := _snapshot()
	var result := MatchCoachModel.derive(snapshot, [_action("end_turn")], {}, {})
	t.assert_eq(result.source_reasons.get("three-cost"), "Not enough Credit", "unaffordable card explains Credit")
	t.assert_eq(result.source_reasons.get("targetless-order"), "No legal target", "targetless Order explains target")
	t.assert_eq(result.source_reasons.get("active-counter"), "Already active", "active Countermeasure explains state")
	t.assert_eq(result.source_reasons.get("unknown"), "No legal action for this card", "unknown category has safe fallback")

	var full := snapshot.duplicate(true)
	full.players.player.support_line = [_card("s0", "Unit", 0), _card("s1", "Unit", 0), _card("s2", "Unit", 0), _card("s3", "Unit", 0)]
	result = MatchCoachModel.derive(full, [_action("end_turn")], {}, {})
	t.assert_eq(result.source_reasons.get("one-cost"), "Support Line is full", "deployable Unit explains full Support Line")

	var opponent_turn := snapshot.merged({"active_player_id": "opponent"}, true)
	result = MatchCoachModel.derive(opponent_turn, [], {}, {})
	for source_id in ["one-cost", "three-cost", "targetless-order", "active-counter", "unknown"]:
		t.assert_eq(result.source_reasons.get(source_id), "Wait for your turn", "opponent turn reason overrides card details")


static func _test_end_turn_requires_the_sole_complete_action(t) -> void:
	var snapshot := _snapshot()
	var sole := MatchCoachModel.derive(snapshot, [_action("end_turn")], {}, {})
	t.assert_true(sole.end_turn_only, "one End Turn action is sole-action guidance")
	var duplicate := MatchCoachModel.derive(snapshot, [_action("end_turn"), _action("end_turn")], {}, {})
	t.assert_true(not duplicate.end_turn_only, "duplicate End Turn candidates are not a sole complete action list")
	var mixed := MatchCoachModel.derive(snapshot, [_action("end_turn"), _action("activate_ability", "support-unit")], {}, {})
	t.assert_true(not mixed.end_turn_only, "any additional legal action disables sole-End-Turn semantics")


static func _test_persistence(t) -> void:
	var path := "user://test-onboarding.json"
	_cleanup(path)
	var store = OnboardingStore.new(path)
	var defaults: Dictionary = store.load()
	t.assert_eq(defaults, {
		"deck_hint_dismissed": false,
		"deployed_unit": false,
		"moved_to_frontline": false,
		"completed_attack": false,
	}, "missing persistence uses safe defaults")
	t.assert_true(store.dismiss_deck_hint(), "dismissal persists atomically")
	for milestone in ["deployed_unit", "moved_to_frontline", "completed_attack"]:
		t.assert_true(store.complete(milestone), "%s persists atomically" % milestone)
	t.assert_true(not FileAccess.file_exists(path + ".tmp"), "atomic save leaves no temporary file")
	t.assert_eq(OnboardingStore.new(path).load(), {
		"deck_hint_dismissed": true,
		"deployed_unit": true,
		"moved_to_frontline": true,
		"completed_attack": true,
	}, "saved onboarding reloads")
	t.assert_true(not store.complete("not_a_milestone"), "unknown milestones are rejected")

	var corrupt := FileAccess.open(path, FileAccess.WRITE)
	corrupt.store_string("{broken")
	corrupt.close()
	t.assert_eq(OnboardingStore.new(path).load(), OnboardingStore.defaults(), "corrupt JSON falls back safely")

	var unwritable = OnboardingStore.new("user://missing-parent/test-onboarding.json")
	_cleanup("user://missing-parent/test-onboarding.json")
	_cleanup_dir("user://missing-parent")
	unwritable.load()
	t.assert_true(not unwritable.complete("deployed_unit"), "unwritable path reports failure")
	t.assert_true(unwritable.load().deployed_unit, "failed persistence preserves in-memory milestone")
	_cleanup(path)


static func _test_persistence_rejects_non_user_paths(t) -> void:
	var invalid_paths := [
		"res://onboarding-invalid.json",
		"/tmp/opencards-onboarding-invalid.json",
		"user://../opencards-onboarding-invalid.json",
		"user://nested/../../opencards-onboarding-invalid.json",
	]
	for path in invalid_paths:
		var absolute := ProjectSettings.globalize_path(path)
		if FileAccess.file_exists(absolute):
			DirAccess.remove_absolute(absolute)
		var store = OnboardingStore.new(path)
		t.assert_eq(store.load(), OnboardingStore.defaults(), "%s reads safe defaults" % path)
		t.assert_true(not store.complete("deployed_unit"), "%s rejects persistence" % path)
		t.assert_true(store.load().deployed_unit, "%s preserves failed write in memory" % path)
		t.assert_true(not FileAccess.file_exists(absolute), "%s cannot create a file" % path)


static func _assert_coach(t, snapshot: Dictionary, actions: Array, selection: Dictionary, objective: String, sources: Array, next_kind: String) -> void:
	var result := MatchCoachModel.derive(snapshot, actions, selection, {})
	t.assert_eq(result.objective, objective, "%s objective copy" % next_kind)
	t.assert_eq(result.legal_source_ids, sources, "%s legal source IDs" % next_kind)
	t.assert_eq(result.next_kind, next_kind, "%s next kind" % next_kind)


static func _snapshot() -> Dictionary:
	return {
		"phase": "action",
		"active_player_id": "player",
		"players": {
			"player": {
				"credit": 1,
				"support_line": [null, null, null, null],
				"hand": [
					_card("one-cost", "Unit", 1),
					_card("three-cost", "Unit", 3),
					_card("targetless-order", "Order", 1),
					_card("active-counter", "Countermeasure", 0, true),
					_card("unknown", "Mystery", 0),
				],
			},
			"opponent": {"credit": 1, "support_line": [null, null, null, null], "hand": []},
		},
		"frontline": [null, null, null, null, null],
	}


static func _populated_grid_snapshot() -> Dictionary:
	var value := _snapshot()
	value.players.player["headquarters"] = _card("player-hq", "Headquarters", 0)
	value.players.opponent["headquarters"] = _card("opponent-hq", "Headquarters", 0)
	value.players.opponent.hand = [{"hidden": true}]
	value.players.opponent.support_line = [
		_card("enemy-support-0", "Unit", 1), _card("enemy-support-1", "Unit", 1),
		_card("enemy-support-2", "Unit", 1), _card("enemy-support-3", "Unit", 1),
	]
	value.players.player.support_line = [
		_card("shared-unit", "Unit", 1), _card("player-support-1", "Unit", 1),
		_card("player-support-2", "Unit", 1), _card("player-support-3", "Unit", 1),
	]
	value.frontline = [
		_card("front-0", "Unit", 1), _card("front-1", "Unit", 1), _card("front-2", "Unit", 1),
		_card("front-3", "Unit", 1), _card("front-4", "Unit", 1),
	]
	return value


static func _card(instance_id: String, category: String, cost: int, active: bool = false) -> Dictionary:
	return {"instance_id": instance_id, "category": category, "deployment_cost": cost, "countermeasure_active": active, "zone": "hand"}


static func _action(type: String, source_id: String = "", targets: Array[String] = [], payload: Dictionary = {}, actor_id: String = "player") -> GameAction:
	return GameAction.create(type, actor_id, source_id, targets, payload)


static func _start_player_turn(controller: MatchController) -> void:
	for action in [_action("start_match", "", [], {}, "system"), _action("mulligan"), _action("mulligan", "", [], {}, "opponent"), _action("confirm_mulligan"), _action("confirm_mulligan", "", [], {}, "opponent")]:
		controller.submit_action(action)
	if controller.state.active_player_id != "player":
		controller.submit_action(_action("end_turn", "", [], {}, "opponent"))


static func _real_action_controller(active_player_id: String) -> MatchController:
	var fixture: Dictionary = CoreCards.build_valid_fixture()
	var controller: MatchController = MatchController.create(fixture.definitions, fixture.player_deck, fixture.enemy_deck, 809)
	_start_player_turn(controller)
	if active_player_id == "opponent":
		var end_action = _first_legal(controller, "player", "end_turn")
		controller.submit_action(end_action)
	return controller


static func _first_legal(controller: MatchController, actor_id: String, action_type: String):
	for action in controller.legal_actions(actor_id):
		if action.type == action_type:
			return action
	return null


static func _main_runtime(controller: MatchController, store) -> Dictionary:
	var main := Main.new()
	var host := Control.new()
	Engine.get_main_loop().root.add_child(host)
	var view = MatchViewScene.instantiate()
	host.add_child(view)
	main.screen_host = host
	main.current_screen = view
	main.controller = controller
	main.ai = null
	main.onboarding_store = store
	view.render_snapshot(controller.state.snapshot_for("player"))
	view.set_legal_actions(controller.legal_actions("player"))
	view.set_onboarding_state(store.load())
	return {"main": main, "host": host, "controller": controller}


static func _submit_through_main(controller: MatchController, action, store) -> Dictionary:
	var runtime := _main_runtime(controller, store)
	runtime.main.submit_player_action(action)
	for _frame in range(12):
		await Engine.get_main_loop().process_frame
		if not runtime.main._match_submission_active:
			break
	return runtime


static func _free_runtime(runtime: Dictionary) -> void:
	var main = runtime.main
	var host = runtime.host
	main.free()
	host.queue_free()
	await Engine.get_main_loop().process_frame


static func _cleanup(path: String) -> void:
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path))
	if FileAccess.file_exists(path + ".tmp"):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path + ".tmp"))


static func _cleanup_dir(path: String) -> void:
	var absolute := ProjectSettings.globalize_path(path)
	if DirAccess.dir_exists_absolute(absolute):
		DirAccess.remove_absolute(absolute)
