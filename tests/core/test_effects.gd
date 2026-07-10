extends RefCounted

const CardInstance = preload("res://scripts/core/card_instance.gd")
const EffectEngine = preload("res://scripts/core/effect_engine.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")


static func run(t) -> void:
	_test_stat_and_status_effects(t)
	_test_zone_and_resource_effects(t)
	_test_destroy_runs_death_trigger(t)
	_test_random_target_is_seeded(t)
	_test_deploy_trigger_and_event_replacement(t)
	_test_effect_validation_is_atomic(t)
	_test_effect_limit_terminates(t)
	_test_orders_and_countermeasures(t)
	_test_countermeasure_refund_and_expiry(t)
	_test_order_targets_and_manual_ability(t)
	_test_retreat_order_rejects_atomically(t)
	_test_effect_lethal_lifecycle(t)
	_test_action_target_validation_is_atomic(t)
	_test_effect_limit_order_rejects_atomically(t)
	_test_multi_retreat_reserves_capacity(t)
	_test_effect_frontline_removals_emit_change(t)
	_test_countermeasure_trigger_matching_and_order(t)
	_test_manual_source_zone_and_random_preflight(t)


static func _test_stat_and_status_effects(t) -> void:
	var controller := _controller(501)
	var source := _card("source", "player")
	var target := _card("target", "opponent", "Unit", 3, 6)
	_place_support(controller, source, 0)
	_place_support(controller, target, 0)
	var engine := _engine(controller, 501)

	_resolve(engine, source, [target.instance_id], [{"type": "damage", "amount": 2}])
	t.assert_true(engine.last_resolution.valid, "damage effect resolves")
	t.assert_eq(target.current_defense, 4, "damage lowers defense")

	_resolve(engine, source, [target.instance_id], [{"type": "repair", "amount": 1}])
	t.assert_eq(target.current_defense, 5, "repair restores defense without exceeding base defense")

	_resolve(engine, source, [target.instance_id], [{"type": "buff", "attack": 2, "defense": 1, "duration": "turn"}])
	t.assert_eq([target.current_attack, target.current_defense], [5, 6], "buff changes both combat stats")
	t.assert_eq(target.modifiers.size(), 1, "temporary buff records a modifier")

	_resolve(engine, source, [target.instance_id], [{"type": "debuff", "attack": 1, "defense": 2}])
	t.assert_eq([target.current_attack, target.current_defense], [4, 4], "debuff subtracts both combat stats")

	_resolve(engine, source, [target.instance_id], [{"type": "status", "status": "Pinned", "active": true}])
	t.assert_true(target.has_keyword_or_status("Pinned"), "status effect adds a named status")
	_resolve(engine, source, [target.instance_id], [{"type": "status", "status": "Pinned", "active": false}])
	t.assert_true(not target.has_keyword_or_status("Pinned"), "status effect removes a named status")


static func _test_zone_and_resource_effects(t) -> void:
	var definitions := {
		"created-unit": _definition("created-unit", "Unit", 2, 3),
	}
	var controller := _controller(502, definitions)
	var source := _card("source", "player")
	var draw_card := _card("draw-card", "player")
	var discard_card := _card("discard-card", "player")
	var copy_target := _card("copy-target", "opponent", "Unit", 4, 5)
	var return_target := _card("return-target", "opponent")
	var retreat_target := _card("retreat-target", "opponent")
	_place_support(controller, source, 0)
	_place_support(controller, copy_target, 0)
	_place_support(controller, return_target, 1)
	_place_frontline(controller, retreat_target, 0)
	_put_deck(controller, draw_card)
	_put_hand(controller, discard_card)
	var engine := _engine(controller, 502, definitions)

	_resolve(engine, source, [], [{"type": "draw", "count": 1, "player": "owner"}], {"selector": "none"})
	t.assert_true(controller.state.players.player.hand.has(draw_card), "draw moves the top card into hand")

	_resolve(engine, source, [discard_card.instance_id], [{"type": "discard"}])
	t.assert_true(controller.state.players.player.discard.has(discard_card), "discard moves the selected hand card")

	_resolve(engine, source, [], [{"type": "create", "definition_id": "created-unit", "destination": "hand", "player": "owner"}], {"selector": "none"})
	var created = controller.state.players.player.hand.back()
	t.assert_eq(created.definition_id, "created-unit", "create instantiates the requested definition")
	t.assert_eq(created.zone, "hand", "created card enters the requested zone")

	_resolve(engine, source, [copy_target.instance_id], [{"type": "copy", "destination": "hand", "player": "owner"}])
	var copied = controller.state.players.player.hand.back()
	t.assert_eq(copied.definition_id, copy_target.definition_id, "copy preserves the definition")
	t.assert_true(copied.instance_id != copy_target.instance_id, "copy receives a unique instance ID")

	_resolve(engine, source, [return_target.instance_id], [{"type": "return"}])
	t.assert_true(controller.state.players.opponent.hand.has(return_target), "return moves a unit to its owner's hand")

	_resolve(engine, source, [retreat_target.instance_id], [{"type": "retreat"}])
	t.assert_eq(retreat_target.zone, "support_line", "Retreat leaves the Frontline")
	t.assert_eq(retreat_target.slot, 1, "Retreat chooses the first available owner Support slot")

	var player = controller.state.players.player
	player.credit = 3
	player.credit_slots = 4
	_resolve(engine, source, [], [
		{"type": "credit", "amount": 2, "player": "owner"},
		{"type": "credit_slots", "amount": 2, "player": "owner"},
	], {"selector": "none"})
	t.assert_eq(player.credit, 5, "Credit effect changes available Credit")
	t.assert_eq(player.credit_slots, 6, "Credit slots effect changes slot count")
	_resolve(engine, source, [], [{"type": "credit", "amount": -2, "player": "owner"}], {"selector": "none"})
	t.assert_eq(player.credit, 3, "Credit effect can spend available Credit")


