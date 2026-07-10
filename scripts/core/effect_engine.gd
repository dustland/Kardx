class_name EffectEngine
extends RefCounted

const CardInstance = preload("res://scripts/core/card_instance.gd")
const GameConstants = preload("res://scripts/core/game_constants.gd")

var state
var definitions: Dictionary
var rng: RandomNumberGenerator
var queue: Array[Dictionary] = []
var last_resolution: Dictionary = {"valid": true, "events": []}
var _resolving_actor_id: String = ""


static func create(match_state, card_definitions: Dictionary, match_rng: RandomNumberGenerator) -> EffectEngine:
	var engine: EffectEngine = load("res://scripts/core/effect_engine.gd").new()
	engine.state = match_state
	engine.definitions = card_definitions.duplicate(true)
	engine.rng = match_rng
	return engine


func validate_trigger(trigger: String, context: Dictionary) -> Dictionary:
	var prepared := _prepare_trigger(trigger, context)
	if not prepared.valid:
		return prepared
	if prepared.queue.size() > GameConstants.MAX_EFFECT_EVENTS:
		return {"valid": false, "code": "effect_limit"}
	return {"valid": true}


func resolve_trigger(trigger: String, context: Dictionary) -> Array[Dictionary]:
	queue.clear()
	var prepared := _prepare_trigger(trigger, context)
	if not prepared.valid:
		last_resolution = {"valid": false, "code": prepared.code, "events": []}
		return []
	queue = prepared.queue
	var drained := drain_queue()
	last_resolution = drained
	if not drained.valid:
		state.phase = "invalid"
	return drained.events


func drain_queue() -> Dictionary:
	var emitted: Array[Dictionary] = []
	while not queue.is_empty():
		if emitted.size() >= GameConstants.MAX_EFFECT_EVENTS:
			return {"valid": false, "code": "effect_limit", "events": emitted}
		var effect: Dictionary = queue.pop_front()
		var event := _apply_effect(effect)
		if not event.is_empty():
			emitted.append(event)
	return {"valid": true, "events": emitted}


func _prepare_trigger(trigger: String, context: Dictionary) -> Dictionary:
	var queued: Array[Dictionary] = []
	for source in _trigger_sources(trigger, context):
		var counter_triggered := false
		for ability_value in source.abilities:
			var ability: Dictionary = ability_value
			if str(ability.get("trigger", "")) != trigger:
				continue
			if context.has("ability_id") and str(ability.get("id", "")) != str(context.ability_id):
				continue
			if not _matches_conditions(source, ability.get("conditions", {}), context):
				continue
			var targets := _select_targets(source, ability.get("target", {}), context)
			var effects: Array = ability.get("effects", [])
			for effect_value in effects:
				var effect: Dictionary = effect_value.duplicate(true)
				effect["source_id"] = source.instance_id
				effect["actor_id"] = str(context.get("actor_id", source.owner_id))
				effect["target_ids"] = targets.duplicate()
				effect["target_selector"] = ability.get("target", {}).get("selector", "action_targets")
				effect["target_count"] = int(ability.get("target", {}).get("count", 0 if str(ability.get("target", {}).get("selector", "")) == "none" else 1))
				effect["context"] = context
				var validation := _validate_effect(effect)
				if not validation.valid:
					return validation
				queued.append(effect)
			if source.category == "Countermeasure" and source.countermeasure_active and not effects.is_empty():
				counter_triggered = true
		if counter_triggered:
			queued.append({"type": "trigger_countermeasure", "source_id": source.instance_id})
	var reservation := _validate_retreat_reservations(queued)
	if not reservation.valid:
		return reservation
	return {"valid": true, "queue": queued}


func _trigger_sources(trigger: String, context: Dictionary) -> Array[CardInstance]:
	var sources: Array[CardInstance] = []
	var source_id := str(context.get("source_id", ""))
	var explicit_source := _find_card(source_id)
	if explicit_source != null:
		sources.append(explicit_source)
	for player_id in state.players:
		var player = state.players[player_id]
		for counter in player.active_countermeasures:
			if counter != null and counter.countermeasure_active and counter != explicit_source and str(context.get("actor_id", "")) != counter.owner_id:
				sources.append(counter)
	return sources


