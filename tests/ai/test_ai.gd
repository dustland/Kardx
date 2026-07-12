extends RefCounted

const ActionGenerator = preload("res://scripts/ai/action_generator.gd")
const BoardEvaluator = preload("res://scripts/ai/board_evaluator.gd")
const CardInstance = preload("res://scripts/core/card_instance.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")


static func run(t) -> void:
	_test_rich_midgame_actions_are_complete_legal_and_stable(t)
	_test_generator_rejects_wrong_actor_and_terminal_states(t)
	_test_simulation_clone_isolated_from_source(t)
	_test_evaluator_respects_information_boundary(t)


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


static func _test_generator_rejects_wrong_actor_and_terminal_states(t) -> void:
	var controller := _rich_controller(701)
	t.assert_eq(ActionGenerator.generate(controller, "opponent").size(), 0, "inactive actor has no actions")
	t.assert_eq(ActionGenerator.generate(controller, "unknown").size(), 0, "unknown actor has no actions")
	controller.state.phase = "complete"
	controller.state.winner_id = "player"
	t.assert_eq(ActionGenerator.generate(controller, "player").size(), 0, "terminal match has no actions")
	t.assert_eq(controller.legal_actions("player").size(), 0, "controller exposes no terminal actions")


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
