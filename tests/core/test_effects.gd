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
	_test_nested_effect_limit_is_action_atomic(t)
	_test_nested_trigger_rejection_is_action_atomic(t)
	_test_queued_targets_revalidate_atomically(t)
	_test_random_target_context_is_sampled_once(t)
	_test_hq_lethal_is_a_prevention_checkpoint(t)
	_test_hq_terminal_stops_trailing_effects(t)
	_test_controller_trigger_checkpoints(t)
	_test_cancelled_attacks_pay_before_countermeasures(t)
	_test_temporary_modifiers_expire_through_turn_lifecycle(t)
	_test_reaction_checkpoints_halt_stale_actions(t)
	_test_temporary_modifier_clamping_and_hq_expiry_lethal(t)
	_test_combat_and_move_reactions_stop_stale_continuations(t)
	_test_terminal_countermeasure_cleanup_is_bound_to_reaction(t)
	_test_overlapping_temporary_modifiers_expire_in_reverse_order(t)
	_test_combat_status_grants_lethal_ambush_to_attacked_defender(t)
	_test_combat_status_cleans_up_after_surviving_combat(t)
	_test_combat_status_preserves_native_ambush(t)
	_test_combat_status_ignores_wrong_owner_and_non_target(t)
	_test_combat_status_cleans_up_after_cancellation(t)
	_test_combat_status_cleans_up_after_terminal_reaction(t)


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
	t.assert_eq(first.controller.state.rng_state, second.controller.state.rng_state, "accepted random action advances RNG deterministically once")


static func _test_nested_effect_limit_is_action_atomic(t) -> void:
	var counter_controller := _controller(528)
	var order := _card("nested-overflow-order", "player", "Order", 0, 0, 3)
	var counter := _card("nested-overflow-counter", "opponent", "Countermeasure")
	var counter_victim := _card("nested-overflow-counter-victim", "player", "Unit", 2, 5)
	counter_victim.abilities = [_overflow_death_ability("counter-overflow-death")]
	counter.abilities = [_ability(
		"counter-destroy",
		"order_played",
		{"selector": "enemy_unit", "count": 1},
		[{"type": "destroy"}],
		{"enemy": true}
	)]
	_put_hand(counter_controller, order)
	_put_hand(counter_controller, counter)
	_place_support(counter_controller, counter_victim, 0)
	counter.countermeasure_active = true
	counter.face_down = true
	counter_controller.state.players.opponent.active_countermeasures.append(counter)
	var counter_before := _controller_digest(counter_controller)
	var counter_result = counter_controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [counter_victim.instance_id], {}, counter_controller.state.sequence
	))
	_assert_atomic_effect_limit(t, counter_controller, counter_result, counter_before, "nested Countermeasure overflow")

	var order_controller := _controller(529)
	var destroy_order := _card("nested-death-order", "player", "Order", 0, 0, 3)
	var order_victim := _card("nested-death-order-victim", "opponent", "Unit", 2, 5)
	order_victim.abilities = [_overflow_death_ability("order-overflow-death")]
	destroy_order.abilities = [_ability(
		"destroy-for-overflow", "play_order", {"selector": "enemy_unit", "count": 1}, [{"type": "destroy"}]
	)]
	_put_hand(order_controller, destroy_order)
	_place_support(order_controller, order_victim, 0)
	var order_before := _controller_digest(order_controller)
	var order_result = order_controller.submit_action(GameAction.create(
		"play_order", "player", destroy_order.instance_id, [order_victim.instance_id], {}, order_controller.state.sequence
	))
	_assert_atomic_effect_limit(t, order_controller, order_result, order_before, "nested Order death overflow")

	var deploy_controller := _controller(530)
	var deployer := _card("nested-overflow-deployer", "player", "Unit", 2, 5, 2)
	var deploy_victim := _card("nested-overflow-deploy-victim", "opponent", "Unit", 2, 2)
	deploy_victim.abilities = [_overflow_death_ability("deploy-overflow-death")]
	deployer.abilities = [_ability(
		"deploy-random-lethal", "deploy", {"selector": "random_enemy_unit", "count": 1}, [{"type": "damage", "amount": 2}]
	)]
	_put_hand(deploy_controller, deployer)
	_place_support(deploy_controller, deploy_victim, 0)
	var deploy_before := _controller_digest(deploy_controller)
	var deploy_result = deploy_controller.submit_action(GameAction.create(
		"deploy_unit", "player", deployer.instance_id, [], {"support_slot": 0}, deploy_controller.state.sequence
	))
	_assert_atomic_effect_limit(t, deploy_controller, deploy_result, deploy_before, "nested deploy death overflow")

	var manual_controller := _controller(531)
	var manual_source := _card("nested-overflow-manual", "player")
	var manual_victim := _card("nested-overflow-manual-victim", "opponent", "Unit", 2, 2)
	manual_victim.abilities = [_overflow_death_ability("manual-overflow-death")]
	manual_source.abilities = [_ability(
		"manual-random-lethal", "manual", {"selector": "random_enemy_unit", "count": 1}, [{"type": "damage", "amount": 2}], {}, 2
	)]
	_place_support(manual_controller, manual_source, 0)
	_place_support(manual_controller, manual_victim, 0)
	var manual_before := _controller_digest(manual_controller)
	var manual_result = manual_controller.submit_action(GameAction.create(
		"activate_ability", "player", manual_source.instance_id, [], {"ability_id": "manual-random-lethal"}, manual_controller.state.sequence
	))
	_assert_atomic_effect_limit(t, manual_controller, manual_result, manual_before, "nested manual death overflow")


static func _assert_atomic_effect_limit(t, controller: MatchController, result, before: Dictionary, label: String) -> void:
	t.assert_true(not result.accepted, "%s is rejected" % label)
	t.assert_eq(result.reason_code, "effect_limit", "%s has a stable reason" % label)
	t.assert_eq(result.events, [], "%s exposes no partial events" % label)
	t.assert_eq(_controller_digest(controller), before, "%s restores match and RNG state" % label)


