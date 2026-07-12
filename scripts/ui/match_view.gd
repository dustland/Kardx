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
	var selected_targets: Array[String] = []
	var selected_zone := ""
	var selected_slot := -1
	var _legal_actions: Array = []
	func set_legal_actions(actions: Array) -> void:
		_legal_actions = actions.duplicate()
	func select_source(instance_id: String) -> void:
		selected_source_id = instance_id
		selected_targets.clear()
		selected_zone = ""
		selected_slot = -1
		status_message = "Choose a highlighted target" if not _source_actions().is_empty() else "No legal action for this card"
	func highlighted_targets() -> Array[String]:
		var result: Array[String] = []
		for action in _candidate_actions():
			if action.target_ids.size() > selected_targets.size():
				var target_id: String = action.target_ids[selected_targets.size()]
				if target_id not in result: result.append(target_id)
		result.sort()
		return result
	func highlighted_slots(zone: String) -> Array[int]:
		var result: Array[int] = []
		for action in _candidate_actions():
			var action_zone := "support" if action.type == "deploy_unit" else str(action.payload.get("zone", ""))
			var slot := int(action.payload.get("support_slot", action.payload.get("slot", -1)))
			if action_zone == zone and slot >= 0 and slot not in result: result.append(slot)
		result.sort()
		return result
	func choose_target(target_id: String):
		if target_id not in highlighted_targets(): return null
		selected_targets.append(target_id)
		return _take_immediate_if_complete()
	func choose_slot(zone: String, slot: int):
		if slot not in highlighted_slots(zone): return null
		selected_zone = zone
		selected_slot = slot
		return _take_immediate_if_complete()
	func immediate_action():
		for action in _candidate_actions():
			if action.type == "toggle_countermeasure":
				cancel()
				return action
		return null
	func confirm_action():
		if _has_unspecified_dimension(): return null
		var complete := _complete_actions()
		if complete.size() != 1: return null
		var action = complete[0]
		if action.type not in ["play_order", "deploy_unit"] and action.target_ids.size() < 2: return null
		cancel()
		return action
	func can_confirm() -> bool:
		return not _has_unspecified_dimension() and _complete_actions().size() == 1 and (_complete_actions()[0].type in ["play_order", "deploy_unit"] or _complete_actions()[0].target_ids.size() > 1)
	func _take_immediate_if_complete():
		if _has_unspecified_dimension(): return null
		var complete := _complete_actions()
		if complete.size() != 1: return null
		var action = complete[0]
		if action.type in ["play_order", "deploy_unit"] or action.target_ids.size() > 1: return null
		cancel()
		return action
	func _complete_actions() -> Array:
		return _candidate_actions().filter(func(action) -> bool:
			return action.target_ids.size() == selected_targets.size() and (not _requires_slot(action) or selected_slot >= 0)
		)
	func _candidate_actions() -> Array:
		return _source_actions().filter(func(action) -> bool:
			if selected_slot >= 0:
				var action_zone := _action_zone(action)
				var action_slot := _action_slot(action)
				if action_zone != selected_zone or action_slot != selected_slot: return false
			if selected_targets.size() > action.target_ids.size(): return false
			for index in range(selected_targets.size()):
				if action.target_ids[index] != selected_targets[index]: return false
			return true
		)
	func _requires_slot(action) -> bool:
		return action.type in ["deploy_unit", "move_unit"]
	func _has_unspecified_dimension() -> bool:
		if not highlighted_targets().is_empty(): return true
		if selected_slot < 0:
			for zone in ["support", "frontline"]:
				if not highlighted_slots(zone).is_empty(): return true
		return false
	func _action_zone(action) -> String:
		return "support" if action.type == "deploy_unit" else str(action.payload.get("zone", ""))
	func _action_slot(action) -> int:
		return int(action.payload.get("support_slot", action.payload.get("slot", -1)))
	func cancel() -> void:
		selected_source_id = ""
		selected_targets.clear()
		selected_zone = ""
		selected_slot = -1
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
	_sanitize_hidden_opponent_hand()
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
	%OpponentHQ.bind(opponent.get("headquarters", {}), "battlefield")
	%PlayerHQ.bind(player.get("headquarters", {}), "battlefield")
	_render_hand(player.get("hand", []))
	%EndTurnButton.disabled = _input_locked or snapshot.get("active_player_id") != "player" or snapshot.get("phase") != "action"
	_refresh_highlights()


