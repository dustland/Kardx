class_name CardMotionDirector
extends RefCounted

const DURATIONS_MS := {
	"card_drawn": 220.0,
	"card_deployed": 240.0,
	"unit_moved": 220.0,
	"attack_started": 260.0,
	"damage_dealt": 180.0,
	"fatigue_damage": 180.0,
	"card_destroyed": 180.0,
	"order_played": 220.0,
	"countermeasure_activated": 220.0,
	"countermeasure_deactivated": 220.0,
	"countermeasure_triggered": 220.0,
}

var speed_scale := 1.0
var processed_event_types: Array[String] = []
var last_duration_ms := 0.0
var _generation := 0
var _transients: Array[Node] = []
var _active_tween: Tween

func play(events: Array, before_snapshot: Dictionary, after_snapshot: Dictionary, view: MatchView) -> void:
	cancel()
	var generation := _generation
	processed_event_types.clear()
	for value in events:
		if generation != _generation or not is_instance_valid(view):
			break
		if not (value is Dictionary):
			continue
		var event: Dictionary = value
		var event_type := str(event.get("type", ""))
		if not DURATIONS_MS.has(event_type):
			continue
		processed_event_types.append(event_type)
		await _play_event(event_type, event, before_snapshot, after_snapshot, view, generation)
	_cleanup_transients()

func cancel() -> void:
	_generation += 1
	if _active_tween != null and _active_tween.is_valid():
		_active_tween.kill()
	_active_tween = null
	_cleanup_transients()

func _play_event(event_type: String, event: Dictionary, _before: Dictionary, _after: Dictionary, view: MatchView, generation: int) -> void:
	var duration_ms: float = float(DURATIONS_MS[event_type])
	if view.animation_mode == "reduced":
		duration_ms = minf(duration_ms, 80.0)
	last_duration_ms = duration_ms
	var rects := view.visible_card_rects()
	var source_id := _source_id(event_type, event)
	var target_id := _target_id(event_type, event)
	var source_rect: Rect2 = rects.get(source_id, Rect2())
	var target_rect: Rect2 = rects.get(target_id, source_rect)
	var proxy := ColorRect.new()
	proxy.color = Color(0.94, 0.81, 0.33, 0.32)
	proxy.mouse_filter = Control.MOUSE_FILTER_IGNORE
	proxy.add_to_group("card_motion_proxy")
	view.add_child(proxy)
	_transients.append(proxy)
	if source_rect.has_area():
		proxy.global_position = source_rect.position
		proxy.size = source_rect.size
	else:
		var pulse_rect := view.animation_zone_rect(str(event.get("player_id", "")))
		proxy.global_position = pulse_rect.position
		proxy.size = pulse_rect.size
	proxy.modulate.a = 0.8
	_active_tween = view.create_tween().set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	var duration := duration_ms * speed_scale / 1000.0
	if view.animation_mode == "reduced" or not target_rect.has_area():
		_active_tween.tween_property(proxy, "modulate:a", 0.0, duration)
	else:
		_active_tween.set_parallel(true)
		_active_tween.tween_property(proxy, "global_position", target_rect.position, duration)
		_active_tween.tween_property(proxy, "scale", Vector2(1.06, 1.06), duration * 0.5)
		_active_tween.tween_property(proxy, "modulate:a", 0.0, duration)
	if event_type in ["damage_dealt", "fatigue_damage"]:
		_add_damage_indicator(view, target_rect if target_rect.has_area() else source_rect, int(event.get("damage", 0)))
	await _active_tween.finished
	if generation == _generation:
		_cleanup_transients()

func _add_damage_indicator(view: MatchView, rect: Rect2, damage: int) -> void:
	var label := Label.new()
	label.text = "-%d" % damage
	label.add_theme_font_size_override("font_size", 22)
	label.add_theme_color_override("font_color", Color("ff8b73"))
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.add_to_group("card_damage_indicator")
	view.add_child(label)
	label.global_position = rect.get_center() - Vector2(16, 18)
	_transients.append(label)

func _cleanup_transients() -> void:
	for node in _transients:
		if is_instance_valid(node): node.free()
	_transients.clear()

func _source_id(event_type: String, event: Dictionary) -> String:
	match event_type:
		"attack_started": return str(event.get("attacker_id", ""))
		"order_played": return str(event.get("order_id", ""))
	return str(event.get("instance_id", event.get("source_id", "")))

func _target_id(event_type: String, event: Dictionary) -> String:
	if event_type == "attack_started": return str(event.get("defender_id", ""))
	if event_type in ["damage_dealt", "fatigue_damage"]: return str(event.get("target_id", ""))
	var targets: Array = event.get("target_ids", [])
	return str(targets[0]) if not targets.is_empty() else _source_id(event_type, event)