static func _test_destroy_runs_death_trigger(t) -> void:
	var controller := _controller(503)
	var source := _card("destroy-source", "player")
	var doomed := _card("doomed", "opponent")
	doomed.abilities = [_ability(
		"death-shot",
		"death",
		{"selector": "enemy_hq"},
		[{"type": "damage", "amount": 2}]
	)]
	_place_support(controller, source, 0)
	_place_support(controller, doomed, 0)
	var engine := _engine(controller, 503)

	var events := _resolve(engine, source, [doomed.instance_id], [{"type": "destroy"}])
	t.assert_true(engine.last_resolution.valid, "destroy and death queue resolve")
	t.assert_eq(doomed.zone, "discard", "destroy moves a unit to discard")
	t.assert_eq(controller.state.players.player.headquarters.current_defense, 18, "death trigger resolves after destruction")
	t.assert_true(_event_types(events).has("card_destroyed"), "destroy emits a semantic event")


static func _test_random_target_is_seeded(t) -> void:
	var first := _random_target_fixture(504)
	var second := _random_target_fixture(504)
	var first_events: Array = first.engine.resolve_trigger("manual", first.context)
	var second_events: Array = second.engine.resolve_trigger("manual", second.context)
	t.assert_eq(_unit_defenses(first.controller, "opponent"), _unit_defenses(second.controller, "opponent"), "same seed selects the same random target")
	t.assert_eq(first_events, second_events, "seeded random target emits identical events")
	var defenses := _unit_defenses(first.controller, "opponent")
	t.assert_true(defenses.values().has(3) and defenses.values().has(5), "exactly one random enemy receives damage")


static func _test_deploy_trigger_and_event_replacement(t) -> void:
	var controller := _controller(505)
	var deployer := _card("deployer", "player")
	deployer.abilities = [_ability(
		"deploy-repair",
		"deploy",
		{"selector": "friendly_hq"},
		[{"type": "repair", "amount": 2}]
	)]
	_place_support(controller, deployer, 0)
	controller.state.players.player.headquarters.current_defense = 17
	var engine := _engine(controller, 505)
	engine.resolve_trigger("deploy", {"source_id": deployer.instance_id, "actor_id": "player", "target_ids": []})
	t.assert_eq(controller.state.players.player.headquarters.current_defense, 19, "deploy trigger resolves from the deployed card")

	var counter := _card("counter", "player", "Countermeasure")
	counter.abilities = [_ability(
		"cancel-order",
		"order_played",
		{"selector": "none"},
		[{"type": "replace_event", "changes": {"cancelled": true, "replacement": "cancel"}}]
	)]
	_put_hand(controller, counter)
	var order_event := {"type": "order_played", "cancelled": false}
	var replacement_events := engine.resolve_trigger("order_played", {
		"source_id": counter.instance_id,
		"actor_id": "player",
		"target_ids": [],
		"event": order_event,
	})
	t.assert_true(order_event.cancelled, "event replacement mutates the pending event")
	t.assert_eq(order_event.replacement, "cancel", "event replacement records replacement data")
	t.assert_true(_event_types(replacement_events).has("event_replaced"), "replacement emits a semantic event")


static func _test_effect_validation_is_atomic(t) -> void:
	var controller := _controller(506)
	var source := _card("atomic-source", "player")
	var retreat_target := _card("atomic-retreat", "opponent")
	_place_support(controller, source, 0)
	_place_frontline(controller, retreat_target, 0)
	for slot in range(GameConstants.SUPPORT_UNIT_SLOTS):
		_place_support(controller, _card("blocker-%d" % slot, "opponent"), slot)
	var player = controller.state.players.player
	player.credit = 4
	var engine := _engine(controller, 506)

	_resolve(engine, source, [retreat_target.instance_id], [
		{"type": "credit", "amount": 2, "player": "owner"},
		{"type": "retreat"},
	])
	t.assert_true(not engine.last_resolution.valid, "invalid effect list is rejected")
	t.assert_eq(engine.last_resolution.code, "support_line_full", "Retreat failure has a stable code")
	t.assert_eq(player.credit, 4, "earlier effects do not partially mutate state")
	t.assert_eq(retreat_target.zone, "frontline", "failed Retreat leaves the unit in place")


