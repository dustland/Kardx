extends Control

const CARDS_PATH := "res://data/cards.json"
const ABILITIES_PATH := "res://data/abilities.json"

var cards: Array = []
var abilities: Array = []
var card_grid: GridContainer
var search_box: LineEdit
var detail_panel: PanelContainer
var detail_content: VBoxContainer
var status_label: Label

var palette := {
	"background": Color("101820"),
	"surface": Color("192633"),
	"surface_alt": Color("223444"),
	"accent": Color("d8a84e"),
	"muted": Color("9eafbd"),
	"text": Color("edf2f5")
}

func _ready() -> void:
	cards = _load_json_array(CARDS_PATH)
	abilities = _load_json_array(ABILITIES_PATH)
	_build_interface()
	_render_cards(cards)
	if not cards.is_empty():
		_show_card(cards[0])

func _load_json_array(path: String) -> Array:
	if not FileAccess.file_exists(path):
		return []
	var parsed = JSON.parse_string(FileAccess.get_file_as_string(path))
	return parsed if parsed is Array else []

func _build_interface() -> void:
	var background := ColorRect.new()
	background.color = palette.background
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(background)

	var margin := MarginContainer.new()
	margin.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	margin.add_theme_constant_override("margin_left", 28)
	margin.add_theme_constant_override("margin_top", 24)
	margin.add_theme_constant_override("margin_right", 28)
	margin.add_theme_constant_override("margin_bottom", 24)
	add_child(margin)

	var page := VBoxContainer.new()
	page.add_theme_constant_override("separation", 18)
	margin.add_child(page)

	var header := HBoxContainer.new()
	header.custom_minimum_size.y = 64
	page.add_child(header)
	var title := Label.new()
	title.text = "OPENKARDS"
	title.add_theme_font_size_override("font_size", 30)
	title.add_theme_color_override("font_color", palette.accent)
	header.add_child(title)
	var subtitle := Label.new()
	subtitle.text = "  tactical card collection"
	subtitle.add_theme_font_size_override("font_size", 16)
	subtitle.add_theme_color_override("font_color", palette.muted)
	subtitle.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	header.add_child(subtitle)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(spacer)
	search_box = LineEdit.new()
	search_box.placeholder_text = "Search cards"
	search_box.custom_minimum_size = Vector2(260, 42)
	search_box.text_changed.connect(_on_search_changed)
	header.add_child(search_box)

	var body := HBoxContainer.new()
	body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_theme_constant_override("separation", 18)
	page.add_child(body)

	var list_panel := PanelContainer.new()
	list_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list_panel.size_flags_vertical = Control.SIZE_EXPAND_FILL
	list_panel.add_theme_stylebox_override("panel", _panel_style(palette.surface))
	body.add_child(list_panel)
	var list_margin := MarginContainer.new()
	list_margin.add_theme_constant_override("margin_left", 18)
	list_margin.add_theme_constant_override("margin_top", 18)
	list_margin.add_theme_constant_override("margin_right", 18)
	list_margin.add_theme_constant_override("margin_bottom", 18)
	list_panel.add_child(list_margin)
	var list := VBoxContainer.new()
	list_margin.add_child(list)
	status_label = Label.new()
	status_label.add_theme_color_override("font_color", palette.muted)
	list.add_child(status_label)
	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	list.add_child(scroll)
	card_grid = GridContainer.new()
	card_grid.columns = 3
	card_grid.add_theme_constant_override("h_separation", 12)
	card_grid.add_theme_constant_override("v_separation", 12)
	scroll.add_child(card_grid)

	detail_panel = PanelContainer.new()
	detail_panel.custom_minimum_size.x = 330
	detail_panel.add_theme_stylebox_override("panel", _panel_style(palette.surface))
	body.add_child(detail_panel)
	var detail_margin := MarginContainer.new()
	detail_margin.add_theme_constant_override("margin_left", 22)
	detail_margin.add_theme_constant_override("margin_top", 22)
	detail_margin.add_theme_constant_override("margin_right", 22)
	detail_margin.add_theme_constant_override("margin_bottom", 22)
	detail_panel.add_child(detail_margin)
	detail_content = VBoxContainer.new()
	detail_content.add_theme_constant_override("separation", 12)
	detail_margin.add_child(detail_content)