func _matches_conditions(source: CardInstance, conditions: Dictionary, context: Dictionary) -> bool:
	if bool(conditions.get("enemy", false)) and str(context.get("actor_id", "")) == source.owner_id:
		return false
	if str(conditions.get("target_owner", "")) == "owner":
		if (context.get("target_ids", []) as Array).is_empty():
			return false
		for target_id in context.get("target_ids", []):
			var target := _find_card(str(target_id))
			if target != null and target.owner_id != source.owner_id:
				return false
	return true


func _select_targets(source: CardInstance, target: Dictionary, context: Dictionary) -> Array[String]:
	var selector := str(target.get("selector", "action_targets"))
	if selector == "none":
		return []
	if selector == "action_targets":
		return _string_ids(context.get("target_ids", []))
	if selector == "enemy_hq":
		return [_player_for(source.owner_id, false).headquarters.instance_id]
	if selector == "friendly_hq":
		return [_player_for(source.owner_id, true).headquarters.instance_id]
	if selector == "enemy_unit_or_hq":
		return _string_ids(context.get("target_ids", []))
	if selector == "enemy_unit":
		return _string_ids(context.get("target_ids", []))
	if selector == "random_enemy_unit":
		return []
	return []


func _validate_effect(effect: Dictionary) -> Dictionary:
	var effect_type := str(effect.get("type", ""))
	var target_ids: Array[String] = effect.get("target_ids", [])
	var selector := str(effect.get("target_selector", ""))
	var target_count := int(effect.get("target_count", 0))
	var caller_targets: Array = effect.context.get("target_ids", [])
	if selector == "random_enemy_unit":
		if not caller_targets.is_empty() or _public_enemy_units(_find_card(str(effect.get("source_id", ""))).owner_id).size() < target_count:
			return {"valid": false, "code": "invalid_target"}
	elif target_ids.size() != target_count:
		return {"valid": false, "code": "invalid_target"}
	if selector != "random_enemy_unit" and not ["credit", "credit_slots", "draw", "create", "replace_event", "trigger_countermeasure"].has(effect_type) and target_ids.is_empty():
		return {"valid": false, "code": "missing_target"}
	var seen := {}
	for target_id in target_ids:
		if seen.has(target_id):
			return {"valid": false, "code": "invalid_target"}
		seen[target_id] = true
		var target := _find_card(target_id)
		if target == null:
			return {"valid": false, "code": "invalid_target"}
		var source := _find_card(str(effect.get("source_id", "")))
		var owner_can_select_hand := effect_type == "discard" and target.zone == "hand" and target.owner_id == str(effect.context.get("actor_id", ""))
		if not _is_public_target(target) and not owner_can_select_hand:
			return {"valid": false, "code": "invalid_target"}
		if selector == "enemy_unit" and (target.owner_id == source.owner_id or target.category != "Unit" or target.zone == "headquarters"):
			return {"valid": false, "code": "invalid_target"}
		if selector == "enemy_unit_or_hq" and (target.owner_id == source.owner_id or not ["Unit", "Headquarters"].has(target.category)):
			return {"valid": false, "code": "invalid_target"}
	if effect_type == "create" and not definitions.has(str(effect.get("definition_id", ""))):
		return {"valid": false, "code": "unknown_definition"}
	return {"valid": true}