static func _test_effect_limit_terminates(t) -> void:
	var controller := _controller(507)
	var source := _card("loop-source", "player")
	_place_support(controller, source, 0)
	var effects: Array = []
	for index in range(GameConstants.MAX_EFFECT_EVENTS + 1):
		effects.append({"type": "credit", "amount": 0, "player": "owner"})
	var engine := _engine(controller, 507)
	var events := _resolve(engine, source, [], effects, {"selector": "none"})
	t.assert_true(not engine.last_resolution.valid, "effect overflow is rejected")
	t.assert_eq(engine.last_resolution.code, "effect_limit", "effect overflow has stable code")
	t.assert_eq(events.size(), GameConstants.MAX_EFFECT_EVENTS, "effect queue stops at the event limit")
	t.assert_eq(controller.state.phase, "invalid", "effect overflow marks the match invalid")


static func _test_orders_and_countermeasures(t) -> void:
	var controller := _controller(512)
	var protected := _card("p-unit", "player", "Unit", 2, 4)
	var counter := _countermeasure("p-signal-watch", "player", 2)
	var order := _damage_order("o-bombardment", "opponent", 3, 3)
	_place_support(controller, protected, 0)
	_put_hand(controller, counter)
	_put_hand(controller, order)

	var activated = controller.submit_action(GameAction.create(
		"toggle_countermeasure", "player", counter.instance_id, [], {}, controller.state.sequence
	))
	t.assert_true(activated.accepted, "Countermeasure activates in hand")
	t.assert_true(counter.countermeasure_active, "activated Countermeasure remains active in hand")
	t.assert_true(controller.state.players.player.hand.has(counter), "activated Countermeasure remains in hand")
	var opponent_view: Dictionary = controller.state.snapshot_for("opponent")
	var hidden_counter: Dictionary = opponent_view.players.player.hand[0]
	t.assert_true(hidden_counter.hidden, "opponent snapshot hides active Countermeasure identity")
	t.assert_true(not hidden_counter.has("countermeasure_active"), "opponent snapshot hides activation state")

	controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
	var before_order_credit: int = controller.state.players.opponent.credit
	var result = controller.submit_action(GameAction.create(
		"play_order", "opponent", order.instance_id, [protected.instance_id], {}, controller.state.sequence
	))
	t.assert_true(result.accepted, "enemy Order action is accepted")
	t.assert_eq(controller.state.players.opponent.credit, before_order_credit - order.deployment_cost, "cancelled Order still pays Credit")
	t.assert_true(controller.state.players.player.discard.has(counter), "triggered Countermeasure is discarded")
	t.assert_true(controller.state.players.opponent.discard.has(order), "cancelled Order is discarded")
	t.assert_eq(protected.current_defense, 4, "replacement cancels Order damage")
	t.assert_true(_event_types(result.events).has("countermeasure_triggered"), "matching Countermeasure trigger is revealed")


static func _test_countermeasure_refund_and_expiry(t) -> void:
	var refund_controller := _controller(513)
	var counter := _countermeasure("refund-counter", "player", 2)
	_put_hand(refund_controller, counter)
	var before_credit: int = refund_controller.state.players.player.credit
	var activated = refund_controller.submit_action(GameAction.create(
		"toggle_countermeasure", "player", counter.instance_id, [], {}, refund_controller.state.sequence
	))
	counter.deployment_cost = 5
	var deactivated = refund_controller.submit_action(GameAction.create(
		"toggle_countermeasure", "player", counter.instance_id, [], {}, refund_controller.state.sequence
	))
	t.assert_true(activated.accepted and deactivated.accepted, "owner can activate and deactivate Countermeasure")
	t.assert_eq(refund_controller.state.players.player.credit, before_credit, "deactivation refunds exactly the activation payment")
	t.assert_true(not counter.countermeasure_active, "deactivated Countermeasure is inactive")

	var expiry_controller := _controller(514)
	var expiring := _countermeasure("expiring-counter", "player", 2)
	_put_hand(expiry_controller, expiring)
	expiry_controller.submit_action(GameAction.create(
		"toggle_countermeasure", "player", expiring.instance_id, [], {}, expiry_controller.state.sequence
	))
	expiry_controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, expiry_controller.state.sequence))
	t.assert_true(expiring.countermeasure_active, "Countermeasure stays active through the enemy turn")
	expiry_controller.submit_action(GameAction.create("end_turn", "opponent", "", [], {}, expiry_controller.state.sequence))
	t.assert_true(not expiring.countermeasure_active, "unused Countermeasure expires after the enemy turn")
	t.assert_true(expiry_controller.state.players.player.hand.has(expiring), "unused Countermeasure remains in hand")


