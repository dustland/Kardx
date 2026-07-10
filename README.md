# OpenCards

OpenCards is now the Godot 4 main project. The original Unity project is preserved on the `unity` branch.

## Run

Open this repository in Godot 4 and press Play. The main scene is `scenes/main.tscn`.

The current Godot port loads the existing card and ability definitions from `data/cards.json` and `data/abilities.json`, presents the card collection, supports search, and shows card details and abilities. Artwork copied from the Unity project is in `game_assets/`.

## Validate data

With Godot 4 installed, run:

```sh
godot --headless --path . --script tests/data_validation.gd
```

The `unity` branch contains the complete Unity source and its original project settings.