func _apply_effect(effect: Dictionary) -> Dictionary:
	var effect_type := str(effect.get("type", ""))
	if effect_type == "trigger_countermeasure":
		return _trigger_countermeasure(_find_card(str(effect.source_id)))
	if effect_type == "frontline_changed":
		return _event("frontline_changed", {
			"previous_controller_id": str(effect.get("previous_controller_id", "")),
			"controller_id": str(effect.get("controller_id", "")),
		})
	if effect_type == "match_ended":
		return _event("match_ended", {
			"winner_id": str(effect.get("winner_id", "")),
			"loser_id": str(effect.get("loser_id", "")),
		})
	_resolving_actor_id = str(effect.get("actor_id", ""))
	var source := _find_card(str(effect.get("source_id", "")))
	var targets := _cards_for_ids(_resolved_target_ids(effect))
	match effect_type:
		"damage":
			for target in targets:
				target.current_defense = maxi(0, target.current_defense - int(effect.get("amount", 0)))
				_check_lethal(target)
			return _event("damage_dealt", {"source_id": source.instance_id, "target_id": _first_id(targets), "damage": int(effect.get("amount", 0))})
		"repair":
			for target in targets:
				target.current_defense = mini(target.base_defense, target.current_defense + int(effect.get("amount", 0)))
			return _event("damage_repaired", {"source_id": source.instance_id, "target_id": _first_id(targets)})
		"buff", "debuff":
			var sign := 1 if effect_type == "buff" else -1
			for target in targets:
				target.current_attack += sign * int(effect.get("attack", 0))
				target.current_defense = maxi(0, target.current_defense + sign * int(effect.get("defense", 0)))
				_check_lethal(target)
				if effect_type == "buff" and not str(effect.get("duration", "")).is_empty():
					target.modifiers.append(effect.duplicate(true))
			return _event("stats_changed", {"source_id": source.instance_id, "target_id": _first_id(targets)})
		"status":
			for target in targets:
				var status := str(effect.get("status", ""))
				if bool(effect.get("active", true)):
					target.statuses[status] = true
				else:
					target.statuses.erase(status)
			return _event("status_changed", {"source_id": source.instance_id, "target_id": _first_id(targets)})
		"draw":
			var draw_player: Variant = _effect_player(effect, source)
			for count in range(int(effect.get("count", 1))):
				_draw(draw_player)
			return _event("card_drawn", {"player_id": draw_player.id})
		"discard":
			for target in targets:
				_move_to_discard(target)
			return _event("card_discarded", {"target_id": _first_id(targets)})
		"create":
			var create_player: Variant = _effect_player(effect, source)
			var definition: Dictionary = definitions[str(effect.definition_id)]
			var card := CardInstance.from_definition(definition, create_player.id, _next_instance_id(create_player.id))
			_move_to_destination(card, create_player, str(effect.get("destination", "hand")))
			return _event("card_created", {"player_id": create_player.id, "instance_id": card.instance_id})
		"copy":
			var copy_player: Variant = _effect_player(effect, source)
			var copied := CardInstance.from_definition(_definition_for(targets[0]), copy_player.id, _next_instance_id(copy_player.id))
			_move_to_destination(copied, copy_player, str(effect.get("destination", "hand")))
			return _event("card_copied", {"source_id": targets[0].instance_id, "instance_id": copied.instance_id})
		"destroy":
			for target in targets:
				_destroy(target)
			return _event("card_destroyed", {"target_id": _first_id(targets)})
		"return":
			for target in targets:
				_move_to_destination(target, _player_for(target.owner_id, true), "hand")
			return _event("card_returned", {"target_id": _first_id(targets)})
		"retreat":
			for target in targets:
				_retreat(target)
			return _event("unit_retreated", {"target_id": _first_id(targets)})
		"credit":
			var credit_player: Variant = _effect_player(effect, source)
			credit_player.credit = maxi(0, credit_player.credit + int(effect.get("amount", 0)))
			return _event("credit_changed", {"player_id": credit_player.id, "credit": credit_player.credit})
		"credit_slots":
			var slots_player: Variant = _effect_player(effect, source)
			slots_player.credit_slots = clampi(slots_player.credit_slots + int(effect.get("amount", 0)), 0, GameConstants.MAX_CREDITS)
			return _event("credit_slots_changed", {"player_id": slots_player.id, "credit_slots": slots_player.credit_slots})
		"replace_event":
			var pending: Dictionary = effect.context.get("event", {})
			for key in effect.get("changes", {}):
				pending[key] = effect.changes[key]
			return _event("event_replaced", {"source_id": source.instance_id})
	return _event("effect_resolved", {"source_id": source.instance_id, "effect_type": effect_type})


func _trigger_countermeasure(counter: CardInstance) -> Dictionary:
	var owner = _player_for(counter.owner_id, true)
	owner.active_countermeasures.erase(counter)
	owner.hand.erase(counter)
	counter.countermeasure_active = false
	counter.face_down = false
	counter.zone = "discard"
	counter.slot = -1
	owner.discard.append(counter)
	return _event("countermeasure_triggered", {"player_id": owner.id, "instance_id": counter.instance_id})