static func _test_order_targets_and_manual_ability(t) -> void:
	var controller := _controller(515)
	var smokescreen := _card("smokescreen-target", "opponent", "Unit", 2, 5)
	smokescreen.statuses["Smokescreen"] = true
	var order := _damage_order("direct-order", "player", 2, 2)
	var hq_order := _damage_order("hq-order", "player", 1, 2)
	var ability_unit := _card("ability-unit", "player")
	ability_unit.abilities = [_ability(
		"paid-shot",
		"manual",
		{"selector": "enemy_unit", "count": 1},
		[{"type": "damage", "amount": 1}],
		{},
		2
	)]
	_place_support(controller, smokescreen, 0)
	_place_support(controller, ability_unit, 0)
	_put_hand(controller, order)
	_put_hand(controller, hq_order)

	var direct = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [smokescreen.instance_id], {}, controller.state.sequence
	))
	t.assert_true(direct.accepted, "Order effect can target through Smokescreen")
	t.assert_eq(smokescreen.current_defense, 3, "Order applies damage through Smokescreen")
	t.assert_true(controller.state.players.player.discard.has(order), "resolved Order is discarded")

	var enemy_hq = controller.state.players.opponent.headquarters
	var hq_result = controller.submit_action(GameAction.create(
		"play_order", "player", hq_order.instance_id, [enemy_hq.instance_id], {}, controller.state.sequence
	))
	t.assert_true(hq_result.accepted, "Order can target a Headquarters")
	t.assert_eq(enemy_hq.current_defense, 19, "Order damages a Headquarters")

	var before_ability_credit: int = controller.state.players.player.credit
	var ability = controller.submit_action(GameAction.create(
		"activate_ability",
		"player",
		ability_unit.instance_id,
		[smokescreen.instance_id],
		{"ability_id": "paid-shot"},
		controller.state.sequence
	))
	t.assert_true(ability.accepted, "manual ability is accepted")
	t.assert_eq(controller.state.players.player.credit, before_ability_credit - 2, "manual ability pays listed Credit")
	t.assert_eq(smokescreen.current_defense, 2, "manual ability resolves its effect")


static func _test_retreat_order_rejects_atomically(t) -> void:
	var controller := _controller(516)
	var retreat_order := _card("retreat-order", "player", "Order", 0, 0, 3)
	retreat_order.abilities = [_ability(
		"forced-retreat",
		"play_order",
		{"selector": "enemy_unit", "count": 1},
		[{"type": "retreat"}]
	)]
	var target := _card("frontline-target", "opponent")
	_put_hand(controller, retreat_order)
	_place_frontline(controller, target, 0)
	for slot in range(GameConstants.SUPPORT_UNIT_SLOTS):
		_place_support(controller, _card("retreat-blocker-%d" % slot, "opponent"), slot)
	var before := _controller_digest(controller)

	var result = controller.submit_action(GameAction.create(
		"play_order", "player", retreat_order.instance_id, [target.instance_id], {}, controller.state.sequence
	))
	t.assert_true(not result.accepted, "Retreat rejects when the owner Support Line is full")
	t.assert_eq(result.reason_code, "support_line_full", "failed Retreat has a stable reason")
	t.assert_eq(_controller_digest(controller), before, "failed Retreat Order changes no state")


static func _test_effect_lethal_lifecycle(t) -> void:
	var controller := _controller(517)
	var order := _damage_order("lethal-order", "player", 4, 2)
	var victim := _card("lethal-effect-victim", "opponent", "Unit", 1, 3)
	victim.abilities = [_ability("death-ping", "death", {"selector": "enemy_hq"}, [{"type": "damage", "amount": 2}])]
	_put_hand(controller, order)
	_place_frontline(controller, victim, 0)
	var result = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [victim.instance_id], {}, controller.state.sequence
	))
	t.assert_true(result.accepted, "lethal Order resolves")
	t.assert_eq(victim.zone, "discard", "lethal effect destroys the unit")
	t.assert_eq(controller.state.players.player.headquarters.current_defense, 18, "lethal effect resolves death trigger")
	t.assert_eq(controller.state.frontline_controller_id, "", "lethal Frontline removal clears control")
	t.assert_true(_event_types(result.events).has("frontline_changed"), "lethal effect emits Frontline change")

	var hq_order := _damage_order("hq-lethal-order", "player", 20, 2)
	_put_hand(controller, hq_order)
	var hq_result = controller.submit_action(GameAction.create(
		"play_order", "player", hq_order.instance_id, [controller.state.players.opponent.headquarters.instance_id], {}, controller.state.sequence
	))
	t.assert_true(hq_result.accepted, "lethal Headquarters Order resolves")
	t.assert_eq(controller.state.players.opponent.headquarters.current_defense, 0, "Headquarters damage clamps at zero")
	t.assert_eq(controller.state.winner_id, "player", "lethal Headquarters effect chooses a winner")
	t.assert_eq(controller.state.phase, "complete", "lethal Headquarters effect ends the match")


