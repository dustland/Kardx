#!/bin/sh
set -eu

workflow=".github/workflows/webgl-pages.yml"
export_preset="export_presets.cfg"

fail() {
  printf 'web deployment validation failed: %s\n' "$1" >&2
  exit 1
}

require_text() {
  grep -Fq -- "$2" "$1" || fail "$1 is missing: $2"
}

reject_text() {
  if grep -Fiq -- "$2" "$1"; then
    fail "$1 contains legacy or forbidden text: $2"
  fi
}

if command -v ruby >/dev/null 2>&1; then
  ruby -e 'require "yaml"; YAML.parse_file(ARGV.fetch(0))' "$workflow" \
    || fail "$workflow is not valid YAML"
elif command -v python3 >/dev/null 2>&1 && python3 -c 'import yaml' >/dev/null 2>&1; then
  python3 -c 'import sys, yaml; yaml.safe_load(open(sys.argv[1], encoding="utf-8"))' "$workflow" \
    || fail "$workflow is not valid YAML"
fi

require_text "$workflow" "push:"
require_text "$workflow" "branches: [main]"
require_text "$workflow" "workflow_dispatch:"
require_text "$workflow" "contents: read"
require_text "$workflow" "pages: write"
require_text "$workflow" "id-token: write"
require_text "$workflow" "group: pages"
require_text "$workflow" "GODOT_VERSION: \"4.7\""
require_text "$workflow" "Godot_v\${GODOT_VERSION}-stable_linux.x86_64.zip"
require_text "$workflow" "Godot_v\${GODOT_VERSION}-stable_export_templates.tpz"
require_text "$workflow" "actions/cache@v4"
require_text "$workflow" "godot --headless --path . --editor --quit"
require_text "$workflow" "godot --headless --path . --script tests/data_validation.gd"
require_text "$workflow" "godot --headless --path . --script tests/test_suite.gd"
require_text "$workflow" "--export-release Web builds/web/index.html"
require_text "$workflow" "actions/configure-pages@v5"
require_text "$workflow" "actions/upload-pages-artifact@v4"
require_text "$workflow" "actions/deploy-pages@v4"
require_text "$workflow" "builds/web/index.html"
require_text "$workflow" "*.js"
require_text "$workflow" "*.wasm"
require_text "$workflow" "*.pck"
reject_text "$workflow" "unity"
reject_text "$workflow" "CNAME"
reject_text "$workflow" "cross-origin"

require_text "$export_preset" 'name="Web"'
require_text "$export_preset" 'export_path="builds/web/index.html"'
require_text "$export_preset" 'variant/extensions_support=false'
require_text "$export_preset" 'variant/thread_support=false'
require_text "$export_preset" 'progressive_web_app/enabled=false'
require_text "$export_preset" 'progressive_web_app/ensure_cross_origin_isolation_headers=false'

printf 'Web deployment configuration is valid.\n'
