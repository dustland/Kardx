extends SceneTree

const SEED := 42
const PLAYER_DIFFICULTY := "standard"
const OPPONENT_DIFFICULTY := "standard"
const MAX_TURNS := 300
const AiMatchRunner = preload("res://scripts/ai/ai_match_runner.gd")


func _init() -> void:
	var arguments := OS.get_cmdline_user_args()
	var seed := int(arguments[0]) if arguments.size() > 0 else SEED
	var player_difficulty := str(arguments[1]) if arguments.size() > 1 else PLAYER_DIFFICULTY
	var opponent_difficulty := str(arguments[2]) if arguments.size() > 2 else OPPONENT_DIFFICULTY
	var max_turns := int(arguments[3]) if arguments.size() > 3 else MAX_TURNS
	var result: Dictionary = AiMatchRunner.run_match(seed, player_difficulty, opponent_difficulty, max_turns)
	var output := result.duplicate(true)
	output.erase("replay")
	print(JSON.stringify(output))
	var valid := bool(result.completed) and int(result.illegal_actions) == 0 and bool(result.replay_matches)
	quit(0 if valid else 1)