func _sanitize_hidden_opponent_hand() -> void:
	var opponent: Dictionary = snapshot.get("players", {}).get("opponent", {})
	var hand: Array = opponent.get("hand", [])
	for index in range(hand.size()):
		if hand[index] is Dictionary and bool((hand[index] as Dictionary).get("hidden", false)):
			hand[index] = {"hidden": true}

func render_events(events: Array) -> void:
	%Timeline.render_events(events)

func set_legal_actions(actions: Array) -> void:
	model.set_legal_actions(actions)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		_on_cancel_pressed()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("end_turn"):
		_on_end_turn_pressed()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("ui_accept") and model.can_confirm():
		_on_confirm_pressed()
		get_viewport().set_input_as_handled()

func set_input_locked(locked: bool) -> void:
	_input_locked = locked
	%EndTurnButton.disabled = locked or snapshot.get("active_player_id") != "player"
	%ConfirmButton.disabled = locked or not model.can_confirm()
	%CancelButton.disabled = locked or model.selected_source_id.is_empty()
	%OpponentHQ.disabled = locked
	%PlayerHQ.disabled = locked
	%OpponentSupport.set_input_locked(locked)
	%Frontline.set_input_locked(locked)
	%PlayerSupport.set_input_locked(locked)
	for card in %PlayerHand.get_children(): card.disabled = locked

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
		card.disabled = _input_locked
		card.card_pressed.connect(_on_card_pressed)

func _on_card_pressed(instance_id: String) -> void:
	if _reject_locked(): return
	model.select_source(instance_id)
	var action = model.immediate_action()
	if action != null: action_requested.emit(action)
	%StatusLabel.text = model.status_message
	_refresh_highlights()

func _on_board_card_pressed(instance_id: String) -> void:
	if _reject_locked(): return
	if model.selected_source_id.is_empty():
		model.select_source(instance_id)
	else:
		var action = model.choose_target(instance_id)
		if action != null:
			action_requested.emit(action)
	_refresh_highlights()

func _on_slot_pressed(zone: String, slot: int) -> void:
	if _reject_locked(): return
	var action = model.choose_slot(zone, slot)
	if action != null: action_requested.emit(action)
	_refresh_highlights()

func _on_card_dropped(instance_id: String, zone: String, slot: int) -> void:
	if _reject_locked(): return
	model.select_source(instance_id)
	_on_slot_pressed(zone, slot)

func _on_target_dropped(source_id: String, target_id: String) -> void:
	if _reject_locked(): return
	model.select_source(source_id)
	var action = model.choose_target(target_id)
	if action != null: action_requested.emit(action)
	_refresh_highlights()

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		if _reject_locked(): return
		model.cancel()
		%StatusLabel.text = ""
		_refresh_highlights()

func _on_end_turn_pressed() -> void:
	if _reject_locked(): return
	for action in model._legal_actions:
		if action.type == "end_turn": action_requested.emit(action); return

func _on_confirm_pressed() -> void:
	if _reject_locked(): return
	var action = model.confirm_action()
	if action != null: action_requested.emit(action)
	_refresh_highlights()

func _on_cancel_pressed() -> void:
	if _reject_locked(): return
	model.cancel()
	%StatusLabel.text = ""
	_refresh_highlights()

func _reject_locked() -> bool:
	if not _input_locked: return false
	show_rejection("input_locked", "Wait for the opponent action to finish.")
	return true

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
	%OpponentHQ.modulate = Color("f2d66d") if str(%OpponentHQ.card_data.get("instance_id", "")) in targets else Color("e8b9b9")
	%PlayerHQ.modulate = Color("f2d66d") if str(%PlayerHQ.card_data.get("instance_id", "")) in targets else Color("b9d8e8")
	%ConfirmButton.disabled = _input_locked or not model.can_confirm()
	%CancelButton.disabled = _input_locked or model.selected_source_id.is_empty()
