# MVC Migration Plan for OpenCards

> **Status: Completed.** The codebase now follows the structure below. This document is kept as a historical reference and onboarding map.

## Current Folder Structure

```
/Assets/Scripts/
  /Models/            # Data models and match rules
    /Cards/           # Card, CardType, CardCollection, Hand, Deck, Battlefield, DiscardPile
    /Match/           # Board, Player, MatchManager
    /Abilities/       # Ability type definitions
  /Views/             # UI components that render game state
    /Cards/           # CardView, CardDetailView
    /Hand/            # PlayerHandView, OpponentHandView
    /Match/           # MatchView, battlefield views, card slots
    /Home/            # Home screen
  /Controllers/       # Input handling
    /DragHandlers/    # Unified drag workflow (CardDragController, CardDropResolver, etc.)
  /Managers/          # ViewManager, ViewRegistry, TabManager
  /Acting/            # AbilitySystem runtime
  /Planning/          # StrategyPlanner, AI providers
  /Utils/             # CombatRules, CardLoader, GameStateValidator, etc.
  /Debugging/         # Editor/runtime debug helpers
```

## Namespace Map

| Legacy | Current |
| --- | --- |
| `OpenCards.Core` | `OpenCards.Models` |
| `OpenCards.UI` | `OpenCards.Views` |
| UI drag handlers | `OpenCards.Controllers.DragHandlers` |
| ViewManager / ViewRegistry | `OpenCards.Managers` |

Sub-namespaces follow folder layout, for example `OpenCards.Models.Cards`, `OpenCards.Views.Match`.

## Migration Notes

The original plan assumed separate `Models/Players` and `Models/Game` folders. The implemented layout places `Player` and `MatchManager` under `Models/Match/` and keeps all zone types under `Models/Cards/`.

Legacy per-action drag handlers (`UnitDeployDragHandler`, `OrderDeployDragHandler`, `AbilityDragHandler`) were replaced by a single `CardDragController` with mode resolution in `CardDragCapability`.

## Key Post-Migration Conventions

1. **Zone changes** go through `CardCollection.AddCard` / `RemoveCard`, including `Battlefield` slot operations.
2. **Combat validation** lives in `CombatRules`; `MatchManager` adds credit checks and orchestrates resolution.
3. **UI updates** subscribe to `MatchManager` events; views do not mutate model state directly.
4. **Card views** are tracked by `ViewRegistry` and created through `ViewManager`.

## Testing Checklist

After structural changes:

1. Fix missing references or imports
2. Compile in Unity
3. Playtest deploy, move, attack, order play, and countermeasure flows in `BattleScene`
