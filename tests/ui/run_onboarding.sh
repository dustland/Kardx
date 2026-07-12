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

if [ "${ONBOARDING_RUNNER_APPEND_LINE+x}" = x ]; then
	printf '%s\n' "$ONBOARDING_RUNNER_APPEND_LINE" >>"$output"
fi

cat "$output"

if [ "$status" -ne 0 ]; then
	exit "$status"
fi

engine_count="$(grep -Ec '^Godot Engine v[0-9][0-9.]*\.stable\.official\.[0-9A-Za-z]* - https://godotengine\.org$' "$output")"
ca_error_count="$(grep -cF 'ERROR: Condition "ret != noErr" is true. Returning: ""' "$output")"
ca_location_count="$(grep -cF '   at: get_system_ca_certificates (platform/macos/os_macos.mm:1035)' "$output")"
if [ "$engine_count" -ne 1 ] || [ "$ca_error_count" -ne "$ca_location_count" ] || [ "$ca_error_count" -gt 1 ]; then
	echo "FAIL: onboarding runner emitted an invalid engine diagnostic sequence" >&2
	exit 1
fi

sed \
	-e '/^Godot Engine v[0-9][0-9.]*\.stable\.official\.[0-9A-Za-z]* - https:\/\/godotengine\.org$/d' \
	-e '/^$/d' \
	-e '/^ERROR: Condition "ret != noErr" is true\. Returning: ""$/d' \
	-e '/^   at: get_system_ca_certificates (platform\/macos\/os_macos\.mm:1035)$/d' \
	-e '/^OnboardingStore: ignoring corrupt state at user:\/\/test-onboarding\.json$/d' \
	-e '/^OnboardingStore: unable to write state at user:\/\/missing-parent\/test-onboarding\.json$/d' \
	-e '/^PASS onboarding model and persistence$/d' \
	"$output" >"$filtered"

if [ -s "$filtered" ]; then
	echo "FAIL: onboarding runner emitted unexpected output" >&2
	cat "$filtered" >&2
	exit 1
fi

if [ "$(grep -c '^PASS onboarding model and persistence$' "$output")" -ne 1 ]; then
	echo "FAIL: onboarding runner did not emit exactly one PASS sentinel" >&2
	exit 1
fi
