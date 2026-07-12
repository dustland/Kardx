#!/bin/sh
set -u

output="$(mktemp -t opencards-task6.XXXXXX)"
trap 'rm -f "$output"' EXIT

for runner in tests/ui/run_task6.gd tests/ui_smoke.gd; do
	HOME=/tmp godot --headless --path . --script "$runner" >>"$output" 2>&1 &
	pid=$!
	(
		sleep 45
		kill "$pid" 2>/dev/null
	) &
	watchdog=$!
	wait "$pid"
	status=$?
	kill "$watchdog" 2>/dev/null
	wait "$watchdog" 2>/dev/null
	if [ "$status" -ne 0 ]; then
		cat "$output"
		exit "$status"
	fi
done

cat "$output"

filtered="$(mktemp -t opencards-task6-filtered.XXXXXX)"
trap 'rm -f "$output" "$filtered"' EXIT
sed \
	-e '/^ERROR: Condition "ret != noErr" is true\. Returning: ""$/d' \
	-e '/^   at: get_system_ca_certificates (platform\/macos\/os_macos\.mm:1035)$/d' \
	"$output" >"$filtered"

if grep -E 'ERROR:|SCRIPT ERROR|Node not found|Invalid call|not in tree|[Ww]ere leaked|instances were leaked|resources still in use|ObjectDB instances leaked' "$filtered" >/dev/null; then
	echo "FAIL: strict Task6 runner emitted runtime or teardown errors" >&2
	exit 1
fi

exit 0
