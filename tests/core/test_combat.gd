extends RefCounted

const CardInstance = preload("res://scripts/core/card_instance.gd")
const CombatRules = preload("res://scripts/core/combat_rules.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")


static func run(t) -> void:
	_test_deploy_and_support_capacity(t)
	_test_move_frontline_control_and_capacity(t)
	_test_type_target_matrix(t)
	_test_guard_and_smokescreen(t)
	_test_smokescreen_reveals_after_accepted_operations(t)
	_test_blitz_fury_and_pinned(t)
	_test_tank_advance_chain(t)
	_test_damage_counterattack_and_destruction(t)
	_test_lethal_retaliation_boundaries(t)
	_test_ambush_first_strike(t)
	_test_headquarters_guard_and_locked_target(t)
	_test_headquarters_victory(t)
	_test_rejections_are_atomic(t)


static func _test_deploy_and_support_capacity(t) -> void:
	var controller := _controller(411)
	var player = controller.state.players.player
	var unit := _card("deploy-unit", "player", "Infantry", 2, 3, 2, 1)
	player.hand.append(unit)
	unit.zone = "hand"
	var deploy = controller.submit_action(GameAction.create(
		"deploy_unit", "player", unit.instance_id, [], {"support_slot": 0}, controller.state.sequence
	))
	t.assert_true(deploy.accepted, "unit deploys to Support Line")
	t.assert_eq(player.credit, 8, "deployment pays deployment Credit")
	t.assert_eq(player.support_line[0], unit, "deployed unit occupies requested Support slot")
	t.assert_eq(unit.deployed_turn, controller.state.turn, "deployment records summoning turn")
	t.assert_eq(_event_types(deploy.events), ["credit_spent", "card_deployed"], "deployment emits replay events in order")

	for slot in range(1, GameConstants.SUPPORT_UNIT_SLOTS):
		_place_support(controller, _card("support-%d" % slot, "player"), slot)
	var overflow := _card("support-overflow", "player")
	player.hand.append(overflow)
	overflow.zone = "hand"
	var before := _digest(controller)
	var rejected = controller.submit_action(GameAction.create(
		"deploy_unit", "player", overflow.instance_id, [], {"support_slot": 0}, controller.state.sequence
	))
	t.assert_eq(rejected.reason_code, "support_line_full", "full Support Line has stable rejection")
	t.assert_eq(_digest(controller), before, "full Support Line rejection is atomic")


static func _test_move_frontline_control_and_capacity(t) -> void:
	var controller := _controller(412)
	var player = controller.state.players.player
	var infantry := _card("p-infantry", "player", "Infantry", 2, 3, 0, 2)
	_place_support(controller, infantry, 0)
	infantry.deployed_turn = controller.state.turn
	var before_sick := _digest(controller)
	var early_move = controller.submit_action(GameAction.create(
		"move_unit", "player", infantry.instance_id, [], {"zone": "frontline", "slot": 0}, controller.state.sequence
	))
	t.assert_eq(early_move.reason_code, "deployment_sickness", "non-Blitz cannot operate on deployment turn")
	t.assert_eq(_digest(controller), before_sick, "deployment sickness rejection is atomic")

	_ready(controller, infantry)
	var move = controller.submit_action(GameAction.create(
		"move_unit", "player", infantry.instance_id, [], {"zone": "frontline", "slot": 0}, controller.state.sequence
	))
	t.assert_true(move.accepted, "ready unit captures empty Frontline")
	t.assert_eq(player.credit, 8, "move pays operation Credit")
	t.assert_eq(infantry.operations_used, 1, "move consumes operation allowance")
	t.assert_eq(controller.state.frontline_controller_id, "player", "Frontline has one controller")
	t.assert_eq(_event_types(move.events), ["credit_spent", "unit_moved", "frontline_changed"], "move emits payment, movement, then control")

	controller.state.active_player_id = "opponent"
	var enemy := _card("o-infantry", "opponent")
	_place_support(controller, enemy, 0)
	_ready(controller, enemy)
	var enemy_before := _digest(controller)
	var enemy_move = controller.submit_action(GameAction.create(
		"move_unit", "opponent", enemy.instance_id, [], {"zone": "frontline", "slot": 1}, controller.state.sequence
	))
	t.assert_eq(enemy_move.reason_code, "enemy_controls_frontline", "opponents cannot share Frontline")
	t.assert_eq(_digest(controller), enemy_before, "enemy Frontline rejection is atomic")

	var full := _controller(413)
	for slot in range(GameConstants.FRONTLINE_SLOTS):
		_place_frontline(full, _card("front-%d" % slot, "player"), slot)
	var waiting := _card("waiting", "player")
	_place_support(full, waiting, 0)
	_ready(full, waiting)
	var full_before := _digest(full)
	var full_move = full.submit_action(GameAction.create(
		"move_unit", "player", waiting.instance_id, [], {"zone": "frontline", "slot": 0}, full.state.sequence
	))
	t.assert_eq(full_move.reason_code, "frontline_full", "five occupied Frontline slots reject movement")
	t.assert_eq(_digest(full), full_before, "full Frontline rejection is atomic")


