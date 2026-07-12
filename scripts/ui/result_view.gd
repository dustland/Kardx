class_name ResultView
extends Control

signal rematch_requested
signal deck_builder_requested

var result_payload: Dictionary = {}


func _ready() -> void:
	%RematchButton.pressed.connect(func() -> void: rematch_requested.emit())
	%DeckBuilderButton.pressed.connect(func() -> void: deck_builder_requested.emit())


func initialize(_router, payload: Dictionary) -> void:
	result_payload = payload.duplicate(true)
	var winner_id := str(payload.get("winner_id", ""))
	%OutcomeLabel.text = "Victory" if winner_id == "player" else ("Defeat" if winner_id == "opponent" else "Draw")
	%WinnerLabel.text = _winner_text(winner_id)
	%ReasonLabel.text = _reason_text(str(payload.get("reason", "unknown")))
	var turns := int(payload.get("turns", 0))
	%TurnsLabel.text = "%d %s" % [turns, "turn" if turns == 1 else "turns"]
	%SeedLabel.text = "Match seed %d" % int(payload.get("seed", 0))


func _winner_text(winner_id: String) -> String:
	match winner_id:
		"player": return "Player wins"
		"opponent": return "Opponent wins"
		_: return "No winner"


func _reason_text(reason: String) -> String:
	var known := {
		"headquarters_destroyed": "Headquarters destroyed",
		"fatigue": "Fatigue",
		"concede": "Conceded",
		"draw": "Draw",
		"invalid": "Invalid match",
	}
	return str(known.get(reason, reason.replace("_", " ").capitalize()))


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_accept"):
		rematch_requested.emit()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("ui_cancel"):
		deck_builder_requested.emit()
		get_viewport().set_input_as_handled()
