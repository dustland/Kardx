extends RefCounted

const ContentCatalog = preload("res://scripts/content/content_catalog.gd")


static func run(t) -> void:
	var catalog = ContentCatalog.load_from_paths(
		"res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json"
	)
	_test_us_hq(t, catalog)
	_test_us_rifle_platoon(t, catalog)
	_test_us_combat_engineers(t, catalog)
	_test_us_field_hospital(t, catalog)
	_test_us_supply_column(t, catalog)
	_test_us_forward_observers(t, catalog)
	_test_us_p40_patrol(t, catalog)
	_test_us_rapid_resupply(t, catalog)
	_test_us_signal_watch(t, catalog)
	_test_us_ranger_company(t, catalog)
	_test_us_armored_group(t, catalog)
	_test_us_field_battery(t, catalog)
	_test_us_emergency_repairs(t, catalog)
	_test_us_tank_hunters(t, catalog)
	_test_us_b25_strike_group(t, catalog)
	_test_us_air_superiority(t, catalog)
	_test_us_combined_arms(t, catalog)
	_test_su_hq(t, catalog)
	_test_su_guards_rifle(t, catalog)
	_test_su_siberian_volunteers(t, catalog)
	_test_su_combat_sappers(t, catalog)
	_test_su_medical_battalion(t, catalog)
	_test_su_rail_convoy(t, catalog)
	_test_su_partisan_scouts(t, catalog)
	_test_su_massed_assault(t, catalog)
	_test_su_maskirovka(t, catalog)
	_test_su_t34_spearhead(t, catalog)
	_test_su_heavy_breakthrough(t, catalog)
	_test_su_katyusha_battery(t, catalog)
	_test_su_hold_the_line(t, catalog)
	_test_su_yak_patrol(t, catalog)
	_test_su_pe2_bomber_wing(t, catalog)
	_test_su_deep_battle(t, catalog)
	_test_su_artillery_preparation(t, catalog)


