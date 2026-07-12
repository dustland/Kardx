class_name DeckStore
extends RefCounted

const DEFAULT_PATH := "user://decks.json"

var path: String
var last_error := ""


func _init(storage_path: String = DEFAULT_PATH) -> void:
	path = storage_path


func load_all(shipped_decks: Array) -> Dictionary:
	last_error = ""
	var result := _index_decks(shipped_decks)
	if not FileAccess.file_exists(path):
		return result
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		last_error = "Could not read saved decks: %s" % error_string(FileAccess.get_open_error())
		return result
	var json := JSON.new()
	var content := file.get_as_text()
	if file.get_error() != OK:
		last_error = "Could not read saved decks: %s" % error_string(file.get_error())
		return result
	if json.parse(content) != OK:
		last_error = "Saved decks are corrupt: %s" % json.get_error_message()
		return result
	var parsed = json.data
	if not parsed is Array:
		last_error = "Saved decks are corrupt: expected a deck list"
		return result
	for deck_value in parsed:
		if not deck_value is Dictionary:
			last_error = "Saved decks are corrupt: ignored an invalid deck entry"
			continue
		var deck: Dictionary = deck_value
		var deck_id := str(deck.get("id", ""))
		if deck_id.begins_with("user-") and deck.get("cards", null) is Array:
			result[deck_id] = deck.duplicate(true)
		else:
			last_error = "Saved decks are corrupt: ignored an invalid user deck"
	return result


func save_user_decks(decks: Array, shipped_decks: Array = []) -> bool:
	last_error = ""
	var shipped_ids := _index_decks(shipped_decks)
	var serializable: Array = []
	for deck_value in decks:
		if not deck_value is Dictionary:
			last_error = "Save failed: invalid deck data"
			return false
		var deck: Dictionary = deck_value
		var deck_id := str(deck.get("id", ""))
		if not deck_id.begins_with("user-") or shipped_ids.has(deck_id) or not deck.get("cards", null) is Array:
			last_error = "Save failed: only valid user decks can be written"
			return false
		serializable.append(deck.duplicate(true))
	var absolute_path := ProjectSettings.globalize_path(path)
	var base_dir := absolute_path.get_base_dir()
	if DirAccess.make_dir_recursive_absolute(base_dir) != OK and not DirAccess.dir_exists_absolute(base_dir):
		last_error = "Save failed: could not create deck directory"
		return false
	var temp_path := absolute_path + ".tmp"
	var file := FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		last_error = "Save failed: could not open temporary file: %s" % error_string(FileAccess.get_open_error())
		return false
	if not file.store_string(JSON.stringify(serializable, "\t")):
		last_error = "Save failed while writing temporary file"
		file.close()
		DirAccess.remove_absolute(temp_path)
		return false
	file.flush()
	if file.get_error() != OK:
		last_error = "Save failed while flushing temporary file: %s" % error_string(file.get_error())
		file.close()
		DirAccess.remove_absolute(temp_path)
		return false
	file.close()
	if DirAccess.rename_absolute(temp_path, absolute_path) != OK:
		last_error = "Save failed while replacing saved decks"
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
