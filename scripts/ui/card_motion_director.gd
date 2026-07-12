class_name CardMotionDirector
extends RefCounted

const DURATIONS_MS := {
	"card_drawn": 220.0, "card_deployed": 240.0, "unit_moved": 220.0,
	"attack_started": 260.0, "damage_dealt": 180.0, "fatigue_damage": 180.0,
	"card_destroyed": 180.0, "order_played": 220.0,
	"countermeasure_activated": 220.0, "countermeasure_deactivated": 220.0,
	"countermeasure_triggered": 220.0,
}

var speed_scale := 1.0
var processed_event_types: Array[String] = []
var last_duration_ms := 0.0
var last_source_rect := Rect2()
var last_destination_rect := Rect2()
var current_event_type := ""
var _generation := 0
var _transients: Array[Node] = []
var _active_tween: Tween
var _aux_tweens: Array[Tween] = []
var _flash_cards: Dictionary = {}

func play(events: Array, before_snapshot: Dictionary, after_snapshot: Dictionary, view: MatchView) -> void:
	cancel()
	var generation := _generation
	processed_event_types.clear()
	var before_rects := view.snapshot_card_rects(before_snapshot)
	var after_rects := view.snapshot_card_rects(after_snapshot)
	for value in events:
		if generation != _generation or not is_instance_valid(view): break
		if not (value is Dictionary): continue
		var event: Dictionary = value
		var event_type := str(event.get("type", ""))
		if not DURATIONS_MS.has(event_type): continue
		processed_event_types.append(event_type)
		current_event_type = event_type
		await _play_event(event_type, event, before_snapshot, after_snapshot, before_rects, after_rects, view, generation)
	current_event_type = ""
	_cleanup_transients()

func cancel() -> void:
	_generation += 1
	current_event_type = ""
	if _active_tween != null and _active_tween.is_valid(): _active_tween.kill()
	_active_tween = null
	for tween in _aux_tweens:
		if tween != null and tween.is_valid(): tween.kill()
	_aux_tweens.clear()
	for card in _flash_cards:
		if is_instance_valid(card):
			card.modulate = _flash_cards[card]
			card.set_meta("motion_flash_active", false)
	_flash_cards.clear()
	_cleanup_transients()

func _play_event(event_type: String, event: Dictionary, before: Dictionary, after: Dictionary, before_rects: Dictionary, after_rects: Dictionary, view: MatchView, generation: int) -> void:
	var duration_ms: float = float(DURATIONS_MS[event_type])
	if view.animation_mode == "reduced": duration_ms = minf(duration_ms, 80.0)
	last_duration_ms = duration_ms
	var source_id := _source_id(event_type, event, before, after)
	var target_id := _target_id(event_type, event, before, after)
	var source_rect: Rect2 = before_rects.get(source_id, Rect2())
	var destination_rect: Rect2 = after_rects.get(source_id, source_rect)
	if event_type == "card_drawn" and not source_rect.has_area(): source_rect = view.deck_edge_rect(str(event.get("player_id", "player")))
	if event_type in ["order_played", "countermeasure_activated", "countermeasure_deactivated", "countermeasure_triggered"]:
		destination_rect = view.command_area_rect()
	if event_type in ["damage_dealt", "fatigue_damage"]:
		destination_rect = before_rects.get(target_id, after_rects.get(target_id, source_rect))
	last_source_rect = source_rect
	last_destination_rect = destination_rect
	var proxy := _proxy(view, source_rect if source_rect.has_area() else view.animation_zone_rect(str(event.get("player_id", ""))))
	var duration := duration_ms * speed_scale / 1000.0
	_active_tween = view.create_tween().set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	if view.animation_mode == "reduced" or not source_rect.has_area():
		_active_tween.tween_property(proxy, "modulate:a", 0.0, duration)
	elif event_type == "attack_started":
		var target_rect: Rect2 = before_rects.get(target_id, after_rects.get(target_id, Rect2()))
		var lunge := source_rect.position.lerp(target_rect.position, 0.35) if target_rect.has_area() else source_rect.position
		_active_tween.tween_property(proxy, "global_position", lunge, duration * 0.5)
		_active_tween.tween_property(proxy, "global_position", source_rect.position, duration * 0.5)
		_flash_target(view.card_view(target_id), duration)
	elif event_type == "card_destroyed":
		_active_tween.set_parallel(true)
		_active_tween.tween_property(proxy, "scale", Vector2(0.85, 0.85), duration)
		_active_tween.tween_property(proxy, "modulate:a", 0.0, duration)
	else:
		_active_tween.set_parallel(true)
		_active_tween.tween_property(proxy, "global_position", destination_rect.position, duration)
		_active_tween.tween_property(proxy, "modulate:a", 0.15 if destination_rect.has_area() else 0.0, duration)
	if event_type in ["damage_dealt", "fatigue_damage"]: _add_damage_indicator(view, destination_rect, int(event.get("damage", event.get("amount", 0))))
	await _wait_for_tween(view, generation)
	_active_tween = null
	if generation == _generation:
		_finish_flashes()
		_cleanup_transients()