static func _overflow_death_ability(id: String) -> Dictionary:
	var effects: Array = []
	for index in range(GameConstants.MAX_EFFECT_EVENTS):
		effects.append({"type": "credit", "amount": 0, "player": "owner"})
	return _ability(id, "death", {"selector": "none"}, effects)


static func _test_nested_trigger_rejection_is_action_atomic(t) -> void:
	var controller := _controller(539)
	var order := _card("nested-invalid-death-order", "player", "Order", 0, 0, 2)
	var victim := _card("nested-invalid-death-victim", "opponent", "Unit", 2, 5)
	victim.abilities = [_ability(
		"missing-death-target",
		"death",
		{"selector": "random_enemy_unit", "count": 1},
		[{"type": "damage", "amount": 1}]
	)]
	order.abilities = [_ability(
		"destroy-invalid-death-source",
		"play_order",
		{"selector": "enemy_unit", "count": 1},
		[{"type": "destroy"}]
	)]
	_put_hand(controller, order)
	_place_support(controller, victim, 0)
	var before := _controller_digest(controller)
	var result = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [victim.instance_id], {}, controller.state.sequence
	))
	t.assert_true(not result.accepted, "nested trigger preparation failure rejects the parent action")
	t.assert_eq(result.reason_code, "invalid_target", "nested trigger preparation reports its validation error")
	t.assert_eq(_controller_digest(controller), before, "nested trigger preparation failure restores the whole action")


static func _test_queued_targets_revalidate_atomically(t) -> void:
	var return_controller := _controller(532)
	var return_order := _card("return-then-damage", "player", "Order", 0, 0, 2)
	var return_target := _card("return-then-damage-target", "opponent", "Unit", 2, 5)
	return_order.abilities = [_ability(
		"return-then-damage-effect",
		"play_order",
		{"selector": "enemy_unit", "count": 1},
		[{"type": "return"}, {"type": "damage", "amount": 2}]
	)]
	_put_hand(return_controller, return_order)
	_place_support(return_controller, return_target, 0)
	var return_before := _controller_digest(return_controller)
	var return_result = return_controller.submit_action(GameAction.create(
		"play_order", "player", return_order.instance_id, [return_target.instance_id], {}, return_controller.state.sequence
	))
	t.assert_true(not return_result.accepted, "return then damage rejects when the queued damage target leaves play")
	t.assert_eq(return_result.reason_code, "invalid_origin", "return then damage has a stable queued-target reason")
	t.assert_eq(_controller_digest(return_controller), return_before, "return then damage rolls back the entire Order")

	var retreat_controller := _controller(533)
	var repeated_retreat := _card("repeated-retreat", "player", "Order", 0, 0, 2)
	var retreat_target := _card("repeated-retreat-target", "opponent")
	repeated_retreat.abilities = [_ability(
		"retreat-twice",
		"play_order",
		{"selector": "enemy_unit", "count": 1},
		[{"type": "retreat"}, {"type": "retreat"}]
	)]
	_put_hand(retreat_controller, repeated_retreat)
	_place_frontline(retreat_controller, retreat_target, 0)
	var retreat_before := _controller_digest(retreat_controller)
	var retreat_result = retreat_controller.submit_action(GameAction.create(
		"play_order", "player", repeated_retreat.instance_id, [retreat_target.instance_id], {}, retreat_controller.state.sequence
	))
	t.assert_true(not retreat_result.accepted, "repeated Retreat rejects after the first queued move changes origin")
	t.assert_eq(retreat_result.reason_code, "invalid_origin", "repeated Retreat reports invalid origin")
	t.assert_eq(_controller_digest(retreat_controller), retreat_before, "repeated Retreat cannot duplicate a unit")

	var capacity_controller := _controller(534)
	var random_retreat := _card("random-retreat-source", "player")
	var first_frontline := _card("random-retreat-first", "opponent")
	var second_frontline := _card("random-retreat-second", "opponent")
	random_retreat.abilities = [_ability(
		"random-retreat-two",
		"manual",
		{"selector": "random_enemy_unit", "count": 2},
		[{"type": "retreat"}]
	)]
	_place_support(capacity_controller, random_retreat, 0)
	_place_frontline(capacity_controller, first_frontline, 0)
	_place_frontline(capacity_controller, second_frontline, 1)
	for slot in range(1, GameConstants.SUPPORT_UNIT_SLOTS):
		_place_support(capacity_controller, _card("random-retreat-blocker-%d" % slot, "opponent"), slot)
	var capacity_before := _controller_digest(capacity_controller)
	var capacity_result = capacity_controller.submit_action(GameAction.create(
		"activate_ability", "player", random_retreat.instance_id, [], {"ability_id": "random-retreat-two"}, capacity_controller.state.sequence
	))
	t.assert_eq(capacity_result.reason_code, "support_line_full", "random Retreat reserves Support capacity before sampling")
	t.assert_eq(_controller_digest(capacity_controller), capacity_before, "rejected random Retreat preserves state and RNG")


static func _test_random_target_context_is_sampled_once(t) -> void:
	var controller := _controller(535)
	var source := _card("shared-random-source", "player")
	source.abilities = [_ability(
		"shared-random-target",
		"manual",
		{"selector": "random_enemy_unit", "count": 1},
		[{"type": "damage", "amount": 1}, {"type": "damage", "amount": 2}]
	)]
	_place_support(controller, source, 0)
	for slot in range(3):
		_place_support(controller, _card("shared-random-target-%d" % slot, "opponent", "Unit", 1, 5), slot)
	var result = controller.submit_action(GameAction.create(
		"activate_ability", "player", source.instance_id, [], {"ability_id": "shared-random-target"}, controller.state.sequence
	))
	t.assert_true(result.accepted, "multi-effect random ability resolves")
	var defenses: Array = _unit_defenses(controller, "opponent").values()
	defenses.sort()
	t.assert_eq(defenses, [2, 5, 5], "all effects in one ability reuse the sampled random target set")


