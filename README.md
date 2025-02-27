# Kardx

A digital card game built with Unity that simulates physical card game mechanics.

## Overview

Kardx is a turn-based card game where players deploy units to the battlefield, use abilities, and attack opponents to win. The game features:

- A data-driven card system
- Ability-based gameplay mechanics
- Battlefield positioning strategy
- Hand and resource management

## Documentation

For developers who want to understand the project better:

- [Architecture](./Design/Arch.md) - Overview of the game's structure
- [Data System Design](./Design/Data.md) - How card data is organized
- [Ability System Design](./Design/Ability.md) - How card abilities work

## Getting Started

### Prerequisites

- Unity 6 (v6000.0.38f1) - [Download from Unity Hub](https://unity.com/download)
- Basic understanding of C# and Unity concepts

### Required Unity Packages

Install these packages to work with the project:

1. **Windsurf IDE** (recommended editor)
   - Install from Git URL: `https://github.com/Asuta/com.unity.ide.windsurf.git.windsurf.git`
   - Windsurf offers better performance than Cursor IDE

2. **NewtonSoft JSON**
   - Add from Package Manager: `com.unity.nuget.newtonsoft-json`
   - Used for JSON serialization/deserialization

### Code Formatting

C# code is formatted with the CSharpier plugin in Windsurf.

## Project Structure

All code and assets are organized in the `Assets` folder:

### Code Organization

- **`Assets/Scripts/Core`** - Core game logic
  - Card, Player, Board, and Ability classes
  - **`/Acting`** - Ability system implementation (card effects and triggers)
  - **`/Planning`** - Game planning logic

- **`Assets/Scripts/UI`** - User interface components
  - Views connected to Unity UI elements

### Asset Organization

- **`Assets/Sprites`** - Static game sprites
- **`Assets/Resources`** - Dynamically loaded art (card faces, etc.)
- **`Assets/StreamingAssets`** - JSON data files (CardTypes, AbilityTypes)

## Development Tips

- Read the design documents before making changes
- The game uses a data-driven approach - many game elements are defined in JSON
- Check the `CardLoader.cs` and `AbilityType.cs` files to understand how data is loaded
- Use the Unity Console to debug issues during gameplay

## License

This project is licensed under the [MIT](./LICENSE) license.
