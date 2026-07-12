extends SceneTree

const AIPlayer = preload("res://scripts/ai/ai_player.gd")
const ContentCatalog = preload("res://scripts/content/content_catalog.gd")
const GameAction = preload("res://scripts/core/game_action.gd")
const MatchController = preload("res://scripts/core/match_controller.gd")

func _initialize() -> void:
	var catalog = ContentCatalog.load_from_paths("res://data/cards.json", "res://data/abilities.json", "res://data/decks.json", "res://data/rules.json")
	var player_deck: Array = catalog.decks_by_id["us-starter"].cards
	var opponent_deck: Array = catalog.decks_by_id["su-starter"].cards
	var controller = MatchController.create(catalog.cards_by_id, player_deck, opponent_deck, 50305)
	for action in [GameAction.create("start_match", "system"), GameAction.create("mulligan", "player"), GameAction.create("mulligan", "opponent"), GameAction.create("confirm_mulligan", "player"), GameAction.create("confirm_mulligan", "opponent")]:
		action.expected_sequence = controller.state.sequence
		if not controller.submit_action(action).accepted: return _fail("setup rejected")
	var ai = AIPlayer.create("easy", 50305)
	var player_classes := {}
	var steps := 0
	while controller.state.phase == "action" and steps < 220:
		if not _validate_public_ownership(controller.state.snapshot_for("player")):
			return _fail("perspective snapshot ownership contract failed")
		var actor: String = controller.state.active_player_id
		var action = _choose_player_action(controller, player_classes) if actor == "player" else ai.choose_action(controller, "opponent")
		if action == null or action.type.is_empty():
			action = _end_turn(controller, actor)
		if action == null: return _fail("no legal action for %s" % actor)
		var result = controller.submit_action(action)
		if not result.accepted: return _fail("%s rejected: %s" % [action.type, result.reason_code])
		if actor == "player" and action.type != "end_turn": player_classes[action.type] = true
		steps += 1
	var required := ["deploy_unit", "move_unit", "play_order", "toggle_countermeasure"]
	for action_type in required:
		if not player_classes.has(action_type): return _fail("fixture did not exercise %s" % action_type)
	if not player_classes.has("attack_unit") and not player_classes.has("attack_hq"):
		return _fail("fixture did not exercise attack")
	print("Task5 scripted player-vs-AI passed: %s" % [player_classes.keys()])
	quit(0)

func _validate_public_ownership(snapshot: Dictionary) -> bool:
	for card in snapshot.get("frontline", []):
		if card is Dictionary and str(card.get("owner_id", "")) not in ["player", "opponent"]: return false
	for player_id in ["player", "opponent"]:
		var player: Dictionary = snapshot.get("players", {}).get(player_id, {})
		var hq: Dictionary = player.get("headquarters", {})
		if str(hq.get("owner_id", "")) != player_id: return false
		for card in player.get("support_line", []):
			if card is Dictionary and str(card.get("owner_id", "")) != player_id: return false
	for hidden_card in snapshot.get("players", {}).get("opponent", {}).get("hand", []):
		if hidden_card is Dictionary and hidden_card.has("owner_id"): return false
	return true

func _choose_player_action(controller, seen: Dictionary):
	var actions: Array = controller.legal_actions("player")
	for desired in ["toggle_countermeasure", "deploy_unit", "move_unit", "play_order", "attack_unit", "attack_hq"]:
		if not seen.has(desired):
			for action in actions:
				if action.type == desired: return action
	for action in actions:
		if action.type in ["attack_unit", "attack_hq", "play_order", "deploy_unit", "move_unit", "toggle_countermeasure"]: return action
	return _end_turn(controller, "player")

func _end_turn(controller, actor: String):
	for action in controller.legal_actions(actor):
		if action.type == "end_turn": return action
	return null

func _fail(message: String) -> void:
	push_error(message)
	quit(1)
