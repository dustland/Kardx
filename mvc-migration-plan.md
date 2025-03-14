# MVC Migration Plan for Kardx

## Folder Structure
```
/Assets/Scripts/
  /Models/            # All data models and game logic
    /Cards/           # Card data models, collection structures
    /Players/         # Player data models
    /Game/            # Game state and rules
  /Views/             # UI components that render game state
    /Cards/           # CardView and specialized card views
    /Battlefield/     # Battlefield visualization
    /Hand/            # Hand visualization  
    /Match/           # Overall match visualization
  /Controllers/       # Components that handle user input
    /DragHandlers/    # Drag & drop controllers
    /InputHandlers/   # Other forms of input handling
  /Managers/          # Cross-cutting service components like ViewManager
  /Utils/             # Shared utilities
```

## Namespace Updates
When moving files, update the namespace according to this pattern:
- `Kardx.Core` → `Kardx.Models`
- `Kardx.UI` → `Kardx.Views` 
- Drag handlers from UI → `Kardx.Controllers`
- Additional subfolders appended to namespace: `Kardx.Models.Cards`, `Kardx.Views.Battlefield`, etc.

## Migration Phases

### Phase 1: Models
1. Move Core/Card.cs → Models/Cards/Card.cs
2. Move Core/Deck.cs → Models/Cards/Deck.cs
3. Move Core/Hand.cs → Models/Cards/Hand.cs
4. Move Core/Player.cs → Models/Players/Player.cs
5. Move Core/MatchManager.cs → Models/Game/MatchManager.cs

### Phase 2: Views
1. Move UI/CardView.cs → Views/Cards/CardView.cs
2. Move UI/HandView.cs → Views/Hand/HandView.cs
3. Move UI/BaseBattlefieldView.cs → Views/Battlefield/BaseBattlefieldView.cs
4. Move UI/PlayerBattlefieldView.cs → Views/Battlefield/PlayerBattlefieldView.cs
5. Move UI/OpponentBattlefieldView.cs → Views/Battlefield/OpponentBattlefieldView.cs
6. Move UI/MatchView.cs → Views/Match/MatchView.cs

### Phase 3: Controllers
1. Move UI/UnitDeployDragHandler.cs → Controllers/DragHandlers/UnitDeployDragHandler.cs
2. Move UI/OrderDeployDragHandler.cs → Controllers/DragHandlers/OrderDeployDragHandler.cs
3. Move UI/AbilityDragHandler.cs → Controllers/DragHandlers/AbilityDragHandler.cs

### Phase 4: Managers
1. Move UI/ViewManager.cs → Managers/ViewManager.cs
2. Move UI/ViewRegistry.cs → Managers/ViewRegistry.cs

## Testing Strategy
After each file migration:
1. Fix any missing references or imports
2. Compile to check for errors
3. Ensure functionality still works

## Recommended Migration Order
Migrate from the bottom up in the dependency chain:
1. Models first (least dependencies)
2. Controllers next (depend on Models)
3. Views after (depend on Models and Controllers)
4. Managers last (depend on all other layers)
