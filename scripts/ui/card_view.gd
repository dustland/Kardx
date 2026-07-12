class_name CardView
extends Button

signal card_pressed(instance_id: String)
signal card_drag_started(instance_id: String)
signal card_dropped(instance_id: String, target: Variant)

const MODE_SIZES := {
	"catalog": Vector2(180, 252),
	"hand": Vector2(116, 162),
	"battlefield": Vector2(84, 118),
	"hidden": Vector2(116, 162),
}

var card_data: Dictionary = {}
var mode := "catalog"


func _ready() -> void:
	pressed.connect(_on_pressed)


func bind(data: Dictionary, display_mode: String) -> void:
	assert(MODE_SIZES.has(display_mode), "Unsupported card display mode: %s" % display_mode)
	mode = display_mode
	size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	size_flags_vertical = Control.SIZE_SHRINK_CENTER
	custom_minimum_size = MODE_SIZES[mode]
	size = custom_minimum_size
	_apply_mode_layout()

	var hidden := mode == "hidden" or bool(data.get("hidden", false))
	var instance_id := str(data.get("instance_id", ""))
	card_data = {"instance_id": instance_id, "hidden": true} if hidden else data.duplicate(true)
	get_node("CardBack").visible = hidden
	get_node("Frame").visible = not hidden
	tooltip_text = "" if hidden else str(data.get("description", ""))
	if hidden:
		_clear_face()
		return

	get_node("Frame/Title").text = str(data.get("title", ""))
	get_node("Frame/Type").text = _type_mark(data)
	get_node("Frame/Costs/Deployment").text = str(data.get("deployment_cost", ""))
	get_node("Frame/Costs/Operation").text = str(data.get("operation_cost", ""))
	get_node("Frame/Description").text = str(data.get("description", ""))
	get_node("Frame/Keywords").text = "  ".join(data.get("keywords", []))
	get_node("Frame/Stats/Attack").text = str(data.get("attack", ""))
	get_node("Frame/Stats/Defense").text = str(data.get("defense", ""))
	get_node("Frame/Artwork").texture = _load_art(str(data.get("image_path", "")))


func _on_pressed() -> void:
	card_pressed.emit(_instance_id())


func _get_drag_data(_at_position: Vector2) -> Variant:
	var instance_id := _instance_id()
	card_drag_started.emit(instance_id)
	if is_inside_tree():
		var preview := Label.new()
		preview.text = str(card_data.get("title", "Card"))
		set_drag_preview(preview)
	return {"instance_id": instance_id}


func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	return data is Dictionary and not str(data.get("instance_id", "")).is_empty()


func _drop_data(_at_position: Vector2, data: Variant) -> void:
	if _can_drop_data(_at_position, data):
		card_dropped.emit(str(data.get("instance_id")), _instance_id())


func _instance_id() -> String:
	return str(card_data.get("instance_id", ""))


func _clear_face() -> void:
	for path in ["Frame/Title", "Frame/Type", "Frame/Costs/Deployment", "Frame/Costs/Operation", "Frame/Description", "Frame/Keywords", "Frame/Stats/Attack", "Frame/Stats/Defense"]:
		get_node(path).text = ""
	get_node("Frame/Artwork").texture = _fallback_art()


func _apply_mode_layout() -> void:
	var frame := get_node("Frame") as Control
	var artwork := get_node("Frame/Artwork") as Control
	var title := get_node("Frame/Title") as Label
	var type := get_node("Frame/Type") as Label
	var costs := get_node("Frame/Costs") as Control
	var description := get_node("Frame/Description") as Control
	var keywords := get_node("Frame/Keywords") as Control
	var stats := get_node("Frame/Stats") as Control
	frame.clip_contents = true
	artwork.visible = mode != "hidden"
	title.visible = mode != "hidden"
	type.visible = mode != "hidden"
	costs.visible = mode != "hidden"
	stats.visible = mode != "hidden"
	description.visible = mode == "catalog"
	keywords.visible = mode == "catalog"

	match mode:
		"catalog":
			title.add_theme_font_size_override("font_size", 13)
			type.add_theme_font_size_override("font_size", 13)
			_set_rect(title, 5, 3, 137, 27)
			_set_rect(type, 141, 3, 167, 27)
			_set_rect(costs, 123, 31, 165, 50)
			_set_rect(artwork, 5, 54, 167, 126)
			_set_rect(description, 6, 130, 166, 188)
			_set_rect(keywords, 6, 192, 166, 211)
			_set_rect(stats, 106, 216, 166, 237)
		"hand":
			title.add_theme_font_size_override("font_size", 10)
			type.add_theme_font_size_override("font_size", 10)
			_set_rect(title, 4, 2, 78, 21)
			_set_rect(type, 82, 2, 104, 21)
			_set_rect(costs, 62, 24, 104, 43)
			_set_rect(artwork, 4, 47, 104, 119)
			_set_rect(stats, 44, 128, 104, 149)
		"battlefield":
			title.add_theme_font_size_override("font_size", 9)
			type.add_theme_font_size_override("font_size", 9)
			_set_rect(title, 3, 2, 51, 19)
			_set_rect(type, 54, 2, 72, 19)
			_set_rect(costs, 30, 22, 72, 41)
			_set_rect(artwork, 3, 45, 72, 78)
			_set_rect(stats, 12, 84, 72, 105)


func _set_rect(control: Control, left: float, top: float, right: float, bottom: float) -> void:
	control.anchor_left = 0.0
	control.anchor_top = 0.0
	control.anchor_right = 0.0
	control.anchor_bottom = 0.0
	control.offset_left = left
	control.offset_top = top
	control.offset_right = right
	control.offset_bottom = bottom


func _type_mark(data: Dictionary) -> String:
	var unit_type := str(data.get("unit_type", ""))
	return unit_type.left(1).to_upper() if not unit_type.is_empty() else str(data.get("category", "")).left(1).to_upper()


func _load_art(path: String) -> Texture2D:
	if not path.is_empty() and ResourceLoader.exists(path):
		var resource := load(path)
		if resource is Texture2D:
			return resource
	return _fallback_art()


func _fallback_art() -> Texture2D:
	var gradient := Gradient.new()
	gradient.colors = PackedColorArray([Color("27352f"), Color("786f4b"), Color("38453b")])
	gradient.offsets = PackedFloat32Array([0.0, 0.58, 1.0])
	var texture := GradientTexture2D.new()
	texture.gradient = gradient
	texture.width = 180
	texture.height = 120
	texture.fill_from = Vector2(0.0, 0.0)
	texture.fill_to = Vector2(1.0, 1.0)
	return texture
