# Archived Unity MVC Migration Plan

> **Archived historical document.** This plan describes the retired Unity codebase and is not current implementation or onboarding guidance. OpenCards now uses Godot 4.7; see the root `README.md` for current development instructions and `Docs/README.md` for documentation status.

## Historical Folder Structure

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

| Earlier name | Migrated Unity name |
| --- | --- |
| `OpenCards.Core` | `OpenCards.Models` |
| `OpenCards.UI` | `OpenCards.Views` |
| UI drag handlers | `OpenCards.Controllers.DragHandlers` |
| ViewManager / ViewRegistry | `OpenCards.Managers` |

Sub-namespaces follow folder layout, for example `OpenCards.Models.Cards`, `OpenCards.Views.Match`.

## Migration Notes

The original plan assumed separate `Models/Players` and `Models/Game` folders. The implemented layout places `Player` and `MatchManager` under `Models/Match/` and keeps all zone types under `Models/Cards/`.

Legacy per-action drag handlers (`UnitDeployDragHandler`, `OrderDeployDragHandler`, `AbilityDragHandler`) were replaced by a single `CardDragController` with mode resolution in `CardDragCapability`.

## Historical Post-Migration Conventions

1. **Zone changes** go through `CardCollection.AddCard` / `RemoveCard`, including `Battlefield` slot operations.
2. **Combat validation** lives in `CombatRules`; `MatchManager` adds credit checks and orchestrates resolution.
3. **UI updates** subscribe to `MatchManager` events; views do not mutate model state directly.
4. **Card views** are tracked by `ViewRegistry` and created through `ViewManager`.

## Historical Testing Notes

The former Unity validation checklist is obsolete. Use the Godot validation and export commands in the root `README.md`.
