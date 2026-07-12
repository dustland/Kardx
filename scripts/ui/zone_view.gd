class_name ZoneView
extends HBoxContainer

signal slot_pressed(zone: String, slot: int)
signal card_pressed(instance_id: String)
signal card_dropped(instance_id: String, zone: String, slot: int)
signal target_dropped(source_id: String, target_id: String)

const CardViewScene = preload("res://scenes/ui/card_view.tscn")

@export var zone_name := "support"
@export var slot_count := 4
var highlighted_slots: Array = []
var highlighted_targets: Array = []
var input_locked := false

func render(cards: Array, hidden := false) -> void:
	for child in get_children():
		child.free()
	for index in range(slot_count):
		var slot := _DropSlot.new()
		slot.custom_minimum_size = Vector2(90, 124)
		slot.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		slot.zone = zone_name
		slot.slot_index = index
		slot.input_locked = input_locked
		slot.disabled = input_locked
		var slot_style := _slot_style(zone_name)
		slot.add_theme_stylebox_override("normal", slot_style)
		slot.add_theme_stylebox_override("hover", slot_style)
		slot.add_theme_stylebox_override("disabled", slot_style)
		slot.pressed.connect(func() -> void: slot_pressed.emit(zone_name, index))
		slot.card_dropped.connect(func(instance_id: String) -> void: card_dropped.emit(instance_id, zone_name, index))
		add_child(slot)
		var card_data: Variant = cards[index] if index < cards.size() else null
		if card_data is Dictionary:
			var card = CardViewScene.instantiate()
			slot.add_child(card)
			card.bind(card_data, "hidden" if hidden else "battlefield")
			card.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
			card.disabled = input_locked
			card.set_meta("owner_id", str(card_data.get("owner_id", "")))
			card.card_pressed.connect(func(instance_id: String) -> void: card_pressed.emit(instance_id))
			card.card_dropped.connect(func(source_id: String, target_id: Variant) -> void: target_dropped.emit(source_id, str(target_id)))
	_apply_highlights()

func _slot_style(zone: String) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	match zone:
		"frontline":
			style.bg_color = Color("2a3431")
			style.border_color = Color("667b7f")
		"opponent_support":
			style.bg_color = Color("332d29")
			style.border_color = Color("76584f")
		_:
			style.bg_color = Color("273237")
			style.border_color = Color("526d7d")
	style.set_border_width_all(1)
	style.set_corner_radius_all(3)
	return style

func set_input_locked(locked: bool) -> void:
	input_locked = locked
	for slot in get_children():
		slot.input_locked = locked
		slot.disabled = locked
		if slot.get_child_count() > 0:
			slot.get_child(0).disabled = locked

func set_highlights(slots: Array, targets: Array) -> void:
	highlighted_slots = slots.duplicate()
	highlighted_targets = targets.duplicate()
	_apply_highlights()

func _apply_highlights() -> void:
	for index in range(get_child_count()):
		var slot := get_child(index) as _DropSlot
		var highlighted := index in highlighted_slots
		var style := _slot_style(zone_name)
		if highlighted:
			style.border_color = Color("f0cf55")
			style.set_border_width_all(4)
		slot.add_theme_stylebox_override("normal", style)
		slot.add_theme_stylebox_override("hover", style)
		slot.add_theme_stylebox_override("disabled", style)
		slot.modulate = Color.WHITE
		if slot.get_child_count() > 0:
			var card = slot.get_child(0)
			var owner_color := Color("b9d8e8") if str(card.get_meta("owner_id", "")) == "player" else Color("e8b9b9")
			card.modulate = Color("f2d66d") if str(card.card_data.get("instance_id", "")) in highlighted_targets else owner_color

class _DropSlot:
	extends Button
	signal card_dropped(instance_id: String)
	var zone := ""
	var slot_index := -1
	var input_locked := false
	func _can_drop_data(_position: Vector2, data: Variant) -> bool:
		return not input_locked and data is Dictionary and not str(data.get("instance_id", "")).is_empty()
	func _drop_data(_position: Vector2, data: Variant) -> void:
		if _can_drop_data(_position, data):
			card_dropped.emit(str(data.instance_id))
