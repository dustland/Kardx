class_name BoardEvaluator
extends RefCounted


static func score(snapshot: Dictionary, actor_id: String) -> float:
	var players: Dictionary = snapshot.get("players", {})
	if not players.has(actor_id):
		return 0.0
	var opponent_id := _other_id(players, actor_id)
	if opponent_id.is_empty():
		return 0.0
	var me: Dictionary = players[actor_id]
	var them: Dictionary = players[opponent_id]
	return float(int(me.get("hq_defense", 0)) - int(them.get("hq_defense", 0))) * 8.0 \
		+ _unit_value(snapshot, actor_id) - _unit_value(snapshot, opponent_id) \
		+ float(_hand_count(me) - _hand_count(them)) * 1.5 \
		+ _frontline_value(snapshot, actor_id) \
		+ float(int(me.get("credit", 0)) - int(them.get("credit", 0))) * 0.25


static func _other_id(players: Dictionary, actor_id: String) -> String:
	for player_id in players:
		if str(player_id) != actor_id:
			return str(player_id)
	return ""


static func _hand_count(player: Dictionary) -> int:
	if player.has("hand_count"):
		return int(player.hand_count)
	var hand: Array = player.get("hand", [])
	return hand.size()


static func _unit_value(snapshot: Dictionary, player_id: String) -> float:
	var player: Dictionary = snapshot.players[player_id]
	var value := _cards_value(player.get("support_line", []))
	if str(snapshot.get("frontline_controller_id", "")) == player_id:
		value += _cards_value(snapshot.get("frontline", []))
	return value


static func _cards_value(cards: Array) -> float:
	var value := 0.0
	for card_value in cards:
		if not (card_value is Dictionary):
			continue
		var card: Dictionary = card_value
		if str(card.get("category", "")) == "Unit":
			value += float(int(card.get("attack", 0)) + int(card.get("defense", 0)))
	return value


static func _frontline_value(snapshot: Dictionary, actor_id: String) -> float:
	var controller_id := str(snapshot.get("frontline_controller_id", ""))
	if controller_id.is_empty():
		return 0.0
	return 10.0 if controller_id == actor_id else -10.0
