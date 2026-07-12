class_name MulliganViewModel
extends Control

signal confirm_requested(selected_instance_ids: Array[String])

const CardViewScene = preload("res://scenes/ui/card_view.tscn")

var _hand_ids: Array[String] = []
var _selected: Dictionary = {}
var _locked := false


func _init(instance_ids: Array[String] = []) -> void:
	_hand_ids = instance_ids.duplicate()


func initialize(_main, payload: Dictionary) -> void:
	var snapshot: Dictionary = payload.get("snapshot", {})
	var players: Dictionary = snapshot.get("players", {})
	var player: Dictionary = players.get("player", {})
	var hand: Array = player.get("hand", [])
	_hand_ids.clear()
	_selected.clear()
	_locked = false
	%ConfirmButton.disabled = false
	%DifficultyLabel.text = str(payload.get("difficulty", "standard")).to_upper()
	for child in %HandRow.get_children():
		child.queue_free()
	for card_value in hand:
		var card: Dictionary = card_value
		var instance_id := str(card.get("instance_id", ""))
		_hand_ids.append(instance_id)
		var view = CardViewScene.instantiate()
		view.toggle_mode = true
		view.card_pressed.connect(_on_card_pressed)
		%HandRow.add_child(view)
		view.bind(card, "hand")


func toggle(instance_id: String) -> void:
	if _locked or instance_id not in _hand_ids:
		return
	if _selected.has(instance_id):
		_selected.erase(instance_id)
	else:
		_selected[instance_id] = true


func selected_ids() -> Array[String]:
	var ordered: Array[String] = []
	for instance_id in _hand_ids:
		if _selected.has(instance_id):
			ordered.append(instance_id)
	return ordered


func lock() -> void:
	_locked = true
	if is_node_ready():
		%ConfirmButton.disabled = true
		for card in %HandRow.get_children():
			card.disabled = true


func _on_card_pressed(instance_id: String) -> void:
	toggle(instance_id)
	for card in %HandRow.get_children():
		if str(card.card_data.get("instance_id", "")) == instance_id:
			card.button_pressed = _selected.has(instance_id)
			card.position.y = -12.0 if card.button_pressed else 0.0
			card.add_theme_stylebox_override("pressed", _selected_card_style() if card.button_pressed else card.get_theme_stylebox("normal"))
			break


func _on_confirm_pressed() -> void:
	if _locked:
		return
	lock()
	confirm_requested.emit(selected_ids())


func _selected_card_style() -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = Color("1b231e")
	style.border_color = Color("d7b557")
	style.set_border_width_all(4)
	style.set_corner_radius_all(4)
	return style
