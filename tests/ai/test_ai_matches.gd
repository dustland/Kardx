extends RefCounted


static func run(t) -> void:
	_test_standard_fixtures_complete_and_replay(t)
	_test_difficulty_paths_and_reproducibility(t)
	_test_runner_reports_search_profile(t)
	_test_search_skips_rejected_candidates(t)
	_test_turn_limit_allows_legal_multi_action_progress(t)
	_test_limit_diagnostics_name_replay_artifacts(t)


static func _test_standard_fixtures_complete_and_replay(t) -> void:
	var runner_script = load("res://scripts/ai/ai_match_runner.gd")
	t.assert_true(runner_script != null, "AiMatchRunner script exists")
	if runner_script == null:
		return
	for seed in [1, 7, 42, 90210]:
		var result := _run_match_with_progress(runner_script, seed, "standard", "standard")
		_assert_valid_match(t, result, "standard seed %d" % seed)
		t.assert_true(result.turns <= 300, "standard seed %d finishes within the turn limit" % seed)
		t.assert_true(result.content_diagnostics.is_empty(), "standard seed %d loads valid shipped content" % seed)


static func _test_difficulty_paths_and_reproducibility(t) -> void:
	var runner_script = load("res://scripts/ai/ai_match_runner.gd")
	if runner_script == null:
		return
	for pairing in [["easy", "hard"], ["hard", "easy"]]:
		var result := _run_match_with_progress(runner_script, 42, pairing[0], pairing[1])
		_assert_valid_match(t, result, "%s v %s" % pairing)

	var first := _run_match_with_progress(runner_script, 90210, "easy", "easy")
	var second := _run_match_with_progress(runner_script, 90210, "easy", "easy")
	_assert_valid_match(t, first, "deterministic first result")
	_assert_valid_match(t, second, "deterministic second result")
	t.assert_eq(first.winner, second.winner, "same seed and difficulties preserve winner")
	t.assert_eq(first.state_hash, second.state_hash, "same seed and difficulties preserve terminal hash")
	t.assert_eq(first.action_count, second.action_count, "same seed and difficulties preserve action count")


static func _test_runner_reports_search_profile(t) -> void:
	var runner_script = load("res://scripts/ai/ai_match_runner.gd")
	if runner_script == null:
		return
	var result := _run_match_with_progress(runner_script, 42, "easy", "easy")
	_assert_valid_match(t, result, "profile fixture")
	var totals: Dictionary = result.get("total_search_metrics", {})
	var maximums: Dictionary = result.get("max_search_metrics", {})
	for field in ["action_lists", "candidate_actions", "simulation_attempts", "rejected_simulations"]:
		t.assert_true(
			int(totals.get("player", {}).get(field, -1)) >= 0,
			"profile fixture records player %s" % field
		)
		t.assert_true(
			int(maximums.get("opponent", {}).get(field, -1)) >= 0,
			"profile fixture records opponent %s" % field
		)


static func _test_search_skips_rejected_candidates(t) -> void:
	var runner_script = load("res://scripts/ai/ai_match_runner.gd")
	if runner_script == null:
		return
	var result := _run_match_with_progress(runner_script, 42, "standard", "standard")
	_assert_valid_match(t, result, "search rejection fixture")
	var totals: Dictionary = result.total_search_metrics
	for actor_id in ["player", "opponent"]:
		var metrics: Dictionary = totals.get(actor_id, {})
		t.assert_true(
			int(metrics.get("simulation_attempts", 0)) > int(metrics.get("rejected_simulations", 0)),
			"%s searches past rejected generated candidates" % actor_id
		)
		t.assert_eq(
			int(metrics.get("simulation_attempts", 0)),
			int(result.total_visited_nodes.get(actor_id, -1)),
			"%s counts rejected and accepted simulations against its node budget" % actor_id
		)


static func _test_turn_limit_allows_legal_multi_action_progress(t) -> void:
	var runner_script = load("res://scripts/ai/ai_match_runner.gd")
	if runner_script == null:
		return
	var max_turns := 3
	var result: Dictionary = runner_script.run_match(42, "easy", "easy", max_turns)
	t.assert_eq(result.final_reason, "turn_limit", "short match stops at the turn limit instead of the action limit")
	t.assert_true(
		int(result.action_count) > max_turns * 2,
		"legal setup and multi-action turns exceed the retired two-actions-per-turn global limit"
	)
	t.assert_true(not bool(result.completed), "short turn-limited match is not reported as complete")


static func _test_limit_diagnostics_name_replay_artifacts(t) -> void:
	var runner_script = load("res://scripts/ai/ai_match_runner.gd")
	if runner_script == null:
		return
	var result: Dictionary = runner_script.run_match(7, "easy", "easy", 1)
	for code in ["turn_limit", "action_limit", "no_progress_limit"]:
		var diagnostic: Dictionary = runner_script._limit_diagnostic(result, code, {})
		var replay_path := str(diagnostic.get("replay_path", ""))
		t.assert_true(not replay_path.is_empty(), "%s diagnostic names its replay artifact" % code)
		t.assert_true(FileAccess.file_exists(replay_path), "%s replay artifact is persisted" % code)


static func _run_match_with_progress(runner_script, seed: int, player_difficulty: String, opponent_difficulty: String) -> Dictionary:
	print("AI match start seed=%d player=%s opponent=%s" % [seed, player_difficulty, opponent_difficulty])
	var started_at := Time.get_ticks_msec()
	var result: Dictionary = runner_script.run_match(seed, player_difficulty, opponent_difficulty)
	var elapsed_ms := Time.get_ticks_msec() - started_at
	print(
		"AI match done seed=%d player=%s opponent=%s elapsed_ms=%d phase=%s winner=%s illegal=%d replay=%s" % [
			seed,
			player_difficulty,
			opponent_difficulty,
			elapsed_ms,
			str(result.get("phase", "")),
			str(result.get("winner", "")),
			int(result.get("illegal_actions", -1)),
			str(bool(result.get("replay_matches", false))),
		]
	)
	return result


static func _assert_valid_match(t, result: Dictionary, label: String) -> void:
	t.assert_true(bool(result.get("completed", false)), "%s completes" % label)
	t.assert_eq(str(result.get("phase", "")), "complete", "%s reaches complete phase" % label)
	t.assert_true(not str(result.get("winner", "")).is_empty(), "%s reports a winner" % label)
	t.assert_eq(int(result.get("illegal_actions", -1)), 0, "%s has no illegal actions" % label)
	t.assert_true(bool(result.get("replay_matches", false)), "%s replay reaches the same terminal state" % label)
	t.assert_true(not str(result.get("replay_integrity_hash", "")).is_empty(), "%s reports replay integrity" % label)
	t.assert_true(result.has("replay"), "%s returns replay data" % label)
	t.assert_true(int(result.get("max_effect_queue", -1)) >= 0, "%s records effect queue usage" % label)
	t.assert_true(int(result.get("max_events", -1)) >= 0, "%s records event usage" % label)
	t.assert_true(int(result.get("max_visited_nodes", {}).get("player", -1)) >= 0, "%s records player search work" % label)
	t.assert_true(int(result.get("total_visited_nodes", {}).get("opponent", -1)) >= 0, "%s records opponent search work" % label)
