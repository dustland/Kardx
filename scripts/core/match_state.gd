class_name MatchState
extends RefCounted

const GameConstants = preload("res://scripts/core/game_constants.gd")
const PlayerState = preload("res://scripts/core/player_state.gd")

var players: Dictionary
var active_player_id: String = ""
var starting_player_id: String = ""
var turn: int = 0
var frontline: Array = []
var frontline_controller_id: String = ""
var winner_id: String = ""
var seed: int
var rng_state: int
var sequence: int = 0
var phase: String = "setup"

static func create(player: PlayerState, opponent: PlayerState, match_seed: int) -> MatchState:
	var state: MatchState = load("res://scripts/core/match_state.gd").new()
	state.players = {player.id: player, opponent.id: opponent}
	state.seed = match_seed
	state.rng_state = match_seed
	state.frontline.resize(GameConstants.FRONTLINE_SLOTS)
	return state

func snapshot_for(viewer_id: String) -> Dictionary:
	var public_players: Dictionary = {}
	for id in players:
		var player_id := str(id)
		var player: PlayerState = players[player_id]
		var is_viewer: bool = player_id == viewer_id
		public_players[player_id] = player.to_public_dict(is_viewer, is_viewer)
	var public_frontline: Array = []
	for card in frontline:
		public_frontline.append(card.to_public_dict(true) if card != null else null)
	return {
		"players": public_players,
		"active_player_id": active_player_id,
		"starting_player_id": starting_player_id,
		"turn": turn,
		"frontline": public_frontline,
		"frontline_controller_id": frontline_controller_id,
		"winner_id": winner_id,
		"seed": seed,
		"sequence": sequence,
		"phase": phase,
	}
