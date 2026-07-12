class_name DeckStore
extends RefCounted

const DEFAULT_PATH := "user://decks.json"

var path: String


func _init(storage_path: String = DEFAULT_PATH) -> void:
	path = storage_path


func load_all(shipped_decks: Array) -> Dictionary:
	var result := _index_decks(shipped_decks)
	if not FileAccess.file_exists(path):
		return result
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return result
	var parsed = JSON.parse_string(file.get_as_text())
	if not parsed is Array:
		return result
	for deck_value in parsed:
		if not deck_value is Dictionary:
			continue
		var deck: Dictionary = deck_value
		var deck_id := str(deck.get("id", ""))
		if deck_id.begins_with("user-") and deck.get("cards", null) is Array:
			result[deck_id] = deck.duplicate(true)
	return result


func save_user_decks(decks: Array, shipped_decks: Array = []) -> bool:
	var shipped_ids := _index_decks(shipped_decks)
	var serializable: Array = []
	for deck_value in decks:
		if not deck_value is Dictionary:
			return false
		var deck: Dictionary = deck_value
		var deck_id := str(deck.get("id", ""))
		if not deck_id.begins_with("user-") or shipped_ids.has(deck_id) or not deck.get("cards", null) is Array:
			return false
		serializable.append(deck.duplicate(true))
	var absolute_path := ProjectSettings.globalize_path(path)
	var base_dir := absolute_path.get_base_dir()
	if DirAccess.make_dir_recursive_absolute(base_dir) != OK and not DirAccess.dir_exists_absolute(base_dir):
		return false
	var temp_path := absolute_path + ".tmp"
	var file := FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(serializable, "\t"))
	file.flush()
	file.close()
	if DirAccess.rename_absolute(temp_path, absolute_path) != OK:
		DirAccess.remove_absolute(temp_path)
		return false
	return true


func _index_decks(decks: Array) -> Dictionary:
	var result := {}
	for deck_value in decks:
		if deck_value is Dictionary:
			var deck: Dictionary = deck_value
			var deck_id := str(deck.get("id", ""))
			if not deck_id.is_empty():
				result[deck_id] = deck.duplicate(true)
	return result