static func _test_action_target_validation_is_atomic(t) -> void:
	var controller := _controller(518)
	var order := _damage_order("targeted-order", "player", 2, 2)
	var visible := _card("visible-enemy", "opponent")
	var hidden := _card("hidden-enemy", "opponent")
	_put_hand(controller, order)
	_place_support(controller, visible, 0)
	_put_hand(controller, hidden)
	var before := _controller_digest(controller)
	var duplicate = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [visible.instance_id, visible.instance_id], {}, controller.state.sequence
	))
	t.assert_eq(duplicate.reason_code, "invalid_target", "duplicate effect targets are rejected")
	t.assert_eq(_controller_digest(controller), before, "duplicate targets are atomic")
	var hidden_target = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [hidden.instance_id], {}, controller.state.sequence
	))
	t.assert_eq(hidden_target.reason_code, "invalid_target", "hidden hand target cannot be probed")
	t.assert_eq(_controller_digest(controller), before, "hidden target rejection is atomic")


static func _test_effect_limit_order_rejects_atomically(t) -> void:
	var controller := _controller(519)
	var order := _card("overflow-order", "player", "Order", 0, 0, 3)
	var effects: Array = []
	for index in range(GameConstants.MAX_EFFECT_EVENTS + 1):
		effects.append({"type": "credit", "amount": 0, "player": "owner"})
	order.abilities = [_ability("overflow", "play_order", {"selector": "none"}, effects)]
	_put_hand(controller, order)
	var before := _controller_digest(controller)
	var result = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [], {}, controller.state.sequence
	))
	t.assert_true(not result.accepted, "effect-limit Order is rejected")
	t.assert_eq(result.reason_code, "effect_limit", "effect limit has a stable action rejection")
	t.assert_eq(_controller_digest(controller), before, "effect-limit Order leaves no partial payment or discard")

	var deploy_controller := _controller(527)
	var unit := _card("overflow-deploy-unit", "player")
	unit.abilities = [_ability("overflow-deploy", "deploy", {"selector": "none"}, effects)]
	_put_hand(deploy_controller, unit)
	var deploy_before := _controller_digest(deploy_controller)
	var deploy_result = deploy_controller.submit_action(GameAction.create(
		"deploy_unit", "player", unit.instance_id, [], {"support_slot": 0}, deploy_controller.state.sequence
	))
	t.assert_eq(deploy_result.reason_code, "effect_limit", "effect-limit deploy trigger is rejected before deployment")
	t.assert_eq(_controller_digest(deploy_controller), deploy_before, "effect-limit deploy leaves no partial payment or zone mutation")


static func _test_multi_retreat_reserves_capacity(t) -> void:
	var controller := _controller(520)
	var order := _card("multi-retreat-order", "player", "Order", 0, 0, 3)
	order.abilities = [_ability(
		"multi-retreat", "play_order", {"selector": "enemy_unit", "count": 2}, [{"type": "retreat"}]
	)]
	var first := _card("retreat-first", "opponent")
	var second := _card("retreat-second", "opponent")
	_put_hand(controller, order)
	_place_frontline(controller, first, 0)
	_place_frontline(controller, second, 1)
	for slot in range(1, GameConstants.SUPPORT_UNIT_SLOTS):
		_place_support(controller, _card("retreat-fill-%d" % slot, "opponent"), slot)
	var before := _controller_digest(controller)
	var result = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [first.instance_id, second.instance_id], {}, controller.state.sequence
	))
	t.assert_eq(result.reason_code, "support_line_full", "multi-Retreat reserves owner capacity before mutation")
	t.assert_eq(_controller_digest(controller), before, "multi-Retreat rejection leaves both targets in the Frontline")


static func _test_effect_frontline_removals_emit_change(t) -> void:
	var controller := _controller(521)
	var order := _card("return-order", "player", "Order", 0, 0, 2)
	order.abilities = [_ability("return-frontline", "play_order", {"selector": "enemy_unit", "count": 1}, [{"type": "return"}])]
	var target := _card("return-frontline-target", "opponent")
	_put_hand(controller, order)
	_place_frontline(controller, target, 0)
	var result = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [target.instance_id], {}, controller.state.sequence
	))
	t.assert_true(result.accepted, "return Order resolves")
	t.assert_eq(target.zone, "hand", "return removes the Frontline unit")
	t.assert_eq(controller.state.frontline_controller_id, "", "return recalculates Frontline control")
	t.assert_true(_event_types(result.events).has("frontline_changed"), "return emits Frontline control change")


