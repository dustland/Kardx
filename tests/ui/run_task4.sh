#!/bin/sh
set -u

output="$(mktemp -t opencards-task4.XXXXXX)"
trap 'rm -f "$output"' EXIT

HOME=/tmp godot --headless --path . --script tests/ui/run_task4.gd >"$output" 2>&1
status=$?
cat "$output"

if [ "$status" -ne 0 ]; then
	exit "$status"
fi

if grep -E 'SCRIPT ERROR|Node not found|Invalid call|not in tree|[Ww]ere leaked|instances were leaked|resources still in use' "$output" >/dev/null; then
	echo "FAIL: focused mulligan runner emitted runtime or teardown errors" >&2
	exit 1
fi

exit 0
