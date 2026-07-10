extends RefCounted

const CardInstance = preload("res://scripts/core/card_instance.gd")
const PlayerState = preload("res://scripts/core/player_state.gd")
const MatchState = preload("res://scripts/core/match_state.gd")

static func run(t) -> void:
	var unit := CardInstance.from_definition({
		"id": "us-rifle", "title": "Rifle Platoon", "category": "Unit",
		"unit_type": "Infantry", "attack": 2, "defense": 3,
		"deployment_cost": 2, "operation_cost": 1, "keywords": []
	}, "player", "p-1")
	t.assert_eq(unit.current_defense, 3, "runtime defense starts at base")
	var hq := CardInstance.headquarters("us-hq", "player", "p-hq")
	var player := PlayerState.create("player", "UnitedStates", hq, [unit])
	var nested_keyword_card := CardInstance.from_definition({
		"id": "us-engineer", "title": "Engineer Platoon", "category": "Unit",
		"unit_type": "Infantry", "attack": 1, "defense": 2,
		"keywords": [{"name": "fortify", "effect": {"defense": 2}}]
	}, "player", "p-2")
	player.hand = [nested_keyword_card]
	var hidden_enemy_card := CardInstance.from_definition({
		"id": "su-scout", "title": "Scout Platoon", "category": "Unit",
		"unit_type": "Infantry", "attack": 1, "defense": 2,
	}, "opponent", "o-1")
	hidden_enemy_card.zone = "hand"
	var enemy := PlayerState.create("opponent", "SovietUnion", CardInstance.headquarters("su-hq", "opponent", "o-hq"), [])
	enemy.hand = [hidden_enemy_card]
	var state := MatchState.create(player, enemy, 1234)
	var enemy_view := state.snapshot_for("player")
	t.assert_true(not enemy_view.players.opponent.has("deck_order"), "enemy deck order hidden")
	t.assert_true(not enemy_view.players.opponent.hand[0].has("instance_id"), "hidden opponent hand card has no stable instance ID")
	t.assert_eq(enemy_view.players.opponent.hand[0], {"hidden": true, "zone": "hand"}, "hidden opponent hand card exposes only opaque UI data")
	var owner_view := state.snapshot_for("opponent")
	t.assert_eq(owner_view.players.opponent.hand[0].instance_id, "o-1", "owner snapshot preserves known card identity")
	enemy_view.players.player.hq_defense = 1
	t.assert_eq(state.players.player.headquarters.current_defense, 20, "snapshot cannot mutate state")
	enemy_view.players.player.hand[0].keywords[0].effect.defense = 99
	t.assert_eq(nested_keyword_card.keywords[0].effect.defense, 2, "nested keyword snapshot cannot mutate state")
