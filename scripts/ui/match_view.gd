class_name MatchView
extends Control

signal action_requested(action: GameAction)

const ActionBuilderScript = preload("res://scripts/ui/action_builder.gd")
const CardViewScene = preload("res://scenes/ui/card_view.tscn")
const MatchCoachModelScript = preload("res://scripts/ui/match_coach_model.gd")

var router: Main
var model := MatchInteractionModel.new()
var snapshot: Dictionary = {}
var _input_locked := false
var _onboarding_state: Dictionary = {}
var _coach_state: Dictionary = {}
var _rejection_message := ""

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


func _ready() -> void:
	%OpponentHQ.card_pressed.connect(_on_board_card_pressed)
	%OpponentHQ.card_dropped.connect(_on_target_dropped)
	%OpponentSupport.card_pressed.connect(_on_board_card_pressed)
	%OpponentSupport.target_dropped.connect(_on_target_dropped)
	%Frontline.card_pressed.connect(_on_board_card_pressed)
	%Frontline.slot_pressed.connect(_on_slot_pressed)
	%Frontline.card_dropped.connect(_on_card_dropped)
	%Frontline.target_dropped.connect(_on_target_dropped)
	%PlayerHQ.card_pressed.connect(_on_board_card_pressed)
	%PlayerHQ.card_dropped.connect(_on_target_dropped)
	%PlayerSupport.card_pressed.connect(_on_board_card_pressed)
	%PlayerSupport.slot_pressed.connect(_on_slot_pressed)
	%PlayerSupport.card_dropped.connect(_on_card_dropped)
	%PlayerSupport.target_dropped.connect(_on_target_dropped)
	%CancelButton.pressed.connect(_on_cancel_pressed)
	%ConfirmButton.pressed.connect(_on_confirm_pressed)
	%EndTurnButton.pressed.connect(_on_end_turn_pressed)
	resized.connect(_apply_responsive_layout)
	_apply_responsive_layout()


func _apply_responsive_layout() -> void:
	if not is_node_ready():
		return
	var compact := size.x <= 1000.0
	%TimelinePanel.custom_minimum_size.x = 136.0 if compact else 148.0
	%HandScroll.custom_minimum_size.y = 164.0 if compact else 170.0
	var row_height := 112.0 if compact else 118.0
	for path in ["Margin/Columns/Board/OpponentArea", "Margin/Columns/Board/Frontline", "Margin/Columns/Board/PlayerArea"]:
		(get_node(path) as Control).custom_minimum_size.y = row_height
	var margin := get_node("Margin") as MarginContainer
	margin.offset_left = 6.0 if compact else 8.0
	margin.offset_right = -6.0 if compact else -8.0

func initialize(main: Main, payload: Dictionary) -> void:
	router = main
	_onboarding_state = payload.get("onboarding", {}).duplicate(true)
	render_events(payload.get("events", []))
	render_snapshot(payload.get("snapshot", {}))

func render_snapshot(next_snapshot: Dictionary) -> void:
	var previous_state_key := _snapshot_state_key(snapshot)
	var next_state_key := _snapshot_state_key(next_snapshot)
	if not _rejection_message.is_empty() and not previous_state_key.is_empty() and next_state_key != previous_state_key:
		_clear_rejection()
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
	_refresh_coach()


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
	_refresh_coach()


func set_onboarding_state(state: Dictionary) -> void:
	_onboarding_state = state.duplicate(true)
	_refresh_coach()


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
	_refresh_coach()

func show_rejection(code: String, message: String) -> void:
	model.apply_rejection(code, message)
	_rejection_message = message
	%StatusLabel.text = message
	_refresh_coach()

func _render_hand(cards: Array) -> void:
	for child in %PlayerHand.get_children(): child.free()
	for card_data in cards:
		if not (card_data is Dictionary): continue
		var card = CardViewScene.instantiate()
		%PlayerHand.add_child(card)
		card.bind(card_data, "hand")
		card.disabled = _input_locked
		card.card_pressed.connect(_on_card_pressed)
	_apply_hand_states()

func _on_card_pressed(instance_id: String) -> void:
	if _reject_locked(): return
	_clear_rejection()
	var reason := str(_coach_state.get("source_reasons", {}).get(instance_id, ""))
	if not reason.is_empty():
		model.cancel()
		model.status_message = reason
		%StatusLabel.text = reason
		_refresh_coach()
		return
	model.select_source(instance_id)
	var action = model.immediate_action()
	if action != null: action_requested.emit(action)
	%StatusLabel.text = ""
	_refresh_coach()

func _on_board_card_pressed(instance_id: String) -> void:
	if _reject_locked(): return
	_clear_rejection()
	if model.selected_source_id.is_empty():
		model.select_source(instance_id)
	else:
		var action = model.choose_target(instance_id)
		if action != null:
			action_requested.emit(action)
	_refresh_coach()

func _on_slot_pressed(zone: String, slot: int) -> void:
	if _reject_locked(): return
	_clear_rejection()
	var action = model.choose_slot(zone, slot)
	if action != null: action_requested.emit(action)
	_refresh_coach()

