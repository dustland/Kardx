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
	var enemy := PlayerState.create("opponent", "SovietUnion", CardInstance.headquarters("su-hq", "opponent", "o-hq"), [])
	var state := MatchState.create(player, enemy, 1234)
	var enemy_view := state.snapshot_for("player")
	t.assert_true(not enemy_view.players.opponent.has("deck_order"), "enemy deck order hidden")
	enemy_view.players.player.hq_defense = 1
	t.assert_eq(state.players.player.headquarters.current_defense, 20, "snapshot cannot mutate state")
