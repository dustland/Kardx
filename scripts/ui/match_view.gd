class_name MatchView
extends Control

signal action_requested(action: GameAction)

const ActionBuilderScript = preload("res://scripts/ui/action_builder.gd")
const CardViewScene = preload("res://scenes/ui/card_view.tscn")

var router: Main
var model := MatchInteractionModel.new()
var snapshot: Dictionary = {}
var _input_locked := false

class MatchInteractionModel:
	var selected_source_id := ""
	var status_message := ""
	var rejection_code := ""
	var _legal_actions: Array = []
	func set_legal_actions(actions: Array) -> void:
		_legal_actions = actions.duplicate()
	func select_source(instance_id: String) -> void:
		selected_source_id = instance_id
		status_message = "Choose a highlighted target" if not _source_actions().is_empty() else "No legal action for this card"
	func highlighted_targets() -> Array[String]:
		var result: Array[String] = []
		for action in _source_actions():
			for target_id in action.target_ids:
				if target_id not in result: result.append(target_id)
		result.sort()
		return result
	func highlighted_slots(zone: String) -> Array[int]:
		var result: Array[int] = []
		for action in _source_actions():
			var action_zone := "support" if action.type == "deploy_unit" else str(action.payload.get("zone", ""))
			var slot := int(action.payload.get("support_slot", action.payload.get("slot", -1)))
			if action_zone == zone and slot >= 0 and slot not in result: result.append(slot)
		result.sort()
		return result
	func choose_target(target_id: String):
		for action in _source_actions():
			if target_id in action.target_ids:
				cancel()
				return action
		return null
	func choose_slot(zone: String, slot: int):
		for action in _source_actions():
			var action_zone := "support" if action.type == "deploy_unit" else str(action.payload.get("zone", ""))
			var action_slot := int(action.payload.get("support_slot", action.payload.get("slot", -1)))
			if action_zone == zone and action_slot == slot:
				cancel()
				return action
		return null
	func immediate_action():
		for action in _source_actions():
			if action.type == "toggle_countermeasure" or (action.type == "play_order" and action.target_ids.is_empty()):
				cancel()
				return action
		return null
	func cancel() -> void:
		selected_source_id = ""
	func apply_rejection(code: String, message: String) -> void:
		rejection_code = code
		status_message = message
	func _source_actions() -> Array:
		return _legal_actions.filter(func(action) -> bool: return action.source_id == selected_source_id)

func initialize(main: Main, payload: Dictionary) -> void:
	router = main
	render_events(payload.get("events", []))
	render_snapshot(payload.get("snapshot", {}))

func render_snapshot(next_snapshot: Dictionary) -> void:
	snapshot = next_snapshot.duplicate(true)
	var players: Dictionary = snapshot.get("players", {})
	var player: Dictionary = players.get("player", {})
	var opponent: Dictionary = players.get("opponent", {})
	%TurnLabel.text = "Turn %d  |  %s" % [int(snapshot.get("turn", 0)), str(snapshot.get("phase", "")).capitalize()]
	%OpponentLabel.text = "%s  HQ %d  Hand %d  Deck %d" % [str(opponent.get("nation", "Opponent")), int(opponent.get("hq_defense", 0)), (opponent.get("hand", []) as Array).size(), int(opponent.get("deck_count", 0))]
	%PlayerLabel.text = "%s  HQ %d" % [str(player.get("nation", "Player")), int(player.get("hq_defense", 0))]
	%CreditLabel.text = "Credit %d / %d" % [int(player.get("credit", 0)), int(player.get("credit_slots", 0))]
	%OpponentSupport.render(opponent.get("support_line", []))
	%Frontline.render(snapshot.get("frontline", []))
	%PlayerSupport.render(player.get("support_line", []))
	_render_hand(player.get("hand", []))
	%EndTurnButton.disabled = _input_locked or snapshot.get("active_player_id") != "player" or snapshot.get("phase") != "action"
	_refresh_highlights()

func render_events(events: Array) -> void:
	%Timeline.render_events(events)

func set_legal_actions(actions: Array) -> void:
	model.set_legal_actions(actions)

func set_input_locked(locked: bool) -> void:
	_input_locked = locked
	%EndTurnButton.disabled = locked or snapshot.get("active_player_id") != "player"

func show_rejection(code: String, message: String) -> void:
	model.apply_rejection(code, message)
	%StatusLabel.text = message

func _render_hand(cards: Array) -> void:
	for child in %PlayerHand.get_children(): child.free()
	for card_data in cards:
		if not (card_data is Dictionary): continue
		var card = CardViewScene.instantiate()
		%PlayerHand.add_child(card)
		card.bind(card_data, "hand")
		card.card_pressed.connect(_on_card_pressed)

func _on_card_pressed(instance_id: String) -> void:
	if _input_locked: return
	model.select_source(instance_id)
	var action = model.immediate_action()
	if action != null: action_requested.emit(action)
	%StatusLabel.text = model.status_message
	_refresh_highlights()

func _on_board_card_pressed(instance_id: String) -> void:
	if _input_locked: return
	var action = model.choose_target(instance_id) if not model.selected_source_id.is_empty() else null
	if action != null:
		action_requested.emit(action)
	else:
		model.select_source(instance_id)
	_refresh_highlights()

func _on_slot_pressed(zone: String, slot: int) -> void:
	var action = model.choose_slot(zone, slot)
	if action != null: action_requested.emit(action)
	_refresh_highlights()

func _on_card_dropped(instance_id: String, zone: String, slot: int) -> void:
	model.select_source(instance_id)
	_on_slot_pressed(zone, slot)

func _on_target_dropped(source_id: String, target_id: String) -> void:
	model.select_source(source_id)
	var action = model.choose_target(target_id)
	if action != null: action_requested.emit(action)
	_refresh_highlights()

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		model.cancel()
		%StatusLabel.text = ""
		_refresh_highlights()

func _on_end_turn_pressed() -> void:
	for action in model._legal_actions:
		if action.type == "end_turn": action_requested.emit(action); return

func _unhandled_key_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		model.cancel()
		%StatusLabel.text = ""
		_refresh_highlights()

func _refresh_highlights() -> void:
	var targets := model.highlighted_targets()
	%OpponentSupport.set_highlights([], targets)
	%Frontline.set_highlights(model.highlighted_slots("frontline"), targets)
	%PlayerSupport.set_highlights(model.highlighted_slots("support"), targets)
