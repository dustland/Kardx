class_name TimelineView
extends VBoxContainer

const MAX_ENTRIES := 40

func render_events(events: Array) -> void:
	for event_value in events:
		if not (event_value is Dictionary):
			continue
		var event: Dictionary = event_value
		var label := Label.new()
		label.text = _format_event(event)
		label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		add_child(label)
	while get_child_count() > MAX_ENTRIES:
		get_child(0).free()

func _format_event(event: Dictionary) -> String:
	var title := str(event.get("type", "event")).replace("_", " ").capitalize()
	var actor := str(event.get("actor_id", event.get("payload", {}).get("actor_id", "")))
	return title if actor.is_empty() else "%s: %s" % [actor.capitalize(), title]
