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

func render(cards: Array, hidden := false) -> void:
	for child in get_children():
		child.free()
	for index in range(slot_count):
		var slot := _DropSlot.new()
		slot.custom_minimum_size = Vector2(90, 124)
		slot.zone = zone_name
		slot.slot_index = index
		slot.pressed.connect(func() -> void: slot_pressed.emit(zone_name, index))
		slot.card_dropped.connect(func(instance_id: String) -> void: card_dropped.emit(instance_id, zone_name, index))
		add_child(slot)
		var card_data: Variant = cards[index] if index < cards.size() else null
		if card_data is Dictionary:
			var card = CardViewScene.instantiate()
			slot.add_child(card)
			card.bind(card_data, "hidden" if hidden else "battlefield")
			card.card_pressed.connect(func(instance_id: String) -> void: card_pressed.emit(instance_id))
			card.card_dropped.connect(func(source_id: String, target_id: Variant) -> void: target_dropped.emit(source_id, str(target_id)))
	_apply_highlights()

func set_highlights(slots: Array, targets: Array) -> void:
	highlighted_slots = slots.duplicate()
	highlighted_targets = targets.duplicate()
	_apply_highlights()

func _apply_highlights() -> void:
	for index in range(get_child_count()):
		var slot := get_child(index) as _DropSlot
		slot.modulate = Color("d8bd58") if index in highlighted_slots else Color.WHITE
		if slot.get_child_count() > 0:
			var card = slot.get_child(0)
			card.modulate = Color("d8bd58") if str(card.card_data.get("instance_id", "")) in highlighted_targets else Color.WHITE

class _DropSlot:
	extends Button
	signal card_dropped(instance_id: String)
	var zone := ""
	var slot_index := -1
	func _can_drop_data(_position: Vector2, data: Variant) -> bool:
		return data is Dictionary and not str(data.get("instance_id", "")).is_empty()
	func _drop_data(_position: Vector2, data: Variant) -> void:
		if _can_drop_data(_position, data):
			card_dropped.emit(str(data.instance_id))