static func _test_type_target_matrix(t) -> void:
	for unit_type in ["Infantry", "Tank"]:
		var support_controller := _controller(420)
		var ground := _card("ground-%s" % unit_type, "player", unit_type)
		var enemy_front := _card("enemy-front-%s" % unit_type, "opponent")
		var enemy_support := _card("enemy-support-%s" % unit_type, "opponent")
		_place_support(support_controller, ground, 0)
		_place_support(support_controller, enemy_support, 0)
		_place_frontline(support_controller, enemy_front, 0)
		_ready(support_controller, ground)
		t.assert_eq(
			CombatRules.legal_targets(support_controller.state, ground.instance_id),
			[enemy_front.instance_id],
			"%s in Support targets only enemy Frontline" % unit_type
		)

		var frontline_controller := _controller(421)
		var advanced := _card("advanced-%s" % unit_type, "player", unit_type)
		var rear := _card("rear-%s" % unit_type, "opponent")
		_place_frontline(frontline_controller, advanced, 0)
		_place_support(frontline_controller, rear, 0)
		_ready(frontline_controller, advanced)
		t.assert_eq(
			CombatRules.legal_targets(frontline_controller.state, advanced.instance_id),
			[rear.instance_id, frontline_controller.state.players.opponent.headquarters.instance_id],
			"%s in owned Frontline targets Support and HQ" % unit_type
		)

	for unit_type in ["Artillery", "Fighter", "Bomber"]:
		var controller := _controller(422)
		var attacker := _card("long-%s" % unit_type, "player", unit_type)
		var enemy_front := _card("long-front-%s" % unit_type, "opponent")
		var enemy_support := _card("long-support-%s" % unit_type, "opponent")
		_place_support(controller, attacker, 0)
		_place_support(controller, enemy_support, 0)
		_place_frontline(controller, enemy_front, 0)
		_ready(controller, attacker)
		t.assert_eq(
			CombatRules.legal_targets(controller.state, attacker.instance_id),
			[enemy_front.instance_id, enemy_support.instance_id, controller.state.players.opponent.headquarters.instance_id],
			"%s has long-range access to both lines and HQ" % unit_type
		)


static func _test_guard_and_smokescreen(t) -> void:
	for unit_type in ["Infantry", "Tank", "Fighter"]:
		var controller := _controller(430)
		var attacker := _card("guard-attacker-%s" % unit_type, "player", unit_type)
		var guard := _card("guard-%s" % unit_type, "opponent", "Infantry", 1, 4, 0, 0, ["Guard"])
		var protected := _card("protected-%s" % unit_type, "opponent")
		var unprotected := _card("unprotected-%s" % unit_type, "opponent")
		_place_support(controller, attacker, 0)
		_place_frontline(controller, guard, 1)
		_place_frontline(controller, protected, 2)
		_place_frontline(controller, unprotected, 4)
		_ready(controller, attacker)
		var targets := CombatRules.legal_targets(controller.state, attacker.instance_id)
		t.assert_true(targets.has(guard.instance_id), "%s may target Guard itself" % unit_type)
		t.assert_true(not targets.has(protected.instance_id), "%s respects adjacent Guard" % unit_type)
		t.assert_true(targets.has(unprotected.instance_id), "%s may target non-adjacent unit" % unit_type)

	for unit_type in ["Artillery", "Bomber"]:
		var controller := _controller(431)
		var attacker := _card("ignore-guard-%s" % unit_type, "player", unit_type)
		var guard := _card("ignored-guard-%s" % unit_type, "opponent", "Infantry", 1, 4, 0, 0, ["Guard"])
		var protected := _card("ignored-protected-%s" % unit_type, "opponent")
		_place_support(controller, attacker, 0)
		_place_frontline(controller, guard, 1)
		_place_frontline(controller, protected, 2)
		_ready(controller, attacker)
		t.assert_true(
			CombatRules.legal_targets(controller.state, attacker.instance_id).has(protected.instance_id),
			"%s ignores Guard" % unit_type
		)

	var support_controller := _controller(433)
	var support_attacker := _card("support-guard-attacker", "player", "Infantry")
	var support_guard := _card("support-guard", "opponent", "Infantry", 1, 4, 0, 0, ["Guard"])
	var support_protected := _card("support-protected", "opponent")
	var support_unprotected := _card("support-unprotected", "opponent")
	_place_frontline(support_controller, support_attacker, 0)
	_place_support(support_controller, support_guard, 1)
	_place_support(support_controller, support_protected, 2)
	_place_support(support_controller, support_unprotected, 3)
	_ready(support_controller, support_attacker)
	var support_targets := CombatRules.legal_targets(support_controller.state, support_attacker.instance_id)
	t.assert_true(support_targets.has(support_guard.instance_id), "Support Line Guard can be targeted directly")
	t.assert_true(not support_targets.has(support_protected.instance_id), "Support Line Guard protects its adjacent unit")
	t.assert_true(support_targets.has(support_unprotected.instance_id), "Support Line Guard does not protect non-adjacent units")

	for unit_type in ["Infantry", "Tank", "Artillery", "Fighter", "Bomber"]:
		var smoke_controller := _controller(432)
		var attacker := _card("smoke-attacker-%s" % unit_type, "player", unit_type)
		var smoke := _card("smoke-target-%s" % unit_type, "opponent", "Infantry", 1, 4, 0, 0, ["Smokescreen"])
		_place_support(smoke_controller, attacker, 0)
		_place_frontline(smoke_controller, smoke, 0)
		_ready(smoke_controller, attacker)
		t.assert_true(
			not CombatRules.legal_targets(smoke_controller.state, attacker.instance_id).has(smoke.instance_id),
			"Smokescreen blocks %s before operating" % unit_type
		)