func _on_card_dropped(instance_id: String, zone: String, slot: int) -> void:
	if _reject_locked(): return
	model.select_source(instance_id)
	_on_slot_pressed(zone, slot)

func _on_target_dropped(source_id: String, target_id: String) -> void:
	if _reject_locked(): return
	_clear_rejection()
	model.select_source(source_id)
	var action = model.choose_target(target_id)
	if action != null: action_requested.emit(action)
	_refresh_coach()

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
		if _reject_locked(): return
		model.cancel()
		_clear_rejection()
		%StatusLabel.text = ""
		_refresh_coach()

func _on_end_turn_pressed() -> void:
	if _reject_locked(): return
	for action in model._legal_actions:
		if action.type == "end_turn": action_requested.emit(action); return

func _on_confirm_pressed() -> void:
	if _reject_locked(): return
	var action = model.confirm_action()
	if action != null: action_requested.emit(action)
	_refresh_coach()

func _on_cancel_pressed() -> void:
	if _reject_locked(): return
	model.cancel()
	_clear_rejection()
	%StatusLabel.text = ""
	_refresh_coach()

func _reject_locked() -> bool:
	if not _input_locked: return false
	show_rejection("input_locked", "Wait for the opponent action to finish.")
	return true

func _unhandled_key_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		model.cancel()
		_clear_rejection()
		%StatusLabel.text = ""
		_refresh_coach()

func _refresh_highlights() -> void:
	var targets := model.highlighted_targets()
	%OpponentSupport.set_highlights([], targets)
	%Frontline.set_highlights(model.highlighted_slots("frontline"), targets)
	%PlayerSupport.set_highlights(model.highlighted_slots("support"), targets)
	%OpponentHQ.modulate = Color("f2d66d") if str(%OpponentHQ.card_data.get("instance_id", "")) in targets else Color("e8b9b9")
	%PlayerHQ.modulate = Color("f2d66d") if str(%PlayerHQ.card_data.get("instance_id", "")) in targets else Color("b9d8e8")
	%ConfirmButton.disabled = _input_locked or not model.can_confirm()
	%CancelButton.disabled = _input_locked or model.selected_source_id.is_empty()
	_apply_hand_states()
	_refresh_end_turn_state()
	_refresh_coach_objective()


func _refresh_coach() -> void:
	_coach_state = MatchCoachModelScript.derive(snapshot, model._legal_actions, {
		"selected_source_id": model.selected_source_id,
		"selected_targets": model.selected_targets,
		"selected_zone": model.selected_zone,
		"selected_slot": model.selected_slot,
	}, _onboarding_state)
	_refresh_highlights()


func _refresh_end_turn_state() -> void:
	var end_actions := model._legal_actions.filter(func(action) -> bool: return action.type == "end_turn")
	var can_end: bool = not end_actions.is_empty() and str(snapshot.get("active_player_id", "")) == "player" and str(snapshot.get("phase", "")) == "action"
	%EndTurnButton.disabled = _input_locked or not can_end
	%EndTurnButton.remove_theme_stylebox_override("normal")
	if not can_end:
		%EndTurnButton.set_meta("action_state", "disabled")
	elif bool(_coach_state.get("end_turn_only", false)):
		%EndTurnButton.set_meta("action_state", "strong")
		var style := StyleBoxFlat.new()
		style.bg_color = Color("2b2d24")
		style.border_color = Color("f0cf55")
		style.set_border_width_all(3)
		style.set_corner_radius_all(4)
		%EndTurnButton.add_theme_stylebox_override("normal", style)
	else:
		%EndTurnButton.set_meta("action_state", "normal")


func _refresh_coach_objective() -> void:
	%CoachObjective.text = _rejection_message if not _rejection_message.is_empty() else str(_coach_state.get("objective", "No legal action is available."))


func _apply_hand_states() -> void:
	var legal_ids: Array = _coach_state.get("legal_source_ids", [])
	var reasons: Dictionary = _coach_state.get("source_reasons", {})
	for child in %PlayerHand.get_children():
		var instance_id := str(child.card_data.get("instance_id", ""))
		if instance_id == model.selected_source_id:
			child.set_action_state("selected")
		elif instance_id in legal_ids:
			child.set_action_state("legal")
		elif reasons.has(instance_id):
			child.set_action_state("unavailable", str(reasons[instance_id]))
		else:
			child.set_action_state("normal")


func _clear_rejection() -> void:
	_rejection_message = ""
	model.rejection_code = ""
	if is_node_ready():
		%StatusLabel.text = ""


func _snapshot_state_key(value: Dictionary) -> String:
	if value.is_empty():
		return ""
	return "%s|%s|%s|%s|%s" % [
		str(value.get("sequence", "")),
		str(value.get("turn", "")),
		str(value.get("phase", "")),
		str(value.get("active_player_id", "")),
		str(value.get("winner_id", "")),
	]
