extends RefCounted

const ActionGenerator = preload("res://scripts/ai/action_generator.gd")
const AIPlayer = preload("res://scripts/ai/ai_player.gd")
const BoardEvaluator = preload("res://scripts/ai/board_evaluator.gd")
const CardInstance = preload("res://scripts/core/card_instance.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")


static func run(t) -> void:
	_test_ai_player_factory_normalizes_difficulties(t)
	_test_ai_player_exposes_safe_chooser_contract(t)
	_test_ai_choices_are_deterministic_legal_and_isolated(t)
	_test_easy_seed_breaks_equal_action_ties(t)
	_test_search_retains_negative_score_ties(t)
	_test_standard_looks_ahead_for_a_tactical_sequence(t)
	_test_standard_avoids_suicidal_attack_when_deployment_is_better(t)
	_test_hard_takes_an_immediate_lethal(t)
	_test_ai_end_turn_fallback_and_safe_null_cases(t)
	_test_ai_respects_hidden_information_boundary(t)
	_test_rich_midgame_actions_are_complete_legal_and_stable(t)
	_test_manual_adjacent_enemy_anchor_actions(t)
	_test_action_keys_preserve_delimited_ids_and_typed_payloads(t)
	_test_generator_rejects_wrong_actor_and_terminal_states(t)
	_test_malformed_generator_inputs_are_safe(t)
	_test_generator_requires_exact_player_map(t)
	_test_simulation_clone_isolated_from_source(t)
	_test_evaluator_respects_information_boundary(t)
	_test_evaluator_handles_malformed_snapshots(t)
	_test_evaluator_requires_exact_player_map(t)


static func _test_ai_player_factory_normalizes_difficulties(t) -> void:
	var ai_script = load("res://scripts/ai/ai_player.gd")
	t.assert_true(ai_script != null, "AIPlayer script exists")
	if ai_script == null:
		return
	var easy = ai_script.create("easy", 11)
	var standard = ai_script.create("standard", 11)
	var hard = ai_script.create("hard", 11)
	var fallback = ai_script.create("unknown", 11)
	t.assert_eq(easy.difficulty, "easy", "easy difficulty is retained")
	t.assert_eq([easy.node_budget, easy.max_depth, easy.beam_width], [32, 1, 4], "easy has exact search limits")
	t.assert_eq([standard.node_budget, standard.max_depth, standard.beam_width], [256, 4, 8], "standard has exact search limits")
	t.assert_eq([hard.node_budget, hard.max_depth, hard.beam_width], [1200, 8, 16], "hard has exact search limits")
	t.assert_eq(fallback.difficulty, "standard", "invalid difficulty falls back safely to standard")


static func _test_ai_player_exposes_safe_chooser_contract(t) -> void:
	var ai_script = load("res://scripts/ai/ai_player.gd")
	if ai_script == null:
		return
	var ai = ai_script.create("standard", 12)
	t.assert_true(ai.has_method("choose_action"), "AIPlayer exposes choose_action")


static func _test_ai_choices_are_deterministic_legal_and_isolated(t) -> void:
	for difficulty in ["easy", "standard", "hard"]:
		var controller := _rich_controller(720)
		var before_hash := controller.state_hash()
		var before_events := controller.event_history.duplicate(true)
		var before_replay := controller.replay_log.to_dict()
		var first := AIPlayer.create(difficulty, 99)
		var second := AIPlayer.create(difficulty, 99)
		var action = first.choose_action(controller, "player")
		var repeated = second.choose_action(controller, "player")
		t.assert_true(_contains_action(ActionGenerator.generate(controller, "player"), action), "%s returns a legal action" % difficulty)
		t.assert_eq(_action_data(action), _action_data(repeated), "%s is deterministic for the same seed and state" % difficulty)
		t.assert_true(first.visited_nodes > 0 and first.visited_nodes <= first.node_budget, "%s search respects its node budget" % difficulty)
		t.assert_eq(controller.state_hash(), before_hash, "%s choice preserves source state and RNG" % difficulty)
		t.assert_eq(controller.event_history, before_events, "%s choice preserves source events" % difficulty)
		t.assert_eq(controller.replay_log.to_dict(), before_replay, "%s choice preserves source replay" % difficulty)