static func _test_hq_lethal_is_a_prevention_checkpoint(t) -> void:
	var controller := _controller(536)
	var order := _damage_order("preventable-hq-lethal", "player", 20, 2)
	var repair_counter := _card("hq-repair-counter", "opponent", "Countermeasure")
	repair_counter.abilities = [_ability(
		"repair-lethal-hq",
		"hq_lethal",
		{"selector": "friendly_hq", "count": 1},
		[{"type": "repair", "amount": 4}],
		{"enemy": true}
	)]
	_put_hand(controller, order)
	_put_hand(controller, repair_counter)
	repair_counter.countermeasure_active = true
	repair_counter.face_down = true
	controller.state.players.opponent.active_countermeasures.append(repair_counter)
	var result = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [controller.state.players.opponent.headquarters.instance_id], {}, controller.state.sequence
	))
	t.assert_true(result.accepted, "repair reaction accepts the lethal Order")
	t.assert_eq(controller.state.players.opponent.headquarters.current_defense, 4, "HQ lethal repair restores defense before finalization")
	t.assert_eq(controller.state.winner_id, "", "prevented HQ lethal chooses no winner")
	t.assert_eq(controller.state.phase, "action", "prevented HQ lethal keeps the match active")
	t.assert_true(controller.state.players.opponent.discard.has(repair_counter), "HQ lethal Countermeasure reveals after its repair")
	t.assert_true(not _event_types(result.events).has("match_ended"), "prevented HQ lethal emits no terminal event")
	t.assert_eq(_event_types(result.events), [
		"credit_spent", "order_played", "damage_dealt", "damage_repaired", "countermeasure_triggered", "card_discarded",
	], "HQ repair resolves before terminal finalization")

	var attack_controller := _controller(537)
	var attacker := _card("preventable-hq-attacker", "player", "Unit", 20, 5)
	attacker.operation_cost = 2
	attacker.deployed_turn = attack_controller.state.turn - 1
	var attack_counter := _card("hq-attack-repair-counter", "opponent", "Countermeasure")
	attack_counter.abilities = [_ability(
		"repair-attacked-hq", "hq_lethal", {"selector": "friendly_hq", "count": 1}, [{"type": "repair", "amount": 3}], {"enemy": true}
	)]
	_place_frontline(attack_controller, attacker, 0)
	_put_hand(attack_controller, attack_counter)
	attack_counter.countermeasure_active = true
	attack_counter.face_down = true
	attack_controller.state.players.opponent.active_countermeasures.append(attack_counter)
	var before_credit: int = attack_controller.state.players.player.credit
	var attack_result = attack_controller.submit_action(GameAction.create(
		"attack_hq",
		"player",
		attacker.instance_id,
		[attack_controller.state.players.opponent.headquarters.instance_id],
		{},
		attack_controller.state.sequence
	))
	t.assert_true(attack_result.accepted, "HQ attack can be saved by an HQ lethal reaction")
	t.assert_eq(attack_controller.state.players.player.credit, before_credit - 2, "prevented HQ attack still pays its operation")
	t.assert_eq(attacker.operations_used, 1, "prevented HQ attack still consumes its operation")
	t.assert_eq(attack_controller.state.players.opponent.headquarters.current_defense, 3, "HQ attack lethal repair restores defense")
	t.assert_eq(attack_controller.state.winner_id, "", "repaired HQ attack does not end the match")
	t.assert_eq(_event_types(attack_result.events), [
		"credit_spent", "attack_started", "damage_dealt", "damage_repaired", "countermeasure_triggered",
	], "HQ attack repair resolves before a terminal event")


static func _test_hq_terminal_stops_trailing_effects(t) -> void:
	var controller := _controller(538)
	var order := _card("terminal-trailing-order", "player", "Order", 0, 0, 2)
	order.abilities = [_ability(
		"terminal-then-credit",
		"play_order",
		{"selector": "enemy_unit_or_hq", "count": 1},
		[{"type": "damage", "amount": 20}, {"type": "credit", "amount": 5, "player": "owner"}]
	)]
	_put_hand(controller, order)
	var before_credit: int = controller.state.players.player.credit
	var result = controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [controller.state.players.opponent.headquarters.instance_id], {}, controller.state.sequence
	))
	var event_types := _event_types(result.events)
	t.assert_true(result.accepted, "unprevented lethal Headquarters Order resolves")
	t.assert_eq(controller.state.players.opponent.headquarters.current_defense, 0, "unprevented lethal HQ remains at zero defense")
	t.assert_eq(controller.state.winner_id, "player", "unprevented lethal HQ finalizes the winner")
	t.assert_eq(controller.state.phase, "complete", "unprevented lethal HQ completes the match")
	t.assert_eq(event_types.count("match_ended"), 1, "HQ lethal emits exactly one match-ended event")
	t.assert_true(not event_types.has("credit_changed"), "post-terminal queued Credit effect is discarded")
	t.assert_eq(controller.state.players.player.credit, before_credit - order.deployment_cost, "post-terminal queued Credit does not mutate state")
	t.assert_eq(event_types, ["credit_spent", "order_played", "damage_dealt", "match_ended", "card_discarded"], "terminal finalization precedes only Order cleanup")