func _render_cards(source: Array) -> void:
	for child in card_grid.get_children():
		child.queue_free()
	status_label.text = "%d cards  |  %d abilities" % [source.size(), abilities.size()]
	for card in source:
		var button := Button.new()
		button.custom_minimum_size = Vector2(190, 116)
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.text = "%s\n%s  |  Cost %s\nATK %s  DEF %s" % [
			card.get("title", "Unknown"), card.get("subtype", "Card"),
			card.get("deploymentCost", "-"), card.get("baseAttack", "-"), card.get("baseDefense", "-")]
		button.add_theme_font_size_override("font_size", 15)
		button.add_theme_color_override("font_color", palette.text)
		button.add_theme_stylebox_override("normal", _panel_style(palette.surface_alt))
		button.add_theme_stylebox_override("hover", _panel_style(Color("30495d")))
		var image_path := "res://game_assets/cards/%s" % card.get("imageUrl", "")
		if ResourceLoader.exists(image_path):
			button.icon = load(image_path)
			button.expand_icon = true
		button.icon_max_width = 62
		button.icon_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		button.pressed.connect(_show_card.bind(card))
		card_grid.add_child(button)

func _show_card(card: Dictionary) -> void:
	for child in detail_content.get_children():
		child.queue_free()
	var title := Label.new()
	title.text = card.get("title", "Unknown Card")
	title.add_theme_font_size_override("font_size", 25)
	title.add_theme_color_override("font_color", palette.accent)
	detail_content.add_child(title)
	var meta := Label.new()
	meta.text = "%s / %s    %s" % [card.get("category", "Card"), card.get("subtype", ""), card.get("rarity", "")]
	meta.add_theme_color_override("font_color", palette.muted)
	detail_content.add_child(meta)
	var stats := Label.new()
	stats.text = "Deployment %s    Operation %s\nAttack %s    Defense %s    Counter %s" % [
		card.get("deploymentCost", "-"), card.get("operationCost", "-"),
		card.get("baseAttack", "-"), card.get("baseDefense", "-"), card.get("baseCounterAttack", "-")]
	stats.add_theme_color_override("font_color", palette.text)
	detail_content.add_child(stats)
	var description := Label.new()
	description.text = card.get("description", "")
	description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	description.add_theme_color_override("font_color", palette.text)
	detail_content.add_child(description)
	var ability_header := Label.new()
	ability_header.text = "Abilities"
	ability_header.add_theme_font_size_override("font_size", 18)
	ability_header.add_theme_color_override("font_color", palette.accent)
	detail_content.add_child(ability_header)
	var ability_map := {}
	for ability in abilities:
		ability_map[ability.get("id", "")] = ability
	for ability_id in card.get("abilities", []):
		var ability: Dictionary = ability_map.get(ability_id, {})
		var ability_label := Label.new()
		ability_label.text = "- %s: %s" % [ability.get("name", ability_id), ability.get("description", "")]
		ability_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		ability_label.add_theme_color_override("font_color", palette.muted)
		detail_content.add_child(ability_label)

func _on_search_changed(query: String) -> void:
	var normalized := query.strip_edges().to_lower()
	if normalized.is_empty():
		_render_cards(cards)
		return
	var filtered: Array = []
	for card in cards:
		var haystack := "%s %s %s" % [card.get("title", ""), card.get("description", ""), card.get("subtype", "")]
		if normalized in haystack.to_lower():
			filtered.append(card)
	_render_cards(filtered)

func _panel_style(color: Color) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = color
	style.corner_radius_top_left = 6
	style.corner_radius_top_right = 6
	style.corner_radius_bottom_left = 6
	style.corner_radius_bottom_right = 6
	style.border_width_left = 1
	style.border_width_top = 1
	style.border_width_right = 1
	style.border_width_bottom = 1
	style.border_color = Color("385268")
	return style