static func _test_easy_seed_breaks_equal_action_ties(t) -> void:
	var choices := {}
	for seed in range(1, 17):
		var action = AIPlayer.create("easy", seed).choose_action(_tied_deployment_controller(721), "player")
		choices[JSON.stringify(_action_data(action))] = true
	t.assert_true(choices.size() > 1, "different easy seeds vary equal-priority choices")


static func _test_search_retains_negative_score_ties(t) -> void:
	var ai := AIPlayer.create("standard", 100)
	var best_nodes: Array[Dictionary] = []
	var first := {"score": -2.0, "first_action": GameAction.create("deploy_unit", "player", "alpha")}
	var second := {"score": -2.0, "first_action": GameAction.create("deploy_unit", "player", "beta")}
	var best_score := ai._record_best(first, -3.0, best_nodes)
	ai._record_best(second, best_score, best_nodes)
	t.assert_eq(best_nodes.size(), 2, "search retains equal improvements even when their scores are negative")


static func _test_standard_looks_ahead_for_a_tactical_sequence(t) -> void:
	var controller := _sequence_controller(722)
	var action = AIPlayer.create("standard", 101).choose_action(controller, "player")
	t.assert_eq(action.type, "activate_ability", "standard starts the sequence that creates a lethal attack")
	t.assert_eq(action.source_id, "tactician", "standard chooses the tactical setup source")


static func _test_standard_avoids_suicidal_attack_when_deployment_is_better(t) -> void:
	var action = AIPlayer.create("standard", 102).choose_action(_suicide_or_deploy_controller(723), "player")
	t.assert_eq(action.type, "deploy_unit", "standard deploys instead of taking the suicidal attack")


static func _test_hard_takes_an_immediate_lethal(t) -> void:
	var action = AIPlayer.create("hard", 103).choose_action(_lethal_controller(724), "player")
	t.assert_eq(action.type, "attack_hq", "hard chooses an immediate Headquarters lethal")


static func _test_ai_end_turn_fallback_and_safe_null_cases(t) -> void:
	var fallback_controller := _empty_action_controller(725)
	var fallback = AIPlayer.create("standard", 104).choose_action(fallback_controller, "player")
	t.assert_eq(fallback.type, "end_turn", "AI returns legal end turn when no command improves the board")
	var ai := AIPlayer.create("hard", 105)
	var malformed = ai.choose_action(null, "player")
	t.assert_eq([malformed.type, malformed.actor_id], ["", ""], "malformed controller returns recognizable null action")
	var wrong_actor = ai.choose_action(fallback_controller, "opponent")
	t.assert_eq([wrong_actor.type, wrong_actor.actor_id], ["", ""], "wrong actor returns recognizable null action")
	fallback_controller.state.phase = "complete"
	var terminal = ai.choose_action(fallback_controller, "player")
	t.assert_eq([terminal.type, terminal.actor_id], ["", ""], "terminal match returns recognizable null action")


static func _test_ai_respects_hidden_information_boundary(t) -> void:
	var visible := _rich_controller(726)
	var substituted = visible.clone_for_simulation("player")
	substituted.state.players.opponent.hand[0].definition_id = "hidden-substitution"
	substituted.state.players.opponent.hand[0].title = "not visible to player"
	var original_action = AIPlayer.create("hard", 106).choose_action(visible, "player")
	var substituted_action = AIPlayer.create("hard", 106).choose_action(substituted, "player")
	t.assert_eq(_action_data(original_action), _action_data(substituted_action), "hidden enemy identities do not affect AI choice")