func _wait_for_tween(view: MatchView, generation: int) -> void:
	while generation == _generation and is_instance_valid(view) and _active_tween != null and _active_tween.is_valid() and _active_tween.is_running():
		await view.get_tree().process_frame

func _proxy(view: MatchView, rect: Rect2) -> ColorRect:
	var proxy := ColorRect.new()
	proxy.color = Color(0.94, 0.81, 0.33, 0.55)
	proxy.mouse_filter = Control.MOUSE_FILTER_IGNORE
	proxy.add_to_group("card_motion_proxy")
	view.add_child(proxy)
	proxy.global_position = rect.position
	proxy.size = rect.size
	_transients.append(proxy)
	return proxy

func _flash_target(card, duration: float) -> void:
	if card == null or not is_instance_valid(card): return
	var original: Color = card.modulate
	_flash_cards[card] = original
	card.set_meta("motion_flash_active", true)
	var tween: Tween = card.create_tween()
	_aux_tweens.append(tween)
	tween.tween_property(card, "modulate", Color("fff0a0"), duration * 0.5)
	tween.tween_property(card, "modulate", original, duration * 0.5)

func _finish_flashes() -> void:
	_aux_tweens.clear()
	for card in _flash_cards:
		if is_instance_valid(card):
			card.modulate = _flash_cards[card]
			card.set_meta("motion_flash_active", false)
	_flash_cards.clear()

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

func _source_id(event_type: String, event: Dictionary, before: Dictionary, after: Dictionary) -> String:
	if event_type == "card_destroyed":
		var destroyed_id := str(event.get("target_id", ""))
		if not destroyed_id.is_empty(): return destroyed_id
	for key in ["instance_id", "source_id", "attacker_id", "order_id"]:
		var value := str(event.get(key, ""))
		if not value.is_empty(): return value
	return _infer_changed_id(event_type, before, after)

func _target_id(event_type: String, event: Dictionary, before: Dictionary, after: Dictionary) -> String:
	for key in ["target_id", "defender_id"]:
		var value := str(event.get(key, ""))
		if not value.is_empty(): return value
	var targets: Array = event.get("target_ids", [])
	if not targets.is_empty(): return str(targets[0])
	if event_type == "fatigue_damage": return _headquarters_id(before, str(event.get("player_id", "")))
	return _source_id(event_type, event, before, after)

func _infer_changed_id(event_type: String, before: Dictionary, after: Dictionary) -> String:
	var before_locations := _locations(before)
	var after_locations := _locations(after)
	for instance_id in before_locations:
		if not after_locations.has(instance_id) and event_type in ["card_destroyed", "order_played"]: return instance_id
		if after_locations.has(instance_id) and before_locations[instance_id] != after_locations[instance_id]: return instance_id
	for instance_id in after_locations:
		if not before_locations.has(instance_id): return instance_id
	return ""

func _locations(snapshot: Dictionary) -> Dictionary:
	var result := {}
	var players: Dictionary = snapshot.get("players", {})
	for player_id in ["player", "opponent"]:
		var player: Dictionary = players.get(player_id, {})
		for zone_name in ["hand", "support_line"]:
			var cards: Array = player.get(zone_name, [])
			for index in range(cards.size()):
				if cards[index] is Dictionary:
					var id := str(cards[index].get("instance_id", ""))
					if not id.is_empty() and not bool(cards[index].get("hidden", false)): result[id] = "%s:%s:%d" % [player_id, zone_name, index]
	var frontline: Array = snapshot.get("frontline", [])
	for index in range(frontline.size()):
		if frontline[index] is Dictionary:
			var id := str(frontline[index].get("instance_id", ""))
			if not id.is_empty(): result[id] = "frontline:%d" % index
	return result

func _headquarters_id(snapshot: Dictionary, player_id: String) -> String:
	return str(snapshot.get("players", {}).get(player_id, {}).get("headquarters", {}).get("instance_id", ""))