static func _test_smokescreen_reveals_after_accepted_operations(t) -> void:
	var move_controller := _controller(434)
	var moving_smoke := _card("moving-smoke", "player", "Infantry", 2, 4, 0, 1, ["Smokescreen"])
	_place_support(move_controller, moving_smoke, 0)
	_ready(move_controller, moving_smoke)
	var move = move_controller.submit_action(GameAction.create(
		"move_unit", "player", moving_smoke.instance_id, [], {"zone": "frontline", "slot": 0}, move_controller.state.sequence
	))
	t.assert_true(move.accepted, "Smokescreen unit can move to the Frontline")
	t.assert_true(move.snapshot.frontline[0].smokescreen_revealed, "accepted move reveals Smokescreen in its snapshot")

	var attack_controller := _controller(435)
	var attacking_smoke := _card("attacking-smoke", "player", "Artillery", 2, 4, 0, 1, ["Smokescreen"])
	var target := _card("smokescreen-attack-target", "opponent")
	_place_support(attack_controller, attacking_smoke, 0)
	_place_support(attack_controller, target, 0)
	_ready(attack_controller, attacking_smoke)
	var attack = _attack(attack_controller, attacking_smoke, target)
	t.assert_true(attack.accepted, "Smokescreen unit can attack")
	t.assert_true(attack.snapshot.players.player.support_line[0].smokescreen_revealed, "accepted attack reveals Smokescreen in its snapshot")


static func _test_blitz_fury_and_pinned(t) -> void:
	var blitz_controller := _controller(440)
	var blitz := _card("blitz", "player", "Infantry", 2, 3, 0, 1, ["Blitz"])
	var target := _card("blitz-target", "opponent")
	_place_support(blitz_controller, blitz, 0)
	_place_frontline(blitz_controller, target, 0)
	blitz.deployed_turn = blitz_controller.state.turn
	var blitz_attack = blitz_controller.submit_action(GameAction.create(
		"attack_unit", "player", blitz.instance_id, [target.instance_id], {}, blitz_controller.state.sequence
	))
	t.assert_true(blitz_attack.accepted, "Blitz permits deployment-turn attack")

	var pinned_controller := _controller(441)
	var pinned := _card("pinned", "player", "Artillery", 2, 3, 0, 1, ["Pinned"])
	var pinned_target := _card("pinned-target", "opponent")
	_place_support(pinned_controller, pinned, 0)
	_place_support(pinned_controller, pinned_target, 0)
	_ready(pinned_controller, pinned)
	var pinned_before := _digest(pinned_controller)
	var pinned_attack = pinned_controller.submit_action(GameAction.create(
		"attack_unit", "player", pinned.instance_id, [pinned_target.instance_id], {}, pinned_controller.state.sequence
	))
	t.assert_eq(pinned_attack.reason_code, "pinned", "Pinned unit cannot operate")
	t.assert_eq(_digest(pinned_controller), pinned_before, "Pinned rejection is atomic")

	var fury_controller := _controller(442)
	var fury := _card("fury", "player", "Artillery", 1, 5, 0, 2, ["Fury"])
	var first := _card("fury-first", "opponent", "Infantry", 0, 5)
	var second := _card("fury-second", "opponent", "Infantry", 0, 5)
	_place_support(fury_controller, fury, 0)
	_place_support(fury_controller, first, 0)
	_place_support(fury_controller, second, 1)
	_ready(fury_controller, fury)
	var initial_credit: int = fury_controller.state.players.player.credit
	var attack_one = _attack(fury_controller, fury, first)
	var attack_two = _attack(fury_controller, fury, second)
	var before_third := _digest(fury_controller)
	var attack_three = _attack(fury_controller, fury, first)
	t.assert_true(attack_one.accepted and attack_two.accepted, "Fury permits two operations")
	t.assert_eq(fury_controller.state.players.player.credit, initial_credit - 4, "Fury pays each operation cost")
	t.assert_eq(attack_three.reason_code, "operation_limit", "Fury rejects a third operation")
	t.assert_eq(_digest(fury_controller), before_third, "Fury limit rejection is atomic")