static func _test_countermeasure_trigger_matching_and_order(t) -> void:
	var controller := _controller(522)
	var first := _countermeasure("first-counter", "player", 1)
	var second := _countermeasure("second-counter", "player", 1)
	var attacker := _card("counter-attack-source", "opponent", "Unit", 2, 4)
	attacker.unit_type = "Artillery"
	var defender := _card("counter-attack-target", "player", "Unit", 1, 4)
	defender.unit_type = "Infantry"
	first.abilities = [_ability("first-attack", "attack", {"selector": "none"}, [{"type": "replace_event", "changes": {"first": true}}], {"enemy": true})]
	second.abilities = [_ability("second-attack", "attack", {"selector": "none"}, [{"type": "replace_event", "changes": {"second": true}}], {"enemy": true})]
	_put_hand(controller, first)
	_put_hand(controller, second)
	_place_support(controller, attacker, 0)
	_place_frontline(controller, defender, 0)
	controller.submit_action(GameAction.create("toggle_countermeasure", "player", first.instance_id, [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("toggle_countermeasure", "player", second.instance_id, [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
	var attack = controller.submit_action(GameAction.create(
		"attack_unit", "opponent", attacker.instance_id, [defender.instance_id], {}, controller.state.sequence
	))
	t.assert_true(attack.accepted, "enemy attack action resolves through Countermeasure checks: %s" % attack.reason_code)
	t.assert_true(controller.state.players.player.discard.has(first) and controller.state.players.player.discard.has(second), "attack-triggered Countermeasures reveal after reacting")
	var reaction_types := _event_types(attack.events).filter(func(event_type): return event_type in ["event_replaced", "countermeasure_triggered"])
	t.assert_eq(reaction_types, ["event_replaced", "countermeasure_triggered", "event_replaced", "countermeasure_triggered"], "Countermeasures resolve and reveal in declaration order: %s" % [reaction_types])

	var untargeted := _countermeasure("target-owner-counter", "player", 1)
	var untargeted_order := _card("untargeted-order", "opponent", "Order", 0, 0, 1)
	untargeted.abilities = [_ability("target-owner-only", "order_played", {"selector": "none"}, [{"type": "replace_event", "changes": {"cancelled": true}}], {"enemy": true, "target_owner": "owner"})]
	untargeted_order.abilities = [_ability("untargeted", "play_order", {"selector": "none"}, [{"type": "credit", "amount": 0, "player": "owner"}])]
	_put_hand(controller, untargeted)
	_put_hand(controller, untargeted_order)
	controller.submit_action(GameAction.create("end_turn", "opponent", "", [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("toggle_countermeasure", "player", untargeted.instance_id, [], {}, controller.state.sequence))
	controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
	var order_result = controller.submit_action(GameAction.create(
		"play_order", "opponent", untargeted_order.instance_id, [], {}, controller.state.sequence
	))
	t.assert_true(order_result.accepted, "untargeted Order resolves")
	t.assert_true(untargeted.countermeasure_active, "target-specific Countermeasure ignores untargeted Order")
	t.assert_true(controller.state.players.player.hand.has(untargeted), "unmatched Countermeasure remains hidden in hand")

	var hq_controller := _controller(525)
	var hq_counter := _countermeasure("hq-lethal-counter", "player", 1)
	hq_counter.abilities = [_ability("hq-lethal-reaction", "hq_lethal", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}], {"enemy": true})]
	var hq_order := _damage_order("hq-lethal-enemy-order", "opponent", 20, 1)
	_put_hand(hq_controller, hq_counter)
	_put_hand(hq_controller, hq_order)
	hq_controller.submit_action(GameAction.create("toggle_countermeasure", "player", hq_counter.instance_id, [], {}, hq_controller.state.sequence))
	hq_controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, hq_controller.state.sequence))
	hq_controller.submit_action(GameAction.create("play_order", "opponent", hq_order.instance_id, [hq_controller.state.players.player.headquarters.instance_id], {}, hq_controller.state.sequence))
	t.assert_true(hq_controller.state.players.player.discard.has(hq_counter), "HQ-lethal Countermeasure trigger resolves against enemy effect")

	var frontline_controller := _controller(526)
	var frontline_counter := _countermeasure("frontline-loss-counter", "player", 1)
	frontline_counter.abilities = [_ability("frontline-loss-reaction", "frontline_lost", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}], {"enemy": true})]
	var return_order := _card("frontline-loss-order", "opponent", "Order", 0, 0, 1)
	return_order.abilities = [_ability("enemy-return", "play_order", {"selector": "enemy_unit", "count": 1}, [{"type": "return"}])]
	var lost_unit := _card("lost-frontline-unit", "player")
	_put_hand(frontline_controller, frontline_counter)
	_put_hand(frontline_controller, return_order)
	_place_frontline(frontline_controller, lost_unit, 0)
	frontline_controller.submit_action(GameAction.create("toggle_countermeasure", "player", frontline_counter.instance_id, [], {}, frontline_controller.state.sequence))
	frontline_controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, frontline_controller.state.sequence))
	frontline_controller.submit_action(GameAction.create("play_order", "opponent", return_order.instance_id, [lost_unit.instance_id], {}, frontline_controller.state.sequence))
	t.assert_true(frontline_controller.state.players.player.discard.has(frontline_counter), "Frontline-loss Countermeasure trigger resolves against enemy effect")