static func _test_rich_midgame_actions_are_complete_legal_and_stable(t) -> void:
	var controller := _rich_controller(700)
	var before_hash := controller.state_hash()
	var before_events := controller.event_history.duplicate(true)
	var before_replay := controller.replay_log.to_dict()
	var actions: Array = ActionGenerator.generate(controller, "player")
	var repeated: Array = ActionGenerator.generate(controller, "player")

	t.assert_true(not actions.is_empty(), "rich midgame has legal actions")
	t.assert_eq(_action_dicts(actions), _action_dicts(repeated), "generation order is deterministic")
	t.assert_eq(_action_dicts(actions), _action_dicts(controller.legal_actions("player")), "controller delegates to generator")
	t.assert_true(_has_action(actions, "deploy_unit", "deploy-unit", ["enemy-support"]), "deploy includes required target")
	t.assert_eq(_count_source(actions, "deploy_unit", "deploy-unit"), _open_support_slots(controller), "deploy covers each open Support slot")
	t.assert_true(_has_action(actions, "play_order", "targeted-order", ["enemy-support"]), "Order has complete unit target")
	t.assert_true(_has_action(actions, "play_order", "targeted-order", [controller.state.players.opponent.headquarters.instance_id]), "Order has complete Headquarters target")
	t.assert_true(_has_action(actions, "toggle_countermeasure", "counter-active"), "Countermeasure deactivation is generated")
	t.assert_true(_has_action(actions, "toggle_countermeasure", "counter-ready"), "Countermeasure activation is generated")
	t.assert_eq(_count_source(actions, "move_unit", "mover"), GameConstants.FRONTLINE_SLOTS - 1, "move covers every open Frontline slot")
	t.assert_true(_has_action(actions, "attack_unit", "attacker", ["enemy-support"]), "unit attack is generated")
	t.assert_true(_has_action(actions, "attack_hq", "attacker", [controller.state.players.opponent.headquarters.instance_id]), "Headquarters attack is generated")
	t.assert_true(_has_action(actions, "activate_ability", "manual-source", ["mover"]), "manual ability target is generated")
	t.assert_true(_has_action(actions, "end_turn"), "end turn is generated")

	for action in actions:
		var clone = controller.clone_for_simulation("player")
		t.assert_true(clone.submit_action(action).accepted, "generated action is legal: %s" % action.type)
	t.assert_eq(controller.state_hash(), before_hash, "generation does not mutate authoritative state or RNG")
	t.assert_eq(controller.event_history, before_events, "generation does not append source events")
	t.assert_eq(controller.replay_log.to_dict(), before_replay, "generation does not write source replay")


static func _test_manual_adjacent_enemy_anchor_actions(t) -> void:
	var controller := _rich_controller(704)
	var source := _card(controller.card_definitions, "adjacent-source", "player", "adjacent-source")
	_place_support(controller, source, 2)
	var anchor := _card(controller.card_definitions, "enemy-support", "opponent", "enemy-anchor")
	_place_support(controller, anchor, 1)
	var actions: Array = ActionGenerator.generate(controller, "player")
	t.assert_true(
		_has_action(actions, "activate_ability", "adjacent-source", ["enemy-anchor"]),
		"manual adjacent-enemy ability includes each accepted enemy anchor"
	)
	for action in actions.filter(func(action): return action.source_id == "adjacent-source"):
		var clone = controller.clone_for_simulation("player")
		t.assert_true(clone.submit_action(action).accepted, "adjacent anchor action is legal on a clone")


static func _test_action_keys_preserve_delimited_ids_and_typed_payloads(t) -> void:
	var controller := _collision_controller(705)
	var actions: Array = ActionGenerator.generate(controller, "player")
	t.assert_true(
		_has_action(actions, "activate_ability", "a", ["b|c,d"]),
		"delimited source and target IDs remain distinct"
	)
	t.assert_true(
		_has_action(actions, "activate_ability", "a|b", ["c,d"]),
		"delimiter collision does not deduplicate another legal action"
	)
	var typed_payloads: Array[GameAction] = [
		GameAction.create("activate_ability", "player", "source", [], {1: "value"}),
		GameAction.create("activate_ability", "player", "source", [], {"1": "value"}),
	]
	var deduplicated := ActionGenerator._deduplicate_and_sort(typed_payloads)
	t.assert_eq(deduplicated.size(), 2, "typed payload keys remain distinct")
	t.assert_eq(
		_action_dicts(deduplicated),
		_action_dicts(ActionGenerator._deduplicate_and_sort(typed_payloads)),
		"typed action sort is stable"
	)


static func _test_generator_rejects_wrong_actor_and_terminal_states(t) -> void:
	var controller := _rich_controller(701)
	t.assert_eq(ActionGenerator.generate(controller, "opponent").size(), 0, "inactive actor has no actions")
	t.assert_eq(ActionGenerator.generate(controller, "unknown").size(), 0, "unknown actor has no actions")
	controller.state.phase = "complete"
	controller.state.winner_id = "player"
	t.assert_eq(ActionGenerator.generate(controller, "player").size(), 0, "terminal match has no actions")
	t.assert_eq(controller.legal_actions("player").size(), 0, "controller exposes no terminal actions")