static func _test_controller_trigger_checkpoints(t) -> void:
	var move_controller := _controller(540)
	var mover := _card("checkpoint-mover", "player")
	mover.abilities = [_ability("move-credit", "move", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}])]
	_place_support(move_controller, mover, 0)
	_ready(move_controller, mover)
	var move_result = move_controller.submit_action(GameAction.create(
		"move_unit", "player", mover.instance_id, [], {"zone": "frontline", "slot": 0}, move_controller.state.sequence
	))
	t.assert_true(move_result.accepted, "move checkpoint action resolves")
	t.assert_true(_event_types(move_result.events).has("credit_changed"), "move trigger resolves after controller movement")

	var draw_controller := _controller(541)
	var drawn := _card("checkpoint-drawn", "player")
	drawn.abilities = [_ability("draw-credit", "draw", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}])]
	_put_deck(draw_controller, drawn)
	var draw_events := draw_controller.debug_draw("player")
	t.assert_true(_event_types(draw_events).has("credit_changed"), "draw checkpoint resolves for the drawn card")

	var discard_controller := _controller(542)
	var discard_order := _card("checkpoint-discard-order", "player", "Order", 0, 0, 1)
	discard_order.abilities = [_ability("discard-credit", "discard", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}])]
	_put_hand(discard_controller, discard_order)
	var discard_result = discard_controller.submit_action(GameAction.create(
		"play_order", "player", discard_order.instance_id, [], {}, discard_controller.state.sequence
	))
	t.assert_true(discard_result.accepted, "order discard checkpoint action resolves")
	t.assert_true(_event_types(discard_result.events).has("credit_changed"), "discard trigger resolves after the order enters discard")

	var combat_controller := _controller(543)
	var attacker := _card("checkpoint-attacker", "player", "Unit", 2, 5)
	var defender := _card("checkpoint-defender", "opponent", "Unit", 1, 5)
	defender.abilities = [
		_ability("defend-credit", "defend", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}]),
		_ability("damage-credit", "damage", {"selector": "none"}, [{"type": "credit", "amount": 2, "player": "owner"}]),
	]
	_place_support(combat_controller, attacker, 0)
	_place_frontline(combat_controller, defender, 0)
	_ready(combat_controller, attacker)
	var combat_result = combat_controller.submit_action(GameAction.create(
		"attack_unit", "player", attacker.instance_id, [defender.instance_id], {}, combat_controller.state.sequence
	))
	t.assert_true(combat_result.accepted, "combat checkpoints action resolves")
	t.assert_eq(combat_controller.state.players.opponent.credit, 13, "defend and damage triggers both resolve exactly once")

	var frontline_controller := _controller(544)
	var frontline_unit := _card("checkpoint-frontline-unit", "player", "Unit", 1, 1)
	frontline_unit.abilities = [
		_ability("frontline-gain-credit", "frontline_gained", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}]),
		_ability("frontline-loss-credit", "frontline_lost", {"selector": "none"}, [{"type": "credit", "amount": 2, "player": "owner"}]),
	]
	var enemy := _card("checkpoint-frontline-enemy", "opponent", "Unit", 2, 5)
	_place_support(frontline_controller, frontline_unit, 0)
	_ready(frontline_controller, frontline_unit)
	var gained_result = frontline_controller.submit_action(GameAction.create(
		"move_unit", "player", frontline_unit.instance_id, [], {"zone": "frontline", "slot": 0}, frontline_controller.state.sequence
	))
	t.assert_true(gained_result.accepted, "Frontline gain action resolves")
	t.assert_eq(frontline_controller.state.players.player.credit, 11, "Frontline gained trigger resolves once after operation payment")
	frontline_controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, frontline_controller.state.sequence))
	_place_support(frontline_controller, enemy, 0)
	_ready(frontline_controller, enemy)
	var lost_result = frontline_controller.submit_action(GameAction.create(
		"attack_unit", "opponent", enemy.instance_id, [frontline_unit.instance_id], {}, frontline_controller.state.sequence
	))
	t.assert_true(lost_result.accepted, "Frontline loss combat action resolves")
	t.assert_eq(frontline_controller.state.players.player.credit, 13, "Frontline lost trigger resolves once when combat removes control")


static func _test_cancelled_attacks_pay_before_countermeasures(t) -> void:
	for target_type in ["unit", "headquarters"]:
		var controller := _controller(545 if target_type == "unit" else 546)
		var attacker := _card("cancelled-%s-attacker" % target_type, "opponent", "Unit", 3, 5)
		attacker.operation_cost = 2
		var target = controller.state.players.player.headquarters
		if target_type == "unit":
			target = _card("cancelled-unit-defender", "player", "Unit", 1, 5)
			_place_frontline(controller, target, 0)
			_place_support(controller, attacker, 0)
		else:
			_place_frontline(controller, attacker, 0)
		var counter := _countermeasure("cancelled-%s-counter" % target_type, "player", 1)
		counter.abilities = [
			_ability("cancel-attack", "attack", {"selector": "none"}, [{"type": "replace_event", "changes": {"cancelled": true}}], {"enemy": true}),
			_ability("counter-triggered-credit", "countermeasure_triggered", {"selector": "none"}, [{"type": "credit", "amount": 1, "player": "owner"}]),
		]
		_put_hand(controller, counter)
		counter.countermeasure_active = true
		counter.face_down = true
		controller.state.players.player.active_countermeasures.append(counter)
		controller.state.active_player_id = "opponent"
		_ready(controller, attacker)
		var before_credit: int = controller.state.players.opponent.credit
		var action := GameAction.create(
			"attack_unit" if target_type == "unit" else "attack_hq",
			"opponent",
			attacker.instance_id,
			[target.instance_id],
			{},
			controller.state.sequence
		)
		var result = controller.submit_action(action)
		t.assert_true(result.accepted, "%s attack Countermeasure cancellation resolves" % target_type)
		t.assert_eq(controller.state.players.opponent.credit, before_credit - attacker.operation_cost, "%s attack still pays before cancellation" % target_type)
		t.assert_eq(attacker.operations_used, 1, "%s attack reserves its operation before cancellation" % target_type)
		t.assert_eq(_event_types(result.events).slice(0, 2), ["credit_spent", "attack_started"], "%s attack exposes payment before reaction" % target_type)
		t.assert_true(_event_types(result.events).has("countermeasure_triggered"), "%s attack reveals the triggered Countermeasure" % target_type)
		t.assert_eq(controller.state.players.player.credit, 11, "%s Countermeasure triggered checkpoint resolves once" % target_type)


