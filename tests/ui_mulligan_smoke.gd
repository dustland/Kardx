extends SceneTree

func _initialize() -> void:
	var view = preload("res://scenes/ui/mulligan_view.tscn").instantiate()
	root.add_child(view)
	await process_frame
	assert(view.has_node("%HandRow"), "mulligan hand row exists")
	assert(view.has_node("%ConfirmButton"), "mulligan confirm exists")
	print("Mulligan UI smoke passed")
	quit(0)