static func _test_malformed_generator_inputs_are_safe(t) -> void:
	t.assert_eq(ActionGenerator.generate(null, "player"), [], "null controller has no actions")
	t.assert_eq(ActionGenerator.generate({}, "player"), [], "non-controller has no actions")
	var missing_state := MatchController.new()
	t.assert_eq(ActionGenerator.generate(missing_state, "player"), [], "missing state has no actions")
	var malformed_players := _rich_controller(706)
	malformed_players.state.players = {"player": null}
	t.assert_eq(ActionGenerator.generate(malformed_players, "player"), [], "malformed players have no actions")
	var malformed_hand := _rich_controller(707)
	malformed_hand.state.players.player.hand = [null, {}]
	t.assert_eq(ActionGenerator.generate(malformed_hand, "player"), [], "malformed card entries have no actions")


static func _test_generator_requires_exact_player_map(t) -> void:
	var one_player := _rich_controller(708)
	var player = one_player.state.players.player
	one_player.state.players = {"player": player}
	t.assert_eq(ActionGenerator.generate(one_player, "player"), [], "one-player state has no actions before simulation")
	var three_players := _rich_controller(709)
	three_players.state.players["third"] = three_players.state.players.player
	t.assert_eq(ActionGenerator.generate(three_players, "player"), [], "extra player state has no actions")
	var missing_expected := _rich_controller(710)
	missing_expected.state.players = {"player": missing_expected.state.players.player, "third": missing_expected.state.players.opponent}
	t.assert_eq(ActionGenerator.generate(missing_expected, "player"), [], "missing opponent state has no actions")
	var wrong_entry := _rich_controller(711)
	wrong_entry.state.players = {"player": wrong_entry.state.players.player, "opponent": {}}
	t.assert_eq(ActionGenerator.generate(wrong_entry, "player"), [], "wrong player entry has no actions")


static func _test_simulation_clone_isolated_from_source(t) -> void:
	var controller := _rich_controller(702)
	var clone = controller.clone_for_simulation("player")
	t.assert_true(clone != controller, "simulation returns another controller")
	t.assert_true(clone.state != controller.state, "simulation state is independent")
	t.assert_true(clone.replay_log == null, "simulation disables replay logging")
	t.assert_eq(clone.state_hash(), controller.state_hash(), "simulation preserves authoritative state and RNG")
	clone.state.players.player.credit = 0
	clone.state.players.player.hand[0].title = "changed only in simulation"
	t.assert_true(controller.state.players.player.credit > 0, "simulation cannot mutate source player")
	t.assert_true(controller.state.players.player.hand[0].title != "changed only in simulation", "simulation cannot leak mutable cards")


static func _test_evaluator_respects_information_boundary(t) -> void:
	var controller := _rich_controller(703)
	var snapshot := controller.state.snapshot_for("player")
	var enemy_hand: Array = snapshot.players.opponent.hand
	t.assert_true(enemy_hand.size() > 0, "perspective safely exposes hidden hand count")
	for card in enemy_hand:
		t.assert_true(bool(card.hidden), "enemy hand identity remains hidden")
	var substituted := snapshot.duplicate(true)
	substituted.players.opponent.hand[0] = {"hidden": true, "definition_id": "not-visible"}
	substituted.players.opponent["deck_order"] = [{"definition_id": "not-visible"}]
	t.assert_eq(
		BoardEvaluator.score(snapshot, "player"),
		BoardEvaluator.score(substituted, "player"),
		"hidden enemy identity and deck order cannot affect evaluation"
	)
	var balanced := {
		"players": {
			"player": {"hq_defense": 10, "hand": [{"hidden": true}], "credit": 4, "support_line": []},
			"opponent": {"hq_defense": 10, "hand": [{"hidden": true}], "credit": 4, "support_line": []},
		},
		"frontline": [],
		"frontline_controller_id": "",
	}
	t.assert_eq(BoardEvaluator.score(balanced, "player"), -BoardEvaluator.score(balanced, "opponent"), "symmetric visible state is antisymmetric")


