const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")
const MatchCoachModel = preload("res://scripts/ui/match_coach_model.gd")
const OnboardingStore = preload("res://scripts/ui/onboarding_store.gd")


static func run(t) -> void:
	_test_coach_priority_and_exact_copy(t)
	_test_real_first_turn_credit_fixture(t)
	_test_real_active_countermeasure_is_legal(t)
	_test_source_reasons(t)
	_test_end_turn_requires_the_sole_complete_action(t)
	_test_persistence(t)
	_test_persistence_rejects_non_user_paths(t)


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


static func _card(instance_id: String, category: String, cost: int, active: bool = false) -> Dictionary:
	return {"instance_id": instance_id, "category": category, "deployment_cost": cost, "countermeasure_active": active, "zone": "hand"}


static func _action(type: String, source_id: String = "", targets: Array[String] = [], payload: Dictionary = {}, actor_id: String = "player") -> GameAction:
	return GameAction.create(type, actor_id, source_id, targets, payload)


static func _start_player_turn(controller: MatchController) -> void:
	for action in [_action("start_match", "", [], {}, "system"), _action("mulligan"), _action("mulligan", "", [], {}, "opponent"), _action("confirm_mulligan"), _action("confirm_mulligan", "", [], {}, "opponent")]:
		controller.submit_action(action)
	if controller.state.active_player_id != "player":
		controller.submit_action(_action("end_turn", "", [], {}, "opponent"))


static func _cleanup(path: String) -> void:
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path))
	if FileAccess.file_exists(path + ".tmp"):
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path + ".tmp"))


static func _cleanup_dir(path: String) -> void:
	var absolute := ProjectSettings.globalize_path(path)
	if DirAccess.dir_exists_absolute(absolute):
		DirAccess.remove_absolute(absolute)