static func _test_us_hq(t, catalog) -> void: _card(t, catalog, "us-hq", "US Command Post", "UnitedStates", "Headquarters", "", "Standard", 0, 0, 0, 20)
static func _test_us_rifle_platoon(t, catalog) -> void: _card(t, catalog, "us-rifle-platoon", "Rifle Platoon", "UnitedStates", "Unit", "Infantry", "Standard", 1, 1, 1, 2)
static func _test_us_combat_engineers(t, catalog) -> void: _ability(t, catalog, "us-combat-engineers", "deploy", "friendly_hq", "repair", 1)
static func _test_us_field_hospital(t, catalog) -> void: _ability(t, catalog, "us-field-hospital", "deploy", "friendly_unit", "repair", 2)
static func _test_us_supply_column(t, catalog) -> void: _ability(t, catalog, "us-supply-column", "deploy", "none", "credit", 1)
static func _test_us_forward_observers(t, catalog) -> void: _ability(t, catalog, "us-forward-observers", "frontline_gained", "none", "draw", 1)
static func _test_us_p40_patrol(t, catalog) -> void: _keyword(t, catalog, "us-p40-patrol", "Smokescreen")
static func _test_us_rapid_resupply(t, catalog) -> void: _ability(t, catalog, "us-rapid-resupply", "play_order", "none", "draw", 2)
static func _test_us_signal_watch(t, catalog) -> void: _ability(t, catalog, "us-signal-watch", "order_played", "none", "replace_event", -1)
static func _test_us_ranger_company(t, catalog) -> void: _keyword(t, catalog, "us-ranger-company", "Blitz")
static func _test_us_armored_group(t, catalog) -> void: _keyword(t, catalog, "us-armored-group", "Fury")
static func _test_us_field_battery(t, catalog) -> void: _card(t, catalog, "us-field-battery", "105mm Field Battery", "UnitedStates", "Unit", "Artillery", "Limited", 4, 2, 4, 3)
static func _test_us_emergency_repairs(t, catalog) -> void: _ability(t, catalog, "us-emergency-repairs", "hq_lethal", "friendly_hq", "repair", 3)
static func _test_us_tank_hunters(t, catalog) -> void: _ability(t, catalog, "us-tank-hunters", "attack", "action_targets", "damage", 2)
static func _test_us_b25_strike_group(t, catalog) -> void: _keyword(t, catalog, "us-b25-strike-group", "Bypass Guard")
static func _test_us_air_superiority(t, catalog) -> void: _ability(t, catalog, "us-air-superiority", "play_order", "enemy_air_units", "damage", 3)
static func _test_us_combined_arms(t, catalog) -> void: _ability(t, catalog, "us-combined-arms", "play_order", "friendly_units", "buff", 1)
static func _test_su_hq(t, catalog) -> void: _card(t, catalog, "su-hq", "Soviet Command Post", "SovietUnion", "Headquarters", "", "Standard", 0, 0, 0, 20)
static func _test_su_guards_rifle(t, catalog) -> void: _keyword(t, catalog, "su-guards-rifle", "Guard")
static func _test_su_siberian_volunteers(t, catalog) -> void: _ability(t, catalog, "su-siberian-volunteers", "damage", "self", "buff", 1)
static func _test_su_combat_sappers(t, catalog) -> void: _ability(t, catalog, "su-combat-sappers", "deploy", "enemy_unit", "damage", 1)
static func _test_su_medical_battalion(t, catalog) -> void: _ability(t, catalog, "su-medical-battalion", "turn_end", "friendly_units", "repair", 1)
static func _test_su_rail_convoy(t, catalog) -> void: _ability(t, catalog, "su-rail-convoy", "deploy", "none", "credit_slots", 1)
static func _test_su_partisan_scouts(t, catalog) -> void: _ability(t, catalog, "su-partisan-scouts", "deploy", "random_enemy_hand", "reveal", -1)
static func _test_su_massed_assault(t, catalog) -> void: _ability(t, catalog, "su-massed-assault", "play_order", "friendly_infantry", "buff", 1)
static func _test_su_maskirovka(t, catalog) -> void: _ability(t, catalog, "su-maskirovka", "defend", "action_targets", "status", -1)
static func _test_su_t34_spearhead(t, catalog) -> void: _keyword(t, catalog, "su-t34-spearhead", "Blitz")
static func _test_su_heavy_breakthrough(t, catalog) -> void: _keyword(t, catalog, "su-heavy-breakthrough", "Heavy Armor")
static func _test_su_katyusha_battery(t, catalog) -> void: _ability(t, catalog, "su-katyusha-battery", "attack", "adjacent_enemy_units", "damage", 1)
static func _test_su_hold_the_line(t, catalog) -> void: _ability(t, catalog, "su-hold-the-line", "frontline_lost", "friendly_units", "repair", 2)
static func _test_su_yak_patrol(t, catalog) -> void: _keyword(t, catalog, "su-yak-patrol", "Fury")
static func _test_su_pe2_bomber_wing(t, catalog) -> void: _ability(t, catalog, "su-pe2-bomber-wing", "deploy", "enemy_hq", "damage", 2)
static func _test_su_deep_battle(t, catalog) -> void: _ability(t, catalog, "su-deep-battle", "play_order", "enemy_unit", "retreat", -1)
static func _test_su_artillery_preparation(t, catalog) -> void: _ability(t, catalog, "su-artillery-preparation", "play_order", "enemy_units", "damage", 2)


static func _card(t, catalog, id: String, title: String, nation: String, category: String, unit_type: String, rarity: String, deployment_cost: int, operation_cost: int, attack: int, defense: int) -> void:
	var card: Dictionary = catalog.cards_by_id.get(id, {})
	t.assert_eq([card.get("title"), card.get("nation"), card.get("category"), card.get("unit_type"), card.get("rarity"), card.get("deployment_cost"), card.get("operation_cost"), card.get("attack"), card.get("defense")], [title, nation, category, unit_type, rarity, deployment_cost, operation_cost, attack, defense], "%s locked matrix" % id)


static func _keyword(t, catalog, id: String, keyword: String) -> void:
	var card: Dictionary = catalog.cards_by_id.get(id, {})
	t.assert_true((card.get("keywords", []) as Array).has(keyword), "%s has %s" % [id, keyword])


static func _ability(t, catalog, card_id: String, trigger: String, selector: String, effect_type: String, amount: int) -> void:
	var card: Dictionary = catalog.cards_by_id.get(card_id, {})
	var found := false
	for ability_id in card.get("ability_ids", []):
		var ability: Dictionary = catalog.abilities_by_id.get(ability_id, {})
		if ability.get("trigger") != trigger or ability.get("target", {}).get("selector") != selector:
			continue
		for effect in ability.get("effects", []):
			if effect.get("type") == effect_type and (amount < 0 or effect.get("amount", effect.get("count", effect.get("attack", -1))) == amount):
				found = true
	t.assert_true(found, "%s declares %s behavior" % [card_id, card_id])