static func _test_evaluator_handles_malformed_snapshots(t) -> void:
	t.assert_eq(BoardEvaluator.score(null, "player"), 0.0, "null snapshot is neutral")
	t.assert_eq(BoardEvaluator.score([], "player"), 0.0, "non-dictionary snapshot is neutral")
	t.assert_eq(BoardEvaluator.score({"players": []}, "player"), 0.0, "non-dictionary players are neutral")
	t.assert_eq(BoardEvaluator.score({
		"players": {"player": [], "opponent": {}},
		"frontline": "not-an-array",
	}, "player"), 0.0, "non-dictionary player snapshot is neutral")
	var partial := BoardEvaluator.score({
		"players": {
			"player": {"hq_defense": "wrong", "hand": "wrong", "credit": 3, "support_line": "wrong"},
			"opponent": {"hq_defense": 4, "hand_count": "wrong", "credit": "wrong", "support_line": []},
		},
		"frontline": [{"category": "Unit", "attack": "wrong", "defense": 2}],
		"frontline_controller_id": "player",
	}, "player")
	t.assert_eq(partial, -19.25, "malformed nested values receive a stable partial score")


static func _test_evaluator_requires_exact_player_map(t) -> void:
	var player := {"hq_defense": 10, "hand": [], "credit": 3, "support_line": []}
	var opponent := {"hq_defense": 4, "hand": [], "credit": 1, "support_line": []}
	t.assert_eq(BoardEvaluator.score({"players": {"player": player}}, "player"), 0.0, "one-player snapshot is neutral")
	t.assert_eq(BoardEvaluator.score({
		"players": {"player": player, "opponent": opponent, "third": {"hq_defense": 7}},
	}, "player"), 0.0, "three-player snapshot is neutral")
	t.assert_eq(BoardEvaluator.score({
		"players": {"player": player, "third": opponent},
	}, "player"), 0.0, "snapshot missing expected opponent is neutral")
	t.assert_eq(BoardEvaluator.score({
		"players": {"player": player, "opponent": []},
	}, "player"), 0.0, "wrong expected player entry is neutral")


static func _rich_controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": _definition("p-hq", "Headquarters", 0, 20),
		"o-hq": _definition("o-hq", "Headquarters", 0, 20),
		"deploy-unit": _definition("deploy-unit", "Unit", 2, 4, 1, 1, [_ability("deploy-hit", "deploy", {"selector": "enemy_unit", "count": 1}, [{"type": "damage", "amount": 1}])]),
		"targeted-order": _definition("targeted-order", "Order", 0, 0, 1, 0, [_ability("order-hit", "play_order", {"selector": "enemy_unit_or_hq", "count": 1}, [{"type": "damage", "amount": 1}])]),
		"counter": _definition("counter", "Countermeasure", 0, 0, 1),
		"mover": _definition("mover", "Unit", 2, 4, 1, 1),
		"attacker": _definition("attacker", "Unit", 3, 5, 1, 1),
		"manual-source": _definition("manual-source", "Unit", 1, 5, 1, 0, [_ability("manual-buff", "manual", {"selector": "friendly_unit", "count": 1}, [{"type": "buff", "attack": 1, "defense": 0}])]),
		"adjacent-source": _definition("adjacent-source", "Unit", 1, 5, 1, 0, [_ability("adjacent-hit", "manual", {"selector": "adjacent_enemy_units"}, [{"type": "damage", "amount": 1}])]),
		"enemy-support": _definition("enemy-support", "Unit", 1, 5, 1, 1),
	}
	var controller := MatchController.create(definitions, ["p-hq"], ["o-hq"], seed)
	controller.state.phase = "action"
	controller.state.active_player_id = "player"
	controller.state.starting_player_id = "player"
	controller.state.turn = 5
	for player_id in controller.state.players:
		controller.state.players[player_id].credit_slots = 10
		controller.state.players[player_id].credit = 10

	_put_hand(controller, _card(definitions, "deploy-unit", "player", "deploy-unit"))
	_put_hand(controller, _card(definitions, "targeted-order", "player", "targeted-order"))
	var active_counter := _card(definitions, "counter", "player", "counter-active")
	active_counter.countermeasure_active = true
	active_counter.countermeasure_activation_cost = 1
	active_counter.face_down = true
	_put_hand(controller, active_counter)
	controller.state.players.player.active_countermeasures.append(active_counter)
	_put_hand(controller, _card(definitions, "counter", "player", "counter-ready"))
	var mover := _card(definitions, "mover", "player", "mover")
	_place_support(controller, mover, 0)
	_ready(controller, mover)
	var manual := _card(definitions, "manual-source", "player", "manual-source")
	_place_support(controller, manual, 1)
	var attacker := _card(definitions, "attacker", "player", "attacker")
	_place_frontline(controller, attacker, 0)
	_ready(controller, attacker)
	var enemy := _card(definitions, "enemy-support", "opponent", "enemy-support")
	_place_support(controller, enemy, 0)
	_ready(controller, enemy)
	_put_hand(controller, _card(definitions, "enemy-support", "opponent", "enemy-hidden"))
	return controller