static func _test_temporary_modifiers_expire_through_turn_lifecycle(t) -> void:
	var controller := _controller(547)
	var source := _card("temporary-modifier-source", "player")
	var target := _card("temporary-modifier-target", "opponent", "Unit", 2, 3)
	source.abilities = [_ability(
		"temporary-debuff",
		"manual",
		{"selector": "enemy_unit", "count": 1},
		[{"type": "debuff", "attack": 1, "defense": 2, "duration": "turn"}]
	)]
	_place_support(controller, source, 0)
	_place_support(controller, target, 0)
	var applied = controller.submit_action(GameAction.create(
		"activate_ability", "player", source.instance_id, [target.instance_id], {"ability_id": "temporary-debuff"}, controller.state.sequence
	))
	t.assert_true(applied.accepted, "temporary modifier action resolves")
	t.assert_eq([target.current_attack, target.current_defense], [1, 1], "temporary debuff applies its stat deltas")
	t.assert_eq(target.modifiers.size(), 1, "temporary debuff records expiry metadata")
	var expired = controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
	t.assert_true(expired.accepted, "turn end expires temporary modifier")
	t.assert_eq([target.current_attack, target.current_defense], [2, 3], "temporary modifier reverses both stats")
	t.assert_eq(target.modifiers.size(), 0, "temporary modifier is removed after expiry")
	t.assert_true(_event_types(expired.events).has("modifier_expired"), "expiry emits a deterministic lifecycle event")
	controller.submit_action(GameAction.create("end_turn", "opponent", "", [], {}, controller.state.sequence))
	t.assert_eq([target.current_attack, target.current_defense], [2, 3], "expired modifier cannot reverse stats twice")


static func _test_reaction_checkpoints_halt_stale_actions(t) -> void:
	var order_controller := _controller(548)
	var order := _damage_order("reaction-return-order", "player", 3, 2)
	var order_target := _card("reaction-return-order-target", "opponent", "Unit", 1, 5)
	var order_counter := _countermeasure("reaction-return-order-counter", "opponent", 1)
	order_counter.abilities = [_ability(
		"return-order-target", "order_played", {"selector": "action_targets", "count": 1}, [{"type": "return"}], {"enemy": true}
	)]
	_put_hand(order_controller, order)
	_put_hand(order_controller, order_counter)
	_place_support(order_controller, order_target, 0)
	order_counter.countermeasure_active = true
	order_counter.face_down = true
	order_controller.state.players.opponent.active_countermeasures.append(order_counter)
	var order_result = order_controller.submit_action(GameAction.create(
		"play_order", "player", order.instance_id, [order_target.instance_id], {}, order_controller.state.sequence
	))
	t.assert_true(order_result.accepted, "zone-changing Order reaction accepts the paid action")
	t.assert_eq(order_controller.state.players.player.credit, 8, "halted Order remains paid")
	t.assert_eq(order_target.zone, "hand", "reaction returns the Order target before resolution")
	t.assert_true(not _event_types(order_result.events).has("damage_dealt"), "halted Order cannot damage a returned target")
	t.assert_eq(order_controller.state.players.player.discard.count(order), 1, "halted Order cleanup leaves one discard copy")
	t.assert_true(not order_controller.state.players.player.hand.has(order), "halted Order leaves no duplicate hand copy")

	var unit_controller := _controller(549)
	var attacker := _card("reaction-return-attacker", "player", "Unit", 3, 5)
	attacker.operation_cost = 2
	var defender := _card("reaction-return-defender", "opponent", "Unit", 1, 5)
	var attack_counter := _countermeasure("reaction-return-attack-counter", "opponent", 1)
	attack_counter.abilities = [_ability(
		"return-attack-target", "attack", {"selector": "action_targets", "count": 1}, [{"type": "return"}], {"enemy": true}
	)]
	_place_support(unit_controller, attacker, 0)
	_place_frontline(unit_controller, defender, 0)
	_put_hand(unit_controller, attack_counter)
	attack_counter.countermeasure_active = true
	attack_counter.face_down = true
	unit_controller.state.players.opponent.active_countermeasures.append(attack_counter)
	_ready(unit_controller, attacker)
	var unit_result = unit_controller.submit_action(GameAction.create(
		"attack_unit", "player", attacker.instance_id, [defender.instance_id], {}, unit_controller.state.sequence
	))
	t.assert_true(unit_result.accepted, "zone-changing unit reaction accepts the paid attack")
	t.assert_eq(unit_controller.state.players.player.credit, 8, "halted unit attack keeps its operation payment")
	t.assert_eq(attacker.operations_used, 1, "halted unit attack keeps its reserved operation")
	t.assert_eq(defender.zone, "hand", "reaction returns the unit defender")
	t.assert_true(not _event_types(unit_result.events).has("damage_dealt"), "halted unit attack cannot damage a returned defender")
	t.assert_eq(unit_controller.state.players.opponent.hand.count(defender), 1, "returned defender has one hand copy")

	var hq_controller := _controller(550)
	var hq_attacker := _card("reaction-terminal-hq-attacker", "player", "Unit", 3, 5)
	hq_attacker.unit_type = "Artillery"
	hq_attacker.operation_cost = 2
	hq_attacker.abilities = [_ability(
		"terminal-attack-reaction", "attack", {"selector": "friendly_hq", "count": 1}, [{"type": "damage", "amount": 20}]
	)]
	_place_support(hq_controller, hq_attacker, 0)
	_ready(hq_controller, hq_attacker)
	var hq_result = hq_controller.submit_action(GameAction.create(
		"attack_hq", "player", hq_attacker.instance_id, [hq_controller.state.players.opponent.headquarters.instance_id], {}, hq_controller.state.sequence
	))
	t.assert_true(hq_result.accepted, "terminal HQ reaction accepts the paid attack")
	t.assert_eq(hq_controller.state.players.player.credit, 8, "terminal HQ reaction keeps operation payment")
	t.assert_eq(hq_controller.state.players.opponent.headquarters.current_defense, 20, "terminal reaction stops the normal HQ damage")
	t.assert_eq(hq_controller.state.winner_id, "opponent", "terminal reaction chooses the reaction winner")
	t.assert_eq(_event_types(hq_result.events).count("match_ended"), 1, "terminal reaction finalizes exactly once")


