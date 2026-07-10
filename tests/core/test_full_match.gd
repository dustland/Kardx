extends RefCounted

const CoreCards = preload("res://tests/fixtures/core_cards.gd")
const ReplayLog = preload("res://scripts/core/replay_log.gd")


static func run(t) -> void:
	_test_scripted_match_replays_identically(t)


static func _test_scripted_match_replays_identically(t) -> void:
	var controller = CoreCards.scripted_full_match(90210)
	var event_types := _event_types(controller.event_history)
	t.assert_eq(controller.state.phase, "complete", "scripted match reaches a terminal result")
	t.assert_eq(controller.state.winner_id, "player", "scripted match defeats the opponent Headquarters")
	t.assert_true(event_types.has("credit_refilled"), "scripted match traverses Credit turns")
	t.assert_true(event_types.has("card_deployed"), "scripted match deploys a unit")
	t.assert_true(event_types.has("unit_moved"), "scripted match moves into the Frontline")
	t.assert_true(event_types.has("attack_started"), "scripted match resolves combat")
	t.assert_true(event_types.has("order_played"), "scripted match plays an Order")
	t.assert_true(event_types.has("countermeasure_triggered"), "scripted match triggers a Countermeasure effect")
	t.assert_true(event_types.has("match_ended"), "scripted match ends from Headquarters defeat")

	var replayed = ReplayLog.from_dict(controller.replay_log.to_dict()).replay(controller.card_definitions)
	t.assert_eq(replayed.state_hash(), controller.state_hash(), "full replay reaches the same hash")
	t.assert_eq(replayed.event_history, controller.event_history, "full replay emits the same events")
	t.assert_eq(replayed.state.winner_id, controller.state.winner_id, "full replay preserves winner")


static func _event_types(events: Array) -> Array[String]:
	var types: Array[String] = []
	for event in events:
		types.append(str(event.type))
	return types