static func _test_tank_advance_chain(t) -> void:
	var controller := _controller(450)
	var tank := _card("tank-chain", "player", "Tank", 2, 6, 0, 3)
	var target := _card("tank-chain-target", "opponent", "Infantry", 0, 8)
	_place_support(controller, tank, 0)
	_place_support(controller, target, 0)
	_ready(controller, tank)
	var initial_credit: int = controller.state.players.player.credit
	var move = controller.submit_action(GameAction.create(
		"move_unit", "player", tank.instance_id, [], {"zone": "frontline", "slot": 0}, controller.state.sequence
	))
	t.assert_true(move.accepted, "Tank advances to Frontline")
	t.assert_eq(tank.operations_used, 1, "Tank advance reserves one operation")
	t.assert_eq(tank.operation_chain, CardInstance.OperationChain.TANK_ADVANCE, "Tank advance opens typed chain state")
	t.assert_eq(move.snapshot.frontline[0].operation_chain, CardInstance.OperationChain.TANK_ADVANCE, "Tank chain is serialized by accepted move")
	t.assert_eq(controller.state.players.player.credit, initial_credit - 3, "Tank advance pays once")
	var invalid_chain_before := _digest(controller)
	var invalid_chain = _attack(controller, tank, tank)
	t.assert_eq(invalid_chain.reason_code, "invalid_target", "invalid chained attack is rejected")
	t.assert_eq(_digest(controller), invalid_chain_before, "invalid chained attack preserves payment and open chain")
	var chained_attack = _attack(controller, tank, target)
	t.assert_true(chained_attack.accepted, "Tank may attack after advancing")
	t.assert_eq(tank.operations_used, 1, "chained Tank attack reuses reserved operation")
	t.assert_eq(tank.operation_chain, CardInstance.OperationChain.NONE, "Tank attack closes chain state")
	t.assert_eq(controller.state.players.player.credit, initial_credit - 3, "chained Tank attack does not pay twice")
	t.assert_true(not _event_types(chained_attack.events).has("credit_spent"), "chained Tank attack emits no duplicate payment")
	var before_reuse := _digest(controller)
	var reused = _attack(controller, tank, target)
	t.assert_eq(reused.reason_code, "operation_limit", "closed non-Fury Tank chain cannot be reused")
	t.assert_eq(_digest(controller), before_reuse, "closed chain rejection is atomic")

	var fury_controller := _controller(451)
	var fury_tank := _card("fury-tank", "player", "Tank", 1, 8, 0, 2, ["Fury"])
	var first := _card("fury-tank-first", "opponent", "Infantry", 0, 8)
	var second := _card("fury-tank-second", "opponent", "Infantry", 0, 8)
	_place_support(fury_controller, fury_tank, 0)
	_place_support(fury_controller, first, 0)
	_place_support(fury_controller, second, 1)
	_ready(fury_controller, fury_tank)
	var fury_credit: int = fury_controller.state.players.player.credit
	var fury_move = fury_controller.submit_action(GameAction.create(
		"move_unit", "player", fury_tank.instance_id, [], {"zone": "frontline", "slot": 0}, fury_controller.state.sequence
	))
	var fury_chain = _attack(fury_controller, fury_tank, first)
	var fury_extra = _attack(fury_controller, fury_tank, second)
	var before_excess := _digest(fury_controller)
	var fury_excess = _attack(fury_controller, fury_tank, first)
	t.assert_true(fury_move.accepted and fury_chain.accepted and fury_extra.accepted, "Fury Tank gets chain plus one operation")
	t.assert_eq(fury_tank.operations_used, 2, "Fury Tank consumes exactly two operation allowances")
	t.assert_eq(fury_controller.state.players.player.credit, fury_credit - 4, "Fury Tank pays once per operation, not action")
	t.assert_eq(fury_excess.reason_code, "operation_limit", "Fury Tank rejects operation beyond chain plus one")
	t.assert_eq(_digest(fury_controller), before_excess, "Fury Tank excess rejection is atomic")

	var reset_controller := _controller(452)
	var reset_tank := _card("reset-tank", "player", "Tank", 2, 6, 0, 1)
	_place_support(reset_controller, reset_tank, 0)
	_ready(reset_controller, reset_tank)
	var reset_move = reset_controller.submit_action(GameAction.create(
		"move_unit", "player", reset_tank.instance_id, [], {"zone": "frontline", "slot": 0}, reset_controller.state.sequence
	))
	t.assert_true(reset_move.accepted, "Tank move opens a chain before turn reset")
	var opponent_turn = reset_controller.submit_action(GameAction.create(
		"end_turn", "player", "", [], {}, reset_controller.state.sequence
	))
	var reset_turn = reset_controller.submit_action(GameAction.create(
		"end_turn", "opponent", "", [], {}, reset_controller.state.sequence
	))
	t.assert_true(opponent_turn.accepted and reset_turn.accepted, "turns advance through Tank reset")
	t.assert_eq(reset_turn.snapshot.frontline[0].operation_chain, CardInstance.OperationChain.NONE, "turn reset serializes a closed Tank chain")
	t.assert_eq(reset_turn.snapshot.frontline[0].operations_used, 0, "turn reset serializes restored Tank operations")


