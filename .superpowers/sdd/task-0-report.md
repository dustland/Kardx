# Task 0 Report: Godot 4.7 Button Theme Constant Compatibility

## Change

- Modified only `scripts/main.gd`.
- Replaced the invalid `Button.icon_max_width = 62` assignment with:

  ```gdscript
  button.add_theme_constant_override("icon_max_width", 62)
  ```

- Preserved the value `62` and all surrounding card-browser behavior.

## Verification

Focused data validation, run from the worktree:

```text
HOME=/tmp/opencards-godot-home godot --headless --path . --script tests/data_validation.gd
```

Result: passed. Output included `Validated 14 cards and 11 abilities`.

Boot smoke, run from the worktree:

```text
HOME=/tmp/opencards-godot-home godot --headless --path . --quit-after 2
```

Result: the Task 0 `icon_max_width` script error and leak warnings are gone. Godot 4.7 on this macOS host emits an unrelated startup error on every headless run:

```text
ERROR: Condition "ret != noErr" is true. Returning: ""
at: get_system_ca_certificates (platform/macos/os_macos.mm:1035)
```

The prescribed commands without the isolated `HOME` crash before project execution because Godot cannot open its `user://logs` file in this environment. The isolated-home run reaches and boots the project, but the certificate error means the brief's literal no-`ERROR` criterion cannot be reported as fully clean.

Additional checks:

```text
git diff --check
```

Result: passed.

## Commit

Commit message: `fix: support Godot 4.7 button icon sizing`

## Concerns

- Headless boot remains formally blocked by the host/runtime certificate error above, unrelated to the changed line. No Task 0 script error or leak warning remains.