func _draw(player) -> void:
	if player.deck.is_empty():
		player.headquarters.current_defense = maxi(0, player.headquarters.current_defense - player.fatigue)
		player.fatigue += 1
		_check_lethal(player.headquarters)
		return
	var card: CardInstance = player.deck.pop_back()
	if player.hand.size() >= GameConstants.MAX_HAND_SIZE:
		card.zone = "discard"
		player.discard.append(card)
		return
	card.zone = "hand"
	player.hand.append(card)


func _destroy(card: CardInstance) -> void:
	if card.category != "Unit" or card.zone == "discard":
		return
	_remove_from_zone(card)
	card.zone = "discard"
	card.slot = -1
	_player_for(card.owner_id, true).discard.append(card)
	var death := _prepare_trigger("death", {"source_id": card.instance_id, "actor_id": card.owner_id, "target_ids": []})
	if not death.valid:
		state.phase = "invalid"
		return
	for index in range(death.queue.size() - 1, -1, -1):
		queue.push_front(death.queue[index])


func _retreat(card: CardInstance) -> void:
	var player = _player_for(card.owner_id, true)
	var slot := _first_open_support_slot(player)
	if slot < 0:
		return
	state.frontline[card.slot] = null
	player.support_line[slot] = card
	card.zone = "support_line"
	card.slot = slot
	_queue_frontline_recalculation(card)


func _move_to_discard(card: CardInstance) -> void:
	_remove_from_zone(card)
	card.zone = "discard"
	card.slot = -1
	_player_for(card.owner_id, true).discard.append(card)


func _move_to_destination(card: CardInstance, player, destination: String) -> void:
	_remove_from_zone(card)
	card.slot = -1
	if destination == "deck":
		card.zone = "deck"
		player.deck.append(card)
	else:
		card.zone = "hand"
		player.hand.append(card)


func _remove_from_zone(card: CardInstance) -> void:
	var removed_from_frontline := false
	var player = _player_for(card.owner_id, true)
	player.deck.erase(card)
	player.hand.erase(card)
	player.discard.erase(card)
	player.active_countermeasures.erase(card)
	for slot in range(player.support_line.size()):
		if player.support_line[slot] == card:
			player.support_line[slot] = null
	for slot in range(state.frontline.size()):
		if state.frontline[slot] == card:
			state.frontline[slot] = null
			removed_from_frontline = true
	if removed_from_frontline:
		_queue_frontline_recalculation(card)


func _effect_player(effect: Dictionary, source: CardInstance):
	var selector := str(effect.get("player", "owner"))
	return _player_for(source.owner_id, selector != "enemy")


func _player_for(owner_id: String, owner: bool):
	return state.players[owner_id] if owner else state.players[_other_player_id(owner_id)]


func _cards_for_ids(ids: Array) -> Array[CardInstance]:
	var cards: Array[CardInstance] = []
	for id_value in ids:
		var card := _find_card(str(id_value))
		if card != null:
			cards.append(card)
	return cards


func _cards_for_owner(owner_id: String) -> Array[CardInstance]:
	var player = _player_for(owner_id, true)
	var cards: Array[CardInstance] = [player.headquarters]
	for collection in [player.deck, player.hand, player.support_line, player.discard, state.frontline]:
		for card in collection:
			if card != null and card.owner_id == owner_id:
				cards.append(card)
	return cards


func _find_card(instance_id: String) -> CardInstance:
	for player_id in state.players:
		var player = state.players[player_id]
		if player.headquarters.instance_id == instance_id:
			return player.headquarters
		for collection in [player.deck, player.hand, player.support_line, player.discard, state.frontline]:
			for card in collection:
				if card != null and card.instance_id == instance_id:
					return card
	return null


func _definition_for(card: CardInstance) -> Dictionary:
	if definitions.has(card.definition_id):
		return definitions[card.definition_id]
	return {
		"id": card.definition_id,
		"title": card.title,
		"category": card.category,
		"unit_type": card.unit_type,
		"attack": card.base_attack,
		"defense": card.base_defense,
		"deployment_cost": card.deployment_cost,
		"operation_cost": card.operation_cost,
		"keywords": card.keywords,
		"abilities": card.abilities,
	}


func _next_instance_id(owner_id: String) -> String:
	var index := 0
	while _find_card("%s-effect-%03d" % [owner_id, index]) != null:
		index += 1
	return "%s-effect-%03d" % [owner_id, index]