static func _test_damage_counterattack_and_destruction(t) -> void:
	var armor_controller := _controller(460)
	var artillery := _card("armor-artillery", "player", "Artillery", 4, 4, 0, 1)
	var armor := _card("heavy-armor", "opponent", "Tank", 2, 6, 0, 0, ["Heavy Armor"])
	_place_support(armor_controller, artillery, 0)
	_place_support(armor_controller, armor, 0)
	_ready(armor_controller, artillery)
	var armor_attack = _attack(armor_controller, artillery, armor)
	t.assert_true(armor_attack.accepted, "Artillery attacks Heavy Armor")
	t.assert_eq(armor.current_defense, 3, "Heavy Armor reduces incoming damage by one")
	t.assert_eq(artillery.current_defense, 4, "Artillery receives no counterattack")

	var counter_controller := _controller(461)
	var fighter := _card("counter-fighter", "player", "Fighter", 2, 5, 0, 1)
	var defender := _card("counter-defender", "opponent", "Infantry", 3, 5)
	_place_support(counter_controller, fighter, 0)
	_place_support(counter_controller, defender, 0)
	_ready(counter_controller, fighter)
	var counter = _attack(counter_controller, fighter, defender)
	t.assert_true(counter.accepted, "Fighter attack resolves")
	t.assert_eq(defender.current_defense, 3, "attack damage applies")
	t.assert_eq(fighter.current_defense, 2, "Fighter receives counterattack")
	t.assert_eq(_event_types(counter.events), ["credit_spent", "attack_started", "damage_dealt", "damage_dealt"], "attack and counterattack checkpoints are ordered")

	var destroy_controller := _controller(462)
	var bomber := _card("destroy-bomber", "player", "Bomber", 6, 4, 0, 1)
	var victim := _card("destroy-victim", "opponent", "Infantry", 5, 2)
	_place_support(destroy_controller, bomber, 0)
	_place_frontline(destroy_controller, victim, 0)
	_ready(destroy_controller, bomber)
	var destroy = _attack(destroy_controller, bomber, victim)
	t.assert_true(destroy.accepted, "lethal Bomber attack resolves")
	t.assert_eq(victim.current_defense, 0, "damage clamps unit defense at zero")
	t.assert_eq(victim.zone, "discard", "destroyed unit moves to discard")
	t.assert_true(destroy_controller.state.players.opponent.discard.has(victim), "owner discard receives destroyed unit")
	t.assert_eq(destroy_controller.state.frontline_controller_id, "", "destroying final Frontline unit clears control")
	t.assert_eq(bomber.current_defense, 4, "Bomber receives no counterattack")

	for unit_type in ["Infantry", "Tank", "Fighter", "Artillery", "Bomber"]:
		var table_controller := _controller(463)
		var table_attacker := _card("table-attacker-%s" % unit_type, "player", unit_type, 1, 7, 0, 1)
		var table_defender := _card("table-defender-%s" % unit_type, "opponent", "Infantry", 2, 7)
		_place_support(table_controller, table_attacker, 0)
		_place_frontline(table_controller, table_defender, 0)
		_ready(table_controller, table_attacker)
		var table_attack = _attack(table_controller, table_attacker, table_defender)
		t.assert_true(table_attack.accepted, "%s counterattack boundary attack resolves" % unit_type)
		var expected_defense := 5 if unit_type in ["Infantry", "Tank", "Fighter"] else 7
		t.assert_eq(
			table_attacker.current_defense,
			expected_defense,
			"%s counterattack table entry is enforced" % unit_type
		)


static func _test_lethal_retaliation_boundaries(t) -> void:
	for unit_type in ["Infantry", "Tank", "Fighter"]:
		var controller := _controller(464)
		var attacker := _card("lethal-attacker-%s" % unit_type, "player", unit_type, 3, 6, 0, 1)
		var defender := _card("lethal-defender-%s" % unit_type, "opponent", "Infantry", 2, 3)
		_place_support(controller, attacker, 0)
		_place_frontline(controller, defender, 0)
		_ready(controller, attacker)
		var result = _attack(controller, attacker, defender)
		t.assert_true(result.accepted, "%s lethal direct attack resolves" % unit_type)
		t.assert_eq(attacker.current_defense, 4, "%s receives snapshotted retaliation from lethal defender" % unit_type)
		t.assert_eq(defender.zone, "discard", "%s lethal defender is destroyed after retaliation" % unit_type)
		t.assert_eq(
			_event_types(result.events),
			["credit_spent", "attack_started", "damage_dealt", "damage_dealt", "card_destroyed", "frontline_changed"],
			"%s lethal attack resolves retaliation before destruction" % unit_type
		)

	for unit_type in ["Artillery", "Bomber"]:
		var controller := _controller(465)
		var attacker := _card("lethal-ranged-attacker-%s" % unit_type, "player", unit_type, 3, 6, 0, 1)
		var defender := _card("lethal-ranged-defender-%s" % unit_type, "opponent", "Infantry", 2, 3)
		_place_support(controller, attacker, 0)
		_place_frontline(controller, defender, 0)
		_ready(controller, attacker)
		var result = _attack(controller, attacker, defender)
		t.assert_true(result.accepted, "%s lethal direct attack resolves" % unit_type)
		t.assert_eq(attacker.current_defense, 6, "%s lethal attack receives no retaliation" % unit_type)
		t.assert_eq(
			_event_types(result.events),
			["credit_spent", "attack_started", "damage_dealt", "card_destroyed", "frontline_changed"],
			"%s lethal attack follows the no-retaliation type boundary" % unit_type
		)

	var ambush_controller := _controller(466)
	var ambush_attacker := _card("lethal-ambush-attacker", "player", "Infantry", 3, 6, 0, 1)
	var ambush_defender := _card("lethal-ambush-defender", "opponent", "Infantry", 2, 3, 0, 0, ["Ambush"])
	_place_support(ambush_controller, ambush_attacker, 0)
	_place_frontline(ambush_controller, ambush_defender, 0)
	_ready(ambush_controller, ambush_attacker)
	var ambush_result = _attack(ambush_controller, ambush_attacker, ambush_defender)
	t.assert_true(ambush_result.accepted, "lethal attack against Ambush resolves")
	t.assert_eq(ambush_attacker.current_defense, 4, "Ambush deals only its first-strike damage before lethal destruction")
	t.assert_eq(ambush_defender.zone, "discard", "lethally damaged Ambush is destroyed")
	t.assert_eq(
		_event_types(ambush_result.events),
		["credit_spent", "attack_started", "damage_dealt", "damage_dealt", "card_destroyed", "frontline_changed"],
		"Ambush first strike replaces normal retaliation on lethal attack"
	)