static func _collision_controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": _definition("p-hq", "Headquarters", 0, 20),
		"o-hq": _definition("o-hq", "Headquarters", 0, 20),
		"manual-target": _definition("manual-target", "Unit", 1, 5, 0, 0, [_ability("same", "manual", {"selector": "friendly_unit", "count": 1}, [{"type": "buff", "attack": 1, "defense": 0}])]),
		"plain": _definition("plain", "Unit", 1, 5),
	}
	var controller := MatchController.create(definitions, ["p-hq"], ["o-hq"], seed)
	controller.state.phase = "action"
	controller.state.active_player_id = "player"
	controller.state.starting_player_id = "player"
	controller.state.turn = 5
	controller.state.players.player.credit = 10
	_place_support(controller, _card(definitions, "manual-target", "player", "a"), 0)
	_place_support(controller, _card(definitions, "manual-target", "player", "a|b"), 1)
	_place_support(controller, _card(definitions, "plain", "player", "b|c,d"), 2)
	_place_support(controller, _card(definitions, "plain", "player", "c,d"), 3)
	return controller


static func _tied_deployment_controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": _definition("p-hq", "Headquarters", 0, 20),
		"o-hq": _definition("o-hq", "Headquarters", 0, 20),
		"alpha": _definition("alpha", "Unit", 2, 2, 1),
		"beta": _definition("beta", "Unit", 2, 2, 1),
	}
	var controller := _empty_action_controller_with_definitions(definitions, seed)
	controller.state.players.player.credit = 3
	_put_hand(controller, _card(definitions, "alpha", "player", "alpha"))
	_put_hand(controller, _card(definitions, "beta", "player", "beta"))
	return controller


static func _sequence_controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": _definition("p-hq", "Headquarters", 0, 20),
		"o-hq": _definition("o-hq", "Headquarters", 0, 20),
		"tactician": _definition("tactician", "Unit", 1, 4, 0, 0, [_ability("rally", "manual", {"selector": "friendly_unit", "count": 1}, [{"type": "buff", "attack": 2, "defense": 0}])]),
		"striker": _definition("striker", "Unit", 2, 4, 0, 0),
	}
	var controller := _empty_action_controller_with_definitions(definitions, seed)
	controller.state.players.player.credit = 5
	controller.state.players.opponent.headquarters.current_defense = 3
	var tactician := _card(definitions, "tactician", "player", "tactician")
	var striker := _card(definitions, "striker", "player", "striker")
	striker.unit_type = "Artillery"
	_place_support(controller, tactician, 0)
	_place_support(controller, striker, 1)
	_ready(controller, tactician)
	_ready(controller, striker)
	return controller


static func _suicide_or_deploy_controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": _definition("p-hq", "Headquarters", 0, 20),
		"o-hq": _definition("o-hq", "Headquarters", 0, 20),
		"scout": _definition("scout", "Unit", 1, 1, 0, 0),
		"brute": _definition("brute", "Unit", 9, 9, 0, 0),
		"reinforcement": _definition("reinforcement", "Unit", 4, 4, 1, 0),
	}
	var controller := _empty_action_controller_with_definitions(definitions, seed)
	controller.state.players.player.credit = 3
	var scout := _card(definitions, "scout", "player", "scout")
	var brute := _card(definitions, "brute", "opponent", "brute")
	_place_frontline(controller, scout, 0)
	brute.keywords.append("Guard")
	_place_support(controller, brute, 1)
	_ready(controller, scout)
	_ready(controller, brute)
	_put_hand(controller, _card(definitions, "reinforcement", "player", "reinforcement"))
	return controller


