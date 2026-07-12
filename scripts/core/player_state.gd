class_name PlayerState
extends RefCounted

const GameConstants = preload("res://scripts/core/game_constants.gd")
const CardInstance = preload("res://scripts/core/card_instance.gd")

var id: String
var nation: String
var headquarters: CardInstance
var deck: Array
var hand: Array = []
var support_line: Array = []
var discard: Array = []
var active_countermeasures: Array = []
var credit_slots: int = 0
var credit: int = 0
var fatigue: int = 1
var turns_started: int = 0
var mulligan_used: bool = false
var mulligan_confirmed: bool = false
var max_hand_size: int = GameConstants.MAX_HAND_SIZE

static func create(player_id: String, player_nation: String, hq: CardInstance, player_deck: Array) -> PlayerState:
	var player: PlayerState = load("res://scripts/core/player_state.gd").new()
	player.id = player_id
	player.nation = player_nation
	player.headquarters = hq
	player.deck = player_deck.duplicate()
	player.support_line.resize(GameConstants.SUPPORT_UNIT_SLOTS)
	return player

func reset_operations() -> void:
	for card in support_line:
		if card != null:
			card.operations_used = 0

func deactivate_countermeasures() -> void:
	for card in active_countermeasures:
		if card != null:
			card.countermeasure_active = false
	active_countermeasures.clear()

func to_public_dict(reveal_hand: bool, reveal_deck_order: bool, viewer_id: String = "") -> Dictionary:
	var public_hand: Array = []
	for card in hand:
		public_hand.append(card.to_public_dict(reveal_hand or bool(card.revealed_to.get(viewer_id, false))) )
	var public_support_line: Array = []
	for card in support_line:
		public_support_line.append(card.to_public_dict(true) if card != null else null)
	var public_discard: Array = []
	for card in discard:
		public_discard.append(card.to_public_dict(true))
	var snapshot := {
		"id": id,
		"nation": nation,
		"headquarters": headquarters.to_public_dict(true),
		"hq_defense": headquarters.current_defense,
		"deck_count": deck.size(),
		"hand": public_hand,
		"support_line": public_support_line,
		"discard": public_discard,
		"credit_slots": credit_slots,
		"credit": credit,
		"fatigue": fatigue,
		"mulligan_used": mulligan_used,
		"mulligan_confirmed": mulligan_confirmed,
	}
	if reveal_deck_order:
		var deck_order: Array = []
		for card in deck:
			deck_order.append(card.to_public_dict(true))
		snapshot["deck_order"] = deck_order
	return snapshot