static func _test_ambush_first_strike(t) -> void:
	var controller := _controller(470)
	var attacker := _card("ambush-attacker", "player", "Infantry", 4, 2, 0, 1)
	var ambush := _card("ambush-defender", "opponent", "Infantry", 3, 5, 0, 0, ["Ambush"])
	_place_support(controller, attacker, 0)
	_place_frontline(controller, ambush, 0)
	_ready(controller, attacker)
	var result = _attack(controller, attacker, ambush)
	t.assert_true(result.accepted, "attack into Ambush is accepted")
	t.assert_eq(attacker.zone, "discard", "Ambush destroys attacker at first-strike checkpoint")
	t.assert_eq(ambush.current_defense, 5, "destroyed attacker deals no damage after Ambush")
	t.assert_eq(_event_types(result.events), ["credit_spent", "attack_started", "damage_dealt", "card_destroyed"], "Ambush damage and destruction precede normal attack")

	for unit_type in ["Artillery", "Bomber"]:
		var ranged_controller := _controller(471)
		var ranged := _card("ambush-ranged-%s" % unit_type, "player", unit_type, 2, 4, 0, 1)
		var ranged_ambush := _card("ambush-ranged-target-%s" % unit_type, "opponent", "Infantry", 3, 5, 0, 0, ["Ambush"])
		_place_support(ranged_controller, ranged, 0)
		_place_support(ranged_controller, ranged_ambush, 0)
		_ready(ranged_controller, ranged)
		var ranged_result = _attack(ranged_controller, ranged, ranged_ambush)
		t.assert_true(ranged_result.accepted, "%s attack into Ambush resolves" % unit_type)
		t.assert_eq(ranged.current_defense, 4, "%s receives no Ambush counterattack" % unit_type)
		t.assert_eq(ranged_ambush.current_defense, 3, "%s still deals normal attack damage" % unit_type)