static func _test_manual_source_zone_and_random_preflight(t) -> void:
	var controller := _controller(523)
	var hidden_source := _card("hidden-manual", "player")
	hidden_source.abilities = [_ability("random-manual", "manual", {"selector": "random_enemy_unit", "count": 1}, [{"type": "damage", "amount": 1}])]
	var target := _card("random-public-target", "opponent")
	_put_hand(controller, hidden_source)
	_place_support(controller, target, 0)
	var before := _controller_digest(controller)
	var rejected = controller.submit_action(GameAction.create(
		"activate_ability", "player", hidden_source.instance_id, [], {"ability_id": "random-manual"}, controller.state.sequence
	))
	t.assert_eq(rejected.reason_code, "invalid_origin", "manual abilities reject hidden source zones")
	t.assert_eq(_controller_digest(controller), before, "hidden manual source rejection is atomic")

	var first := _random_action_controller(524)
	var invalid_random = first.controller.submit_action(GameAction.create(
		"activate_ability", "player", first.source.instance_id, [first.targets[0].instance_id], {"ability_id": "random-manual"}, first.controller.state.sequence
	))
	t.assert_eq(invalid_random.reason_code, "invalid_target", "random selector rejects caller-supplied targets")
	var first_result = first.controller.submit_action(GameAction.create(
		"activate_ability", "player", first.source.instance_id, [], {"ability_id": "random-manual"}, first.controller.state.sequence
	))
	var second := _random_action_controller(524)
	var second_result = second.controller.submit_action(GameAction.create(
		"activate_ability", "player", second.source.instance_id, [], {"ability_id": "random-manual"}, second.controller.state.sequence
	))
	t.assert_true(first_result.accepted and second_result.accepted, "random manual ability resolves after valid preflight: %s/%s" % [first_result.reason_code, second_result.reason_code])
	t.assert_eq(_unit_defenses(first.controller, "opponent"), _unit_defenses(second.controller, "opponent"), "rejected random action leaves the seeded outcome unchanged")


static func _resolve(
	engine,
	source,
	target_ids: Array,
	effects: Array,
	target: Dictionary = {"selector": "action_targets", "count": 1},
	extra_context: Dictionary = {}
) -> Array:
	source.abilities = [_ability("test-ability", "manual", target, effects)]
	var context := {
		"source_id": source.instance_id,
		"actor_id": source.owner_id,
		"target_ids": target_ids.duplicate(),
	}
	for key in extra_context:
		context[key] = extra_context[key]
	return engine.resolve_trigger("manual", context)


static func _ability(
	id: String,
	trigger: String,
	target: Dictionary,
	effects: Array,
	conditions: Dictionary = {},
	credit_cost: int = 0
) -> Dictionary:
	return {
		"id": id,
		"trigger": trigger,
		"conditions": conditions.duplicate(true),
		"target": target.duplicate(true),
		"effects": effects.duplicate(true),
		"credit_cost": credit_cost,
	}


static func _countermeasure(instance_id: String, owner_id: String, cost: int) -> CardInstance:
	var card := _card(instance_id, owner_id, "Countermeasure", 0, 0, cost)
	card.abilities = [_ability(
		"%s--cancel" % instance_id,
		"order_played",
		{"selector": "none"},
		[{"type": "replace_event", "changes": {"cancelled": true}}],
		{"enemy": true, "target_owner": "owner"}
	)]
	return card


static func _damage_order(instance_id: String, owner_id: String, damage: int, cost: int) -> CardInstance:
	var card := _card(instance_id, owner_id, "Order", 0, 0, cost)
	card.abilities = [_ability(
		"%s--damage" % instance_id,
		"play_order",
		{"selector": "enemy_unit_or_hq", "count": 1},
		[{"type": "damage", "amount": damage}]
	)]
	return card


static func _random_target_fixture(seed: int) -> Dictionary:
	var controller := _controller(seed)
	var source := _card("random-source", "player")
	var first := _card("random-a", "opponent", "Unit", 1, 5)
	var second := _card("random-b", "opponent", "Unit", 1, 5)
	_place_support(controller, source, 0)
	_place_support(controller, first, 0)
	_place_support(controller, second, 1)
	source.abilities = [_ability(
		"random-hit",
		"manual",
		{"selector": "random_enemy_unit", "count": 1},
		[{"type": "damage", "amount": 2}]
	)]
	return {
		"controller": controller,
		"engine": _engine(controller, seed),
		"context": {"source_id": source.instance_id, "actor_id": "player", "target_ids": []},
	}


