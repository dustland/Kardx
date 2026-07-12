#!/bin/sh
set -u

output="$(mktemp -t opencards-onboarding.XXXXXX)"
filtered="$(mktemp -t opencards-onboarding-filtered.XXXXXX)"
trap 'rm -f "$output" "$filtered"' EXIT

HOME=/tmp/opencards-onboarding godot --headless --path . --script tests/ui/run_onboarding.gd >"$output" 2>&1 &
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

cat "$output"

if [ "$status" -ne 0 ]; then
	exit "$status"
fi

sed \
	-e '/^ERROR: Condition "ret != noErr" is true\. Returning: ""$/d' \
	-e '/^   at: get_system_ca_certificates (platform\/macos\/os_macos\.mm:1035)$/d' \
	"$output" >"$filtered"

if grep -E 'ERROR:|SCRIPT ERROR|Node not found|Invalid call|not in tree|[Ww]ere leaked|instances were leaked|resources still in use|ObjectDB instances leaked' "$filtered" >/dev/null; then
	echo "FAIL: onboarding runner emitted runtime or teardown errors" >&2
	exit 1
fi

grep -q '^PASS onboarding model and persistence$' "$filtered"