static func _test_temporary_modifier_clamping_and_hq_expiry_lethal(t) -> void:
	var clamp_controller := _controller(551)
	var clamp_source := _card("clamped-modifier-source", "player")
	var clamp_target := _card("clamped-modifier-target", "opponent", "Unit", 2, 1)
	clamp_source.abilities = [_ability(
		"clamped-temporary-debuff", "manual", {"selector": "enemy_unit", "count": 1},
		[{"type": "debuff", "defense": 2, "duration": "turn"}]
	)]
	_place_support(clamp_controller, clamp_source, 0)
	_place_support(clamp_controller, clamp_target, 0)
	var clamped = clamp_controller.submit_action(GameAction.create(
		"activate_ability", "player", clamp_source.instance_id, [clamp_target.instance_id], {"ability_id": "clamped-temporary-debuff"}, clamp_controller.state.sequence
	))
	t.assert_true(clamped.accepted, "clamped temporary debuff action resolves")
	t.assert_eq(clamp_target.current_defense, 0, "temporary debuff clamps defense at zero")
	t.assert_eq(clamp_target.modifiers[0].defense_delta, -1, "modifier records the actual clamped defense delta")
	clamp_controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, clamp_controller.state.sequence))
	t.assert_eq(clamp_target.current_defense, 1, "expiry restores exactly the clamped defense delta")
	t.assert_eq(clamp_target.modifiers.size(), 0, "clamped modifier is removed once after expiry")

	var hq_controller := _controller(552)
	var hq_source := _card("expiry-hq-source", "player")
	var hq = hq_controller.state.players.opponent.headquarters
	hq_source.abilities = [_ability(
		"temporary-hq-buffer", "manual", {"selector": "enemy_unit_or_hq", "count": 1},
		[{"type": "buff", "defense": 2, "duration": "turn"}]
	)]
	hq.abilities = [_ability(
		"expiry-hq-repair", "hq_lethal", {"selector": "friendly_hq", "count": 1}, [{"type": "repair", "amount": 3}]
	)]
	_place_support(hq_controller, hq_source, 0)
	_put_deck(hq_controller, _card("expiry-hq-next-draw", "opponent"))
	var buffered = hq_controller.submit_action(GameAction.create(
		"activate_ability", "player", hq_source.instance_id, [hq.instance_id], {"ability_id": "temporary-hq-buffer"}, hq_controller.state.sequence
	))
	t.assert_true(buffered.accepted, "temporary HQ buffer action resolves")
	hq.current_defense = 1
	var expiry = hq_controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, hq_controller.state.sequence))
	t.assert_true(expiry.accepted, "HQ modifier expiry action resolves")
	t.assert_eq(hq.current_defense, 3, "HQ lethal reaction repairs after modifier expiry")
	t.assert_eq(hq_controller.state.winner_id, "", "HQ expiry repair prevents defeat")
	t.assert_true(not _event_types(expiry.events).has("match_ended"), "prevented HQ expiry emits no terminal event")


static func _test_combat_and_move_reactions_stop_stale_continuations(t) -> void:
	var return_controller := _controller(553)
	var attacker := _card("damage-return-attacker", "player", "Unit", 3, 5)
	attacker.operation_cost = 2
	var defender := _card("damage-return-defender", "opponent", "Unit", 1, 5)
	defender.abilities = [_ability(
		"return-damaging-attacker", "damage", {"selector": "action_targets", "count": 1}, [{"type": "return"}]
	)]
	_place_support(return_controller, attacker, 0)
	_place_frontline(return_controller, defender, 0)
	_ready(return_controller, attacker)
	var returned = return_controller.submit_action(GameAction.create(
		"attack_unit", "player", attacker.instance_id, [defender.instance_id], {}, return_controller.state.sequence
	))
	t.assert_true(returned.accepted, "damage reaction returning attacker accepts the paid combat action")
	t.assert_eq(_event_types(returned.events).count("damage_dealt"), 1, "returned attacker receives no stale retaliation damage")
	t.assert_eq(attacker.zone, "hand", "damage reaction returns the attacker")
	t.assert_eq(return_controller.state.players.player.hand.count(attacker), 1, "returned attacker has one hand copy")
	t.assert_true(not return_controller.state.players.player.discard.has(attacker), "returned attacker is not discarded from a stale combat reference")

	var death_controller := _controller(554)
	var death_attacker := _card("terminal-death-attacker", "player", "Unit", 3, 5)
	var death_defender := _card("terminal-death-defender", "opponent", "Unit", 1, 1)
	death_defender.abilities = [_ability(
		"terminal-death", "death", {"selector": "friendly_hq", "count": 1}, [{"type": "damage", "amount": 20}]
	)]
	_place_support(death_controller, death_attacker, 0)
	_place_frontline(death_controller, death_defender, 0)
	_ready(death_controller, death_attacker)
	var terminal = death_controller.submit_action(GameAction.create(
		"attack_unit", "player", death_attacker.instance_id, [death_defender.instance_id], {}, death_controller.state.sequence
	))
	t.assert_true(terminal.accepted, "terminal death reaction accepts combat action")
	var terminal_types := _event_types(terminal.events)
	var terminal_index := terminal_types.find("match_ended")
	t.assert_true(terminal_index >= 0, "terminal death reaction finalizes the match")
	t.assert_true(not terminal_types.slice(terminal_index + 1).has("frontline_changed"), "terminal death stops stale Frontline updates")

	var move_controller := _controller(555)
	var mover := _card("terminal-move-source", "player")
	mover.abilities = [_ability(
		"terminal-move", "move", {"selector": "friendly_hq", "count": 1}, [{"type": "damage", "amount": 20}]
	)]
	_place_support(move_controller, mover, 0)
	_ready(move_controller, mover)
	var moved = move_controller.submit_action(GameAction.create(
		"move_unit", "player", mover.instance_id, [], {"zone": "frontline", "slot": 0}, move_controller.state.sequence
	))
	t.assert_true(moved.accepted, "terminal move reaction accepts the movement action")
	t.assert_eq(move_controller.state.frontline_controller_id, "player", "terminal move preserves Frontline controller consistency")
	t.assert_true(not _event_types(moved.events).has("frontline_changed"), "terminal move emits no post-match Frontline event")


