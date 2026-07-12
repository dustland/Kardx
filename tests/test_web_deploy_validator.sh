#!/bin/sh
set -eu

validator="tests/validate_web_deploy.sh"
workflow=".github/workflows/webgl-pages.yml"
tmp_dir="$(mktemp -d "${TMPDIR:-/tmp}/opencards-web-validator.XXXXXX")"
trap 'rm -rf "$tmp_dir"' EXIT

expect_rejected() {
  name="$1"
  pattern="$2"
  replacement="$3"
  mutated="$tmp_dir/$name.yml"
  sed "s|$pattern|$replacement|" "$workflow" > "$mutated"
  if sh "$validator" "$mutated" export_presets.cfg >/dev/null 2>&1; then
    echo "validator accepted invalid mutation: $name" >&2
    exit 1
  fi
}

sh "$validator"
expect_rejected trigger 'branches: \[main\]' 'branches: [develop]'
expect_rejected editor_hash '0b1a6c54c2c619c12e169fe9241edda4b81080b519451cec2984bf0d2c6cb73c' 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
expect_rejected deploy_action 'actions/deploy-pages@v4' 'actions/deploy-pages@v3'
expect_rejected artifact 'test -s builds/web/index.wasm' 'test -f builds/web/*.wasm'

printf 'Web deployment validator mutation tests passed.\n'