static func _random_action_controller(seed: int) -> Dictionary:
	var controller := _controller(seed)
	var source := _card("random-action-source", "player")
	var first := _card("random-action-a", "opponent")
	var second := _card("random-action-b", "opponent")
	source.abilities = [_ability("random-manual", "manual", {"selector": "random_enemy_unit", "count": 1}, [{"type": "damage", "amount": 1}])]
	_place_support(controller, source, 0)
	_place_support(controller, first, 0)
	_place_support(controller, second, 1)
	return {"controller": controller, "source": source, "targets": [first, second]}


static func _controller(seed: int, extra_definitions: Dictionary = {}) -> MatchController:
	var definitions := {
		"p-hq": _definition("p-hq", "Headquarters", 0, GameConstants.HQ_DEFENSE),
		"o-hq": _definition("o-hq", "Headquarters", 0, GameConstants.HQ_DEFENSE),
	}
	for definition_id in extra_definitions:
		definitions[definition_id] = extra_definitions[definition_id]
	var controller: MatchController = MatchController.create(definitions, ["p-hq"], ["o-hq"], seed)
	controller.state.phase = "action"
	controller.state.active_player_id = "player"
	controller.state.starting_player_id = "player"
	controller.state.turn = 5
	for player_id in controller.state.players:
		controller.state.players[player_id].credit_slots = 10
		controller.state.players[player_id].credit = 10
	return controller


static func _engine(controller: MatchController, seed: int, definitions: Dictionary = {}) -> EffectEngine:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed
	return EffectEngine.create(controller.state, definitions, rng)


static func _definition(id: String, category: String, attack: int, defense: int) -> Dictionary:
	return {
		"id": id,
		"title": id,
		"nation": "Test",
		"category": category,
		"rarity": "Standard",
		"unit_type": "Infantry",
		"attack": attack,
		"defense": defense,
		"deployment_cost": 0,
		"operation_cost": 0,
		"keywords": [],
		"abilities": [],
	}


static func _card(
	instance_id: String,
	owner_id: String,
	category: String = "Unit",
	attack: int = 1,
	defense: int = 5,
	deployment_cost: int = 0
) -> CardInstance:
	var definition := _definition(instance_id, category, attack, defense)
	definition.deployment_cost = deployment_cost
	return CardInstance.from_definition(definition, owner_id, instance_id)


static func _place_support(controller: MatchController, card, slot: int) -> void:
	_remove_card(controller, card)
	controller.state.players[card.owner_id].support_line[slot] = card
	card.zone = "support_line"
	card.slot = slot


static func _place_frontline(controller: MatchController, card, slot: int) -> void:
	_remove_card(controller, card)
	controller.state.frontline[slot] = card
	controller.state.frontline_controller_id = card.owner_id
	card.zone = "frontline"
	card.slot = slot


static func _put_hand(controller: MatchController, card) -> void:
	_remove_card(controller, card)
	controller.state.players[card.owner_id].hand.append(card)
	card.zone = "hand"
	card.slot = -1


static func _put_deck(controller: MatchController, card) -> void:
	_remove_card(controller, card)
	controller.state.players[card.owner_id].deck.append(card)
	card.zone = "deck"
	card.slot = -1


static func _remove_card(controller: MatchController, card) -> void:
	for player_id in controller.state.players:
		var player = controller.state.players[player_id]
		player.deck.erase(card)
		player.hand.erase(card)
		player.discard.erase(card)
		player.active_countermeasures.erase(card)
		for slot in range(player.support_line.size()):
			if player.support_line[slot] == card:
				player.support_line[slot] = null
	for slot in range(controller.state.frontline.size()):
		if controller.state.frontline[slot] == card:
			controller.state.frontline[slot] = null


static func _unit_defenses(controller: MatchController, owner_id: String) -> Dictionary:
	var defenses := {}
	for card in controller.state.players[owner_id].support_line:
		if card != null:
			defenses[card.instance_id] = card.current_defense
	for card in controller.state.frontline:
		if card != null and card.owner_id == owner_id:
			defenses[card.instance_id] = card.current_defense
	return defenses


static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(str(event.get("type", "")))
	return types


static func _controller_digest(controller: MatchController) -> Dictionary:
	var players := {}
	for player_id in controller.state.players:
		var player = controller.state.players[player_id]
		players[player_id] = {
			"credit": player.credit,
			"hand": _card_ids(player.hand),
			"support": _slot_ids(player.support_line),
			"discard": _card_ids(player.discard),
			"active_countermeasures": _card_ids(player.active_countermeasures),
		}
	return {
		"sequence": controller.state.sequence,
		"frontline": _slot_ids(controller.state.frontline),
		"frontline_controller_id": controller.state.frontline_controller_id,
		"players": players,
	}


static func _card_ids(cards: Array) -> Array[String]:
	var ids: Array[String] = []
	for card in cards:
		ids.append(card.instance_id)
	return ids


static func _slot_ids(cards: Array) -> Array:
	var ids: Array = []
	for card in cards:
		ids.append(null if card == null else card.instance_id)
	return ids