static func _test_terminal_countermeasure_cleanup_is_bound_to_reaction(t) -> void:
	var controller := _controller(556)
	var attacker := _card("terminal-counter-attacker", "opponent", "Unit", 3, 5)
	var defender := _card("terminal-counter-defender", "player", "Unit", 1, 5)
	var counter := _countermeasure("terminal-counter", "player", 1)
	counter.abilities = [_ability(
		"terminal-counter-effect", "attack", {"selector": "friendly_hq", "count": 1}, [{"type": "damage", "amount": 20}], {"enemy": true}
	)]
	_place_support(controller, attacker, 0)
	_place_frontline(controller, defender, 0)
	_put_hand(controller, counter)
	counter.countermeasure_active = true
	counter.face_down = true
	controller.state.players.player.active_countermeasures.append(counter)
	controller.state.active_player_id = "opponent"
	_ready(controller, attacker)
	var result = controller.submit_action(GameAction.create(
		"attack_unit", "opponent", attacker.instance_id, [defender.instance_id], {}, controller.state.sequence
	))
	t.assert_true(result.accepted, "terminal Countermeasure reaction accepts the paid attack")
	t.assert_true(not counter.countermeasure_active and not counter.face_down, "terminal Countermeasure is revealed and inactive")
	t.assert_eq(counter.zone, "discard", "terminal Countermeasure enters discard")
	t.assert_eq(controller.state.players.player.discard.count(counter), 1, "terminal Countermeasure has one discard copy")
	var event_types := _event_types(result.events)
	t.assert_true(event_types.find("countermeasure_triggered") >= 0, "terminal Countermeasure emits its trigger event")
	t.assert_true(event_types.find("countermeasure_triggered") < event_types.find("match_ended"), "Countermeasure cleanup event precedes terminal match event")


