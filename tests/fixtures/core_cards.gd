class_name CoreCards
extends RefCounted

const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")

static func build_valid_fixture() -> Dictionary:
	var definitions := {}
	definitions["us-hq"] = _definition("us-hq", "UnitedStates", "Headquarters", "Elite")
	definitions["su-hq"] = _definition("su-hq", "SovietUnion", "Headquarters", "Elite")
	for index in range(13):
		definitions["us-%02d" % index] = _definition("us-%02d" % index, "UnitedStates", "Unit", "Standard")
		definitions["su-%02d" % index] = _definition("su-%02d" % index, "SovietUnion", "Unit", "Standard")

	return {
		"definitions": definitions,
		"player_deck": _starter_deck("us"),
		"enemy_deck": _starter_deck("su"),
	}


static func scripted_full_match(seed: int) -> MatchController:
	var definitions := {
		"player-hq": _gameplay_definition("player-hq", "Headquarters"),
		"opponent-hq": _gameplay_definition("opponent-hq", "Headquarters"),
		"player-unit": _gameplay_definition("player-unit", "Unit", 20, 10),
		"player-counter": _gameplay_definition("player-counter", "Countermeasure", 0, 0, [
			{
				"id": "cancel-order",
				"trigger": "order_played",
				"conditions": {"enemy": true},
				"target": {"selector": "none"},
				"effects": [{"type": "replace_event", "changes": {"cancelled": true}}],
			},
		]),
		"player-order": _gameplay_definition("player-order", "Order"),
		"opponent-order": _gameplay_definition("opponent-order", "Order", 0, 0, [
			{
				"id": "bombard-hq",
				"trigger": "play_order",
				"conditions": {},
				"target": {"selector": "enemy_unit_or_hq", "count": 1},
				"effects": [{"type": "damage", "amount": 1}],
			},
		]),
	}
	var player_deck: Array[String] = []
	for index in range(20):
		player_deck.append("player-unit")
	for index in range(10):
		player_deck.append("player-counter")
	for index in range(9):
		player_deck.append("player-order")
	player_deck.append("player-hq")
	var opponent_deck: Array[String] = []
	for index in range(39):
		opponent_deck.append("opponent-order")
	opponent_deck.append("opponent-hq")
	var controller := MatchController.create(definitions, player_deck, opponent_deck, seed)
	_submit(controller, GameAction.create("start_match", "system", "", [], {}, controller.state.sequence))
	_submit(controller, GameAction.create("mulligan", "player", "", [], {}, controller.state.sequence))
	_submit(controller, GameAction.create("mulligan", "opponent", "", [], {}, controller.state.sequence))
	_submit(controller, GameAction.create("confirm_mulligan", "player", "", [], {}, controller.state.sequence))
	_submit(controller, GameAction.create("confirm_mulligan", "opponent", "", [], {}, controller.state.sequence))

	var counter_id := ""
	var counter_triggered := false
	var unit_id := ""
	for turn_limit in range(80):
		if controller.state.phase == "complete" or controller.state.phase == "invalid":
			break
		var active_player_id: String = controller.state.active_player_id
		if active_player_id == "player":
			if counter_id.is_empty():
				var counter = _first_hand_card(controller, "player", "Countermeasure")
				if counter != null:
					counter_id = counter.instance_id
					if not _submit(controller, GameAction.create("toggle_countermeasure", "player", counter_id, [], {}, controller.state.sequence)):
						break
					_submit(controller, GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
					continue
			if not counter_triggered:
				_submit(controller, GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
				continue
			if unit_id.is_empty():
				var unit = _first_hand_card(controller, "player", "Unit")
				if unit != null:
					unit_id = unit.instance_id
					if not _submit(controller, GameAction.create(
						"deploy_unit", "player", unit_id, [], {"support_slot": 0}, controller.state.sequence
					)):
						break
				_submit(controller, GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
				continue
			var unit_card = _find_card(controller, unit_id)
			if unit_card == null:
				break
			if unit_card.zone == "support_line":
				if not _submit(controller, GameAction.create(
					"move_unit", "player", unit_id, [], {"zone": "frontline", "slot": 0}, controller.state.sequence
				)):
					break
				_submit(controller, GameAction.create("end_turn", "player", "", [], {}, controller.state.sequence))
				continue
			if unit_card.zone == "frontline":
				_submit(controller, GameAction.create(
					"attack_hq", "player", unit_id, [], {"target_player_id": "opponent"}, controller.state.sequence
				))
				continue
			break
		if not counter_id.is_empty() and not counter_triggered:
			var order = _first_hand_card(controller, "opponent", "Order")
			if order == null:
				break
			var order_result = controller.submit_action(GameAction.create(
				"play_order",
				"opponent",
				order.instance_id,
				[controller.state.players.player.headquarters.instance_id],
				{},
				controller.state.sequence
			))
			if not order_result.accepted:
				break
			counter_triggered = _has_event(order_result.events, "countermeasure_triggered")
		if controller.state.phase != "complete" and controller.state.phase != "invalid":
			_submit(controller, GameAction.create("end_turn", "opponent", "", [], {}, controller.state.sequence))
	return controller

static func _starter_deck(nation_prefix: String) -> Array[String]:
	var deck: Array[String] = []
	for index in range(9):
		for copy_index in range(4):
			deck.append("%s-%02d" % [nation_prefix, index])
	for copy_index in range(3):
		deck.append("%s-09" % nation_prefix)
	deck.append("%s-hq" % nation_prefix)
	return deck

static func _definition(id: String, nation: String, category: String, rarity: String) -> Dictionary:
	return {
		"id": id,
		"title": id,
		"nation": nation,
		"category": category,
		"rarity": rarity,
		"unit_type": "Infantry",
		"attack": 1,
		"defense": 1,
	}


static func _gameplay_definition(id: String, category: String, attack: int = 0, defense: int = 0, abilities: Array = []) -> Dictionary:
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
		"abilities": abilities.duplicate(true),
	}


static func _submit(controller: MatchController, action: GameAction) -> bool:
	return controller.submit_action(action).accepted


static func _first_hand_card(controller: MatchController, player_id: String, category: String):
	for card in controller.state.players[player_id].hand:
		if card.category == category:
			return card
	return null


static func _find_card(controller: MatchController, instance_id: String):
	for player_id in controller.state.players:
		var player = controller.state.players[player_id]
		if player.headquarters.instance_id == instance_id:
			return player.headquarters
		for collection in [player.deck, player.hand, player.support_line, player.discard]:
			for card in collection:
				if card != null and card.instance_id == instance_id:
					return card
	for card in controller.state.frontline:
		if card != null and card.instance_id == instance_id:
			return card
	return null


static func _has_event(events: Array, event_type: String) -> bool:
	for event in events:
		if str(event.type) == event_type:
			return true
	return false