func _first_open_support_slot(player) -> int:
	for slot in range(player.support_line.size()):
		if player.support_line[slot] == null:
			return slot
	return -1


func _update_frontline_control() -> void:
	state.frontline_controller_id = ""
	for card in state.frontline:
		if card != null:
			state.frontline_controller_id = card.owner_id
			return


func _queue_frontline_recalculation(lost_card: CardInstance) -> void:
	var previous_controller: String = state.frontline_controller_id
	_update_frontline_control()
	if previous_controller != state.frontline_controller_id:
		queue.push_front({
			"type": "frontline_changed",
			"previous_controller_id": previous_controller,
			"controller_id": state.frontline_controller_id,
		})
		_enqueue_trigger("frontline_lost", {
			"source_id": lost_card.instance_id,
			"actor_id": _resolving_actor_id,
			"target_ids": [lost_card.instance_id],
		})


func _check_lethal(card: CardInstance) -> void:
	if card.current_defense > 0:
		return
	if card.category == "Unit":
		_destroy(card)
	elif card.category == "Headquarters" and state.winner_id.is_empty():
		state.winner_id = _other_player_id(card.owner_id)
		state.phase = "complete"
		queue.push_front({"type": "match_ended", "winner_id": state.winner_id, "loser_id": card.owner_id})
		_enqueue_trigger("hq_lethal", {
			"source_id": card.instance_id,
			"actor_id": _resolving_actor_id,
			"target_ids": [card.instance_id],
		})


func _enqueue_trigger(trigger: String, context: Dictionary) -> void:
	var prepared := _prepare_trigger(trigger, context)
	if not prepared.valid:
		state.phase = "invalid"
		return
	for index in range(prepared.queue.size() - 1, -1, -1):
		queue.push_front(prepared.queue[index])


func _validate_retreat_reservations(effects: Array[Dictionary]) -> Dictionary:
	var reserved := {}
	for effect in effects:
		if str(effect.get("type", "")) != "retreat":
			continue
		for target_id in effect.get("target_ids", []):
			var target := _find_card(str(target_id))
			if target == null or target.zone != "frontline":
				return {"valid": false, "code": "invalid_origin"}
			var owner_id := target.owner_id
			var capacity := _open_support_slots(_player_for(owner_id, true))
			var used := int(reserved.get(owner_id, 0))
			if used >= capacity:
				return {"valid": false, "code": "support_line_full"}
			reserved[owner_id] = used + 1
	return {"valid": true}


func _resolved_target_ids(effect: Dictionary) -> Array:
	if str(effect.get("target_selector", "")) != "random_enemy_unit":
		return effect.get("target_ids", [])
	var candidates := _public_enemy_units(_find_card(str(effect.get("source_id", ""))).owner_id)
	var selected: Array[String] = []
	for ignored in range(int(effect.get("target_count", 1))):
		var index := rng.randi_range(0, candidates.size() - 1)
		selected.append(candidates[index])
		candidates.remove_at(index)
	state.rng_state = rng.state
	return selected


func _public_enemy_units(owner_id: String) -> Array[String]:
	var candidates: Array[String] = []
	for card in _cards_for_owner(_other_player_id(owner_id)):
		if card.category == "Unit" and (card.zone == "support_line" or card.zone == "frontline"):
			candidates.append(card.instance_id)
	return candidates


func _open_support_slots(player) -> int:
	var count := 0
	for card in player.support_line:
		if card == null:
			count += 1
	return count


func _is_public_target(card: CardInstance) -> bool:
	return card.zone == "support_line" or card.zone == "frontline" or card.zone == "headquarters"


func _string_ids(values: Array) -> Array[String]:
	var ids: Array[String] = []
	for value in values:
		ids.append(str(value))
	return ids


func _first_id(cards: Array[CardInstance]) -> String:
	return cards[0].instance_id if not cards.is_empty() else ""


func _other_player_id(player_id: String) -> String:
	return "opponent" if player_id == "player" else "player"


func _event(event_type: String, payload: Dictionary) -> Dictionary:
	state.sequence += 1
	var event := {"type": event_type, "sequence": state.sequence}
	for key in payload:
		event[key] = payload[key]
	return event