static func _test_overlapping_temporary_modifiers_expire_in_reverse_order(t) -> void:
	var controller := _controller(557)
	var source := _card("overlap-modifier-source", "player")
	var target := _card("overlap-modifier-target", "opponent", "Unit", 2, 3)
	source.abilities = [_ability(
		"overlap-modifiers", "manual", {"selector": "enemy_unit", "count": 1}, [
			{"type": "buff", "defense": 4, "duration": "turn"},
			{"type": "debuff", "defense": 8, "duration": "turn"},
		]
	)]
	_place_support(controller, source, 0)
	_place_support(controller, target, 0)
	var applied = controller.submit_action(GameAction.create(
		"activate_ability", "player", source.instance_id, [target.instance_id], {"ability_id": "overlap-modifiers"}, controller.state.sequence
	))
	t.assert_true(applied.accepted, "overlapping temporary modifiers apply")
	t.assert_eq(target.current_defense, 0, "overlapping modifier sequence clamps defense at zero")
	controller.submit_action(GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
	t.assert_eq(target.current_defense, 3, "reverse-order expiry restores original defense exactly")
	t.assert_eq(target.modifiers.size(), 0, "overlapping modifiers expire once")


static func _test_combat_status_grants_lethal_ambush_to_attacked_defender(t) -> void:
	var fixture := _ambush_attack_fixture(558, 3, 4, 2, 2)
	var result = _submit_fixture_attack(fixture)
	t.assert_true(result.accepted, "temporary Ambush attack resolves")
	t.assert_eq(fixture.attacker.current_defense, 0, "attacked defender deals lethal Ambush damage first")
	t.assert_eq(fixture.attacker.zone, "discard", "lethal temporary Ambush destroys the attacker")
	t.assert_eq(fixture.defender.current_defense, 4, "lethal Ambush prevents attacker damage")
	t.assert_eq(fixture.counter.zone, "discard", "triggered combat Countermeasure is revealed and discarded")
	t.assert_true(not fixture.counter.face_down and not fixture.counter.countermeasure_active, "triggered combat Countermeasure is inactive")


static func _test_combat_status_cleans_up_after_surviving_combat(t) -> void:
	var fixture := _ambush_attack_fixture(559, 2, 5, 1, 5)
	var result = _submit_fixture_attack(fixture)
	t.assert_true(result.accepted, "surviving temporary Ambush combat resolves")
	t.assert_eq([fixture.attacker.current_defense, fixture.defender.current_defense], [3, 4], "Ambush precedes normal attack when attacker survives")
	t.assert_true(not fixture.defender.has_keyword_or_status("Ambush"), "temporary Ambush is removed after combat")


static func _test_combat_status_preserves_native_ambush(t) -> void:
	var fixture := _ambush_attack_fixture(560, 2, 5, 1, 5)
	fixture.defender.keywords.append("Ambush")
	var result = _submit_fixture_attack(fixture)
	t.assert_true(result.accepted, "native Ambush defender combat resolves")
	t.assert_true(fixture.defender.has_keyword_or_status("Ambush"), "combat cleanup preserves native Ambush")


static func _test_combat_status_ignores_wrong_owner_and_non_target(t) -> void:
	var fixture := _ambush_attack_fixture(561, 2, 5, 1, 5)
	var bystander := _card("ambush-bystander", "player", "Unit", 4, 4)
	_place_support(fixture.controller, bystander, 1)
	var result = _submit_fixture_attack(fixture)
	t.assert_true(result.accepted, "targeted defender attack resolves")
	t.assert_true(not bystander.has_keyword_or_status("Ambush"), "combat status does not affect a friendly non-target")

	var wrong_owner := _ambush_attack_fixture(562, 2, 5, 1, 5)
	wrong_owner.counter.owner_id = "opponent"
	wrong_owner.controller.state.players.player.active_countermeasures.erase(wrong_owner.counter)
	wrong_owner.controller.state.players.player.hand.erase(wrong_owner.counter)
	wrong_owner.controller.state.players.opponent.hand.append(wrong_owner.counter)
	wrong_owner.controller.state.players.opponent.active_countermeasures.append(wrong_owner.counter)
	var wrong_result = _submit_fixture_attack(wrong_owner)
	t.assert_true(wrong_result.accepted, "wrong-owner Countermeasure attack resolves without reaction")
	t.assert_eq(wrong_owner.counter.zone, "hand", "attacker-owned Countermeasure does not trigger")
	t.assert_eq(wrong_owner.defender.current_defense, 4, "wrong-owner Countermeasure grants no Ambush")


static func _test_combat_status_cleans_up_after_cancellation(t) -> void:
	var fixture := _ambush_attack_fixture(563, 2, 5, 1, 5)
	fixture.counter.abilities.append(_ability(
		"cancel-same-attack", "attack", {"selector": "none"},
		[{"type": "replace_event", "changes": {"cancelled": true}}], {"enemy": true}
	))
	var result = _submit_fixture_attack(fixture)
	t.assert_true(result.accepted, "cancelled attack resolves its Countermeasure reaction")
	t.assert_eq(fixture.defender.current_defense, 5, "cancelled attack deals no combat damage")
	t.assert_true(not fixture.defender.has_keyword_or_status("Ambush"), "temporary Ambush is removed after cancellation")


static func _test_combat_status_cleans_up_after_terminal_reaction(t) -> void:
	var fixture := _ambush_attack_fixture(564, 2, 5, 1, 5)
	fixture.counter.abilities.append(_ability(
		"terminal-same-attack", "attack", {"selector": "friendly_hq", "count": 1},
		[{"type": "damage", "amount": 20}], {"enemy": true}
	))
	var result = _submit_fixture_attack(fixture)
	t.assert_true(result.accepted, "terminal attack reaction resolves")
	t.assert_eq(fixture.controller.state.phase, "complete", "reaction ends match before combat")
	t.assert_true(not fixture.defender.has_keyword_or_status("Ambush"), "temporary Ambush is removed after terminal reaction")


static func _ambush_attack_fixture(seed: int, defender_attack: int, defender_defense: int, attacker_attack: int, attacker_defense: int) -> Dictionary:
	var controller := _controller(seed)
	var attacker := _card("ambush-attacker-%d" % seed, "opponent", "Unit", attacker_attack, attacker_defense)
	var defender := _card("ambush-defender-%d" % seed, "player", "Unit", defender_attack, defender_defense)
	var counter := _countermeasure("ambush-counter-%d" % seed, "player", 0)
	counter.abilities = [_ability(
		"temporary-ambush", "attack", {"selector": "action_targets", "count": 1},
		[{"type": "status", "status": "Ambush", "duration": "combat"}],
		{"enemy": true, "target_owner": "owner", "target_category": "Unit"}
	)]
	_place_support(controller, attacker, 0)
	_place_frontline(controller, defender, 0)
	_put_hand(controller, counter)
	counter.countermeasure_active = true
	counter.face_down = true
	controller.state.players.player.active_countermeasures.append(counter)
	controller.state.active_player_id = "opponent"
	_ready(controller, attacker)
	return {"controller": controller, "attacker": attacker, "defender": defender, "counter": counter}


static func _submit_fixture_attack(fixture: Dictionary):
	return fixture.controller.submit_action(GameAction.create(
		"attack_unit", "opponent", fixture.attacker.instance_id, [fixture.defender.instance_id], {}, fixture.controller.state.sequence
	))


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


static func _ready(controller: MatchController, card) -> void:
	card.deployed_turn = controller.state.turn - 1
	card.operations_used = 0


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
			"credit_slots": player.credit_slots,
			"fatigue": player.fatigue,
			"hand": _card_ids(player.hand),
			"support": _slot_ids(player.support_line),
			"discard": _card_ids(player.discard),
			"active_countermeasures": _card_ids(player.active_countermeasures),
			"cards": _card_states(controller, player_id),
		}
	return {
		"sequence": controller.state.sequence,
		"phase": controller.state.phase,
		"winner_id": controller.state.winner_id,
		"rng_state": controller.state.rng_state,
		"frontline": _slot_ids(controller.state.frontline),
		"frontline_controller_id": controller.state.frontline_controller_id,
		"players": players,
	}


static func _card_states(controller: MatchController, owner_id: String) -> Dictionary:
	var cards := {}
	var player = controller.state.players[owner_id]
	var collections: Array = [player.deck, player.hand, player.support_line, player.discard, controller.state.frontline]
	for collection in collections:
		for card in collection:
			if card == null or card.owner_id != owner_id or cards.has(card.instance_id):
				continue
			cards[card.instance_id] = {
				"zone": card.zone,
				"slot": card.slot,
				"attack": card.current_attack,
				"defense": card.current_defense,
				"operations_used": card.operations_used,
				"operation_chain": card.operation_chain,
				"modifiers": card.modifiers.duplicate(true),
				"statuses": card.statuses.duplicate(true),
				"countermeasure_active": card.countermeasure_active,
				"countermeasure_activation_cost": card.countermeasure_activation_cost,
				"face_down": card.face_down,
			}
	return cards


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
