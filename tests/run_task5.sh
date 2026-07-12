#!/bin/sh
set -eu

ROOT=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
HOME_DIR=${TASK5_HOME:-/tmp/opencards-task5}
mkdir -p "$HOME_DIR"

run_and_gate() {
	name=$1
	shift
	log=$(mktemp "${TMPDIR:-/tmp}/opencards-task5.XXXXXX")
	if ! HOME="$HOME_DIR" "$@" >"$log" 2>&1; then
		cat "$log"
		rm -f "$log"
		echo "Task5 $name failed" >&2
		exit 1
	fi
	cat "$log"
	if awk '
		$0 == "ERROR: Condition \"ret != noErr\" is true. Returning: \"\"" { next }
		$0 == "   at: get_system_ca_certificates (platform/macos/os_macos.mm:1035)" { next }
		/SCRIPT ERROR:|ERROR:|WARNING:|leaked|ObjectDB/ { bad = 1; print "Unwhitelisted runtime output: " $0 > "/dev/stderr" }
		END { exit bad }
	' "$log"; then
		:
	else
		rm -f "$log"
		exit 1
	fi
	rm -f "$log"
}

cd "$ROOT"
run_and_gate contracts godot --headless --path . --script tests/ui_task5.gd
run_and_gate interaction godot --headless --path . --script tests/ui_task5_interaction.gd
run_and_gate import godot --headless --editor --path . --quit
echo "Task5 strict verification passed"