static func _lethal_controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": _definition("p-hq", "Headquarters", 0, 20),
		"o-hq": _definition("o-hq", "Headquarters", 0, 20),
		"finisher": _definition("finisher", "Unit", 4, 4, 0, 0),
	}
	var controller := _empty_action_controller_with_definitions(definitions, seed)
	controller.state.players.opponent.headquarters.current_defense = 4
	var finisher := _card(definitions, "finisher", "player", "finisher")
	_place_frontline(controller, finisher, 0)
	_ready(controller, finisher)
	return controller


static func _empty_action_controller(seed: int) -> MatchController:
	return _empty_action_controller_with_definitions({
		"p-hq": _definition("p-hq", "Headquarters", 0, 20),
		"o-hq": _definition("o-hq", "Headquarters", 0, 20),
	}, seed)


static func _empty_action_controller_with_definitions(definitions: Dictionary, seed: int) -> MatchController:
	var controller := MatchController.create(definitions, ["p-hq"], ["o-hq"], seed)
	controller.state.phase = "action"
	controller.state.active_player_id = "player"
	controller.state.starting_player_id = "player"
	controller.state.turn = 5
	return controller


static func _definition(id: String, category: String, attack: int, defense: int, deployment_cost: int = 0, operation_cost: int = 0, abilities: Array = []) -> Dictionary:
	return {
		"id": id,
		"title": id,
		"nation": "Test",
		"category": category,
		"rarity": "Standard",
		"unit_type": "Infantry",
		"attack": attack,
		"defense": defense,
		"deployment_cost": deployment_cost,
		"operation_cost": operation_cost,
		"keywords": [],
		"abilities": abilities.duplicate(true),
	}


static func _ability(id: String, trigger: String, target: Dictionary, effects: Array) -> Dictionary:
	return {"id": id, "trigger": trigger, "target": target, "effects": effects, "allowed_zones": ["support_line", "frontline"]}


static func _card(definitions: Dictionary, definition_id: String, owner_id: String, instance_id: String) -> CardInstance:
	return CardInstance.from_definition(definitions[definition_id], owner_id, instance_id)


static func _put_hand(controller: MatchController, card: CardInstance) -> void:
	controller.state.players[card.owner_id].hand.append(card)
	card.zone = "hand"
	card.slot = -1


static func _place_support(controller: MatchController, card: CardInstance, slot: int) -> void:
	controller.state.players[card.owner_id].support_line[slot] = card
	card.zone = "support_line"
	card.slot = slot


static func _place_frontline(controller: MatchController, card: CardInstance, slot: int) -> void:
	controller.state.frontline[slot] = card
	controller.state.frontline_controller_id = card.owner_id
	card.zone = "frontline"
	card.slot = slot


static func _ready(controller: MatchController, card: CardInstance) -> void:
	card.deployed_turn = controller.state.turn - 1
	card.operations_used = 0


static func _open_support_slots(controller: MatchController) -> int:
	var count := 0
	for card in controller.state.players.player.support_line:
		if card == null:
			count += 1
	return count


static func _has_action(actions: Array, type: String, source_id: String = "", targets: Array[String] = []) -> bool:
	for action in actions:
		if action.type == type and action.source_id == source_id and action.target_ids == targets:
			return true
	return false


static func _count_source(actions: Array, type: String, source_id: String) -> int:
	var count := 0
	for action in actions:
		if action.type == type and action.source_id == source_id:
			count += 1
	return count


static func _action_dicts(actions: Array) -> Array:
	var values: Array = []
	for action in actions:
		values.append({
			"type": action.type,
			"actor_id": action.actor_id,
			"source_id": action.source_id,
			"target_ids": action.target_ids.duplicate(),
			"payload": action.payload.duplicate(true),
			"expected_sequence": action.expected_sequence,
		})
	return values


static func _contains_action(actions: Array, expected: GameAction) -> bool:
	for action in actions:
		if _action_data(action) == _action_data(expected):
			return true
	return false


static func _action_data(action: GameAction) -> Dictionary:
	return {
		"type": action.type,
		"actor_id": action.actor_id,
		"source_id": action.source_id,
		"target_ids": action.target_ids.duplicate(),
		"payload": action.payload.duplicate(true),
		"expected_sequence": action.expected_sequence,
	}
