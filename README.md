# OpenCards

OpenCards is now the Godot 4 main project. The original Unity project is preserved on the `unity` branch.

## Run

Open this repository in Godot 4 and press Play. The main scene is `scenes/main.tscn`.

The playable flow is Deck Builder, opening-hand Mulligan, player-versus-AI Match, and Result. Result supports a rematch with the same complete player deck and difficulty or a return to Deck Builder with the current in-memory deck still selected. Artwork copied from the Unity project is in `game_assets/`.

### Controls

- Mouse: select cards and targets, drag cards to legal zones, and activate commands.
- Enter or Space: activate the primary command where supported, including Play, confirm, and Rematch.
- Escape: clear the current Match selection or return from Result to Deck Builder.
- E: end the player turn when End Turn is legal.

## Validate

With Godot 4 installed, run the focused validation commands:

```sh
godot --headless --path . --script tests/data_validation.gd
godot --headless --path . --script tests/test_suite.gd
sh tests/ui/run_task6.sh
```

The strict Task6 runner covers Result contracts and the complete Deck Builder -> Mulligan -> Match -> Result -> Rematch/Builder flow at 1280x720 and true 1024x720. It rejects runtime and teardown diagnostics except the exact known macOS headless system-CA lookup failure.

## Current limitations

- Matches are local player-versus-AI only; there is no network multiplayer.
- Replay data is retained on Result for verification but has no replay-browser UI.
- User deck changes remain in memory until Save is used in Deck Builder.

The `unity` branch contains the complete Unity source and its original project settings.