static func _test_headquarters_guard_and_locked_target(t) -> void:
	var guard_restricted := ["Infantry", "Tank", "Fighter"]
	var hq_guard_slots := {
		0: false,
		1: true,
		2: true,
		3: false,
	}
	for guard_slot in hq_guard_slots:
		for unit_type in guard_restricted:
			var guarded_controller := _controller(475)
			var attacker := _card("hq-guard-attacker-%s-%d" % [unit_type, guard_slot], "player", unit_type, 2, 5)
			var guard := _card("hq-guard-%s-%d" % [unit_type, guard_slot], "opponent", "Infantry", 1, 4, 0, 0, ["Guard"])
			_place_frontline(guarded_controller, attacker, 0)
			_place_support(guarded_controller, guard, guard_slot)
			_ready(guarded_controller, attacker)
			var hq_id: String = guarded_controller.state.players.opponent.headquarters.instance_id
			var before := _digest(guarded_controller)
			var result = guarded_controller.submit_action(GameAction.create(
				"attack_hq", "player", attacker.instance_id, [hq_id], {}, guarded_controller.state.sequence
			))
			if hq_guard_slots[guard_slot]:
				t.assert_eq(result.reason_code, "guard_protected", "%s Guard in adjacent HQ slot %d blocks attack" % [unit_type, guard_slot])
				t.assert_eq(_digest(guarded_controller), before, "%s blocked Headquarters attack is atomic" % unit_type)
			else:
				t.assert_true(result.accepted, "%s Guard in outer support slot %d does not block Headquarters" % [unit_type, guard_slot])

	for unit_type in ["Artillery", "Bomber"]:
		for guard_slot in [1, 2]:
			var bypass_controller := _controller(476)
			var attacker := _card("hq-bypass-attacker-%s-%d" % [unit_type, guard_slot], "player", unit_type, 2, 5)
			var guard := _card("hq-bypass-guard-%s-%d" % [unit_type, guard_slot], "opponent", "Infantry", 1, 4, 0, 0, ["Guard"])
			_place_support(bypass_controller, attacker, 0)
			_place_support(bypass_controller, guard, guard_slot)
			_ready(bypass_controller, attacker)
			var hq_id: String = bypass_controller.state.players.opponent.headquarters.instance_id
			var result = bypass_controller.submit_action(GameAction.create(
				"attack_hq", "player", attacker.instance_id, [hq_id], {}, bypass_controller.state.sequence
			))
			t.assert_true(result.accepted, "%s bypasses Guard in adjacent HQ slot %d" % [unit_type, guard_slot])
			t.assert_eq(bypass_controller.state.players.opponent.headquarters.current_defense, 18, "%s damages guarded Headquarters" % unit_type)

	var direct_controller := _controller(477)
	var direct_attacker := _card("direct-hq-attacker", "player", "Artillery", 2, 5)
	_place_support(direct_controller, direct_attacker, 0)
	_ready(direct_controller, direct_attacker)
	var direct_hq_id: String = direct_controller.state.players.opponent.headquarters.instance_id
	var direct = direct_controller.submit_action(GameAction.create(
		"attack_hq", "player", direct_attacker.instance_id, [direct_hq_id], {}, direct_controller.state.sequence
	))
	t.assert_true(direct.accepted, "Headquarters instance ID targets Headquarters directly")

	var conflicting_controller := _controller(478)
	var conflicting_attacker := _card("conflicting-hq-attacker", "player", "Artillery", 2, 5)
	_place_support(conflicting_controller, conflicting_attacker, 0)
	_ready(conflicting_controller, conflicting_attacker)
	var conflicting_before := _digest(conflicting_controller)
	var conflicting = conflicting_controller.submit_action(GameAction.create(
		"attack_hq",
		"player",
		conflicting_attacker.instance_id,
		[conflicting_controller.state.players.player.headquarters.instance_id],
		{"target_player_id": "opponent"},
		conflicting_controller.state.sequence
	))
	t.assert_eq(conflicting.reason_code, "invalid_target", "payload cannot override a locked Headquarters target")
	t.assert_eq(_digest(conflicting_controller), conflicting_before, "conflicting Headquarters target is atomic")

	var multi_controller := _controller(479)
	var multi_attacker := _card("multi-hq-attacker", "player", "Artillery", 2, 5)
	_place_support(multi_controller, multi_attacker, 0)
	_ready(multi_controller, multi_attacker)
	var multi_before := _digest(multi_controller)
	var multi = multi_controller.submit_action(GameAction.create(
		"attack_hq",
		"player",
		multi_attacker.instance_id,
		[multi_controller.state.players.opponent.headquarters.instance_id, multi_controller.state.players.player.headquarters.instance_id],
		{},
		multi_controller.state.sequence
	))
	t.assert_eq(multi.reason_code, "invalid_target", "Headquarters attack rejects multiple targets")
	t.assert_eq(_digest(multi_controller), multi_before, "multiple Headquarters targets are atomic")

	var unit_target_controller := _controller(4792)
	var unit_target_attacker := _card("unit-target-hq-attacker", "player", "Artillery", 2, 5)
	var unit_target := _card("unit-target-hq-defender", "opponent", "Infantry", 1, 4)
	_place_support(unit_target_controller, unit_target_attacker, 0)
	_place_support(unit_target_controller, unit_target, 0)
	_ready(unit_target_controller, unit_target_attacker)
	var unit_target_before := _digest(unit_target_controller)
	var unit_target_result = unit_target_controller.submit_action(GameAction.create(
		"attack_hq", "player", unit_target_attacker.instance_id, [unit_target.instance_id], {}, unit_target_controller.state.sequence
	))
	t.assert_eq(unit_target_result.reason_code, "invalid_target", "Headquarters attack rejects an existing unit instance ID")
	t.assert_eq(_digest(unit_target_controller), unit_target_before, "existing unit Headquarters target rejection is atomic")

	var unknown_target_controller := _controller(4793)
	var unknown_target_attacker := _card("unknown-target-hq-attacker", "player", "Artillery", 2, 5)
	_place_support(unknown_target_controller, unknown_target_attacker, 0)
	_ready(unknown_target_controller, unknown_target_attacker)
	var unknown_target_before := _digest(unknown_target_controller)
	var unknown_target_result = unknown_target_controller.submit_action(GameAction.create(
		"attack_hq", "player", unknown_target_attacker.instance_id, ["unknown-unit-instance-id"], {}, unknown_target_controller.state.sequence
	))
	t.assert_eq(unknown_target_result.reason_code, "invalid_target", "Headquarters attack rejects an unknown target ID")
	t.assert_eq(_digest(unknown_target_controller), unknown_target_before, "unknown Headquarters target rejection is atomic")

	var fallback_controller := _controller(4791)
	var fallback_attacker := _card("fallback-hq-attacker", "player", "Artillery", 2, 5)
	_place_support(fallback_controller, fallback_attacker, 0)
	_ready(fallback_controller, fallback_attacker)
	var fallback = fallback_controller.submit_action(GameAction.create(
		"attack_hq", "player", fallback_attacker.instance_id, [], {"target_player_id": "opponent"}, fallback_controller.state.sequence
	))
	t.assert_true(fallback.accepted, "payload target player remains a fallback without target IDs")


static func _test_headquarters_victory(t) -> void:
	var controller := _controller(480)
	var artillery := _card("hq-artillery", "player", "Artillery", 5, 3, 0, 1)
	var frontline_blocker := _card("hq-frontline-blocker", "opponent", "Infantry", 1, 3)
	_place_support(controller, artillery, 0)
	_place_frontline(controller, frontline_blocker, 0)
	_ready(controller, artillery)
	controller.state.players.opponent.headquarters.current_defense = 5
	var result = controller.submit_action(GameAction.create(
		"attack_hq",
		"player",
		artillery.instance_id,
		[controller.state.players.opponent.headquarters.instance_id],
		{},
		controller.state.sequence
	))
	t.assert_true(result.accepted, "long-range unit attacks Headquarters without Frontline")
	t.assert_eq(controller.state.players.opponent.headquarters.current_defense, 0, "Headquarters damage clamps at zero")
	t.assert_eq(controller.state.winner_id, "player", "Headquarters destruction awards victory")
	t.assert_eq(controller.state.phase, "complete", "Headquarters destruction completes match")
	t.assert_eq(_event_types(result.events), ["credit_spent", "attack_started", "damage_dealt", "match_ended"], "Headquarters victory emits terminal event last")


