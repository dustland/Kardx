class_name ActionBuilder
extends RefCounted


static func deploy(instance_id: String, support_slot: int, actor_id: String, sequence: int) -> GameAction:
	return GameAction.create(
		"deploy_unit", actor_id, instance_id, [], {"support_slot": support_slot}, sequence
	)


static func move(instance_id: String, frontline_slot: int, actor_id: String, sequence: int) -> GameAction:
	return GameAction.create(
		"move_unit", actor_id, instance_id, [], {"zone": "frontline", "slot": frontline_slot}, sequence
	)


static func attack(instance_id: String, target_id: String, actor_id: String, sequence: int) -> GameAction:
	return GameAction.create(
		"attack_unit", actor_id, instance_id, [target_id], {}, sequence
	)


static func play_order(instance_id: String, target_ids: Array[String], actor_id: String, sequence: int) -> GameAction:
	return GameAction.create(
		"play_order", actor_id, instance_id, target_ids, {}, sequence
	)


static func toggle_countermeasure(instance_id: String, actor_id: String, sequence: int) -> GameAction:
	return GameAction.create(
		"toggle_countermeasure", actor_id, instance_id, [], {}, sequence
	)