static func _test_rejections_are_atomic(t) -> void:
	var credit_controller := _controller(490)
	var expensive := _card("expensive", "player", "Artillery", 2, 3, 0, 11)
	var target := _card("expensive-target", "opponent")
	_place_support(credit_controller, expensive, 0)
	_place_support(credit_controller, target, 0)
	_ready(credit_controller, expensive)
	var credit_before := _digest(credit_controller)
	var no_credit = _attack(credit_controller, expensive, target)
	t.assert_eq(no_credit.reason_code, "insufficient_credit", "operation Credit shortage has stable rejection")
	t.assert_eq(_digest(credit_controller), credit_before, "Credit rejection changes no state")

	var target_controller := _controller(491)
	var infantry := _card("invalid-infantry", "player", "Infantry", 2, 3, 0, 1)
	var rear := _card("invalid-rear", "opponent")
	_place_support(target_controller, infantry, 0)
	_place_support(target_controller, rear, 0)
	_ready(target_controller, infantry)
	var target_before := _digest(target_controller)
	var invalid = _attack(target_controller, infantry, rear)
	t.assert_eq(invalid.reason_code, "invalid_target", "ground Support unit cannot target enemy Support")
	t.assert_eq(_digest(target_controller), target_before, "invalid target changes no sequence, Credit, operation, or zone")


static func _controller(seed: int) -> MatchController:
	var definitions := {
		"p-hq": {"id": "p-hq", "title": "Player HQ", "nation": "US", "category": "Headquarters", "rarity": "Elite"},
		"o-hq": {"id": "o-hq", "title": "Opponent HQ", "nation": "SU", "category": "Headquarters", "rarity": "Elite"},
	}
	var controller: MatchController = MatchController.create(definitions, ["p-hq"], ["o-hq"], seed)
	controller.state.phase = "action"
	controller.state.active_player_id = "player"
	controller.state.starting_player_id = "player"
	controller.state.turn = 5
	for player_id in controller.state.players:
		controller.state.players[player_id].credit_slots = 10
		controller.state.players[player_id].credit = 10
	return controller


static func _card(
	instance_id: String,
	owner_id: String,
	unit_type: String = "Infantry",
	attack: int = 1,
	defense: int = 3,
	deployment_cost: int = 0,
	operation_cost: int = 1,
	keywords: Array = []
) -> CardInstance:
	return CardInstance.from_definition({
		"id": instance_id,
		"title": instance_id,
		"category": "Unit",
		"unit_type": unit_type,
		"attack": attack,
		"defense": defense,
		"deployment_cost": deployment_cost,
		"operation_cost": operation_cost,
		"keywords": keywords,
	}, owner_id, instance_id)


static func _place_support(controller: MatchController, card, slot: int) -> void:
	var player = controller.state.players[card.owner_id]
	_remove_from_state(controller, card)
	player.support_line[slot] = card
	card.zone = "support_line"
	card.slot = slot


static func _place_frontline(controller: MatchController, card, slot: int) -> void:
	_remove_from_state(controller, card)
	controller.state.frontline[slot] = card
	controller.state.frontline_controller_id = card.owner_id
	card.zone = "frontline"
	card.slot = slot


static func _remove_from_state(controller: MatchController, card) -> void:
	for player_id in controller.state.players:
		var player = controller.state.players[player_id]
		player.deck.erase(card)
		player.hand.erase(card)
		player.discard.erase(card)
		for slot in range(player.support_line.size()):
			if player.support_line[slot] == card:
				player.support_line[slot] = null
	for slot in range(controller.state.frontline.size()):
		if controller.state.frontline[slot] == card:
			controller.state.frontline[slot] = null


static func _ready(controller: MatchController, card) -> void:
	card.deployed_turn = controller.state.turn - 1
	card.operations_used = 0


static func _attack(controller: MatchController, attacker, defender):
	return controller.submit_action(GameAction.create(
		"attack_unit", attacker.owner_id, attacker.instance_id, [defender.instance_id], {}, controller.state.sequence
	))


static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(event.type)
	return types


static func _digest(controller: MatchController) -> Dictionary:
	var players := {}
	for player_id in controller.state.players:
		var player = controller.state.players[player_id]
		players[player_id] = {
			"credit": player.credit,
			"hq": player.headquarters.current_defense,
			"hand": _card_states(player.hand),
			"support": _slot_states(player.support_line),
			"discard": _card_states(player.discard),
		}
	return {
		"sequence": controller.state.sequence,
		"phase": controller.state.phase,
		"winner_id": controller.state.winner_id,
		"frontline_controller_id": controller.state.frontline_controller_id,
		"frontline": _slot_states(controller.state.frontline),
		"players": players,
	}


static func _card_states(cards: Array) -> Array:
	var states: Array = []
	for card in cards:
		states.append(_card_state(card))
	return states


static func _slot_states(cards: Array) -> Array:
	var states: Array = []
	for card in cards:
		states.append(null if card == null else _card_state(card))
	return states


static func _card_state(card) -> Dictionary:
	return {
		"id": card.instance_id,
		"zone": card.zone,
		"slot": card.slot,
		"attack": card.current_attack,
		"defense": card.current_defense,
		"operations_used": card.operations_used,
		"operation_chain": card.operation_chain,
		"smokescreen_revealed": card.smokescreen_revealed,
		"deployed_turn": card.deployed_turn,
	}
