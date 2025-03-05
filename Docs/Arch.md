# Kardx Architecture: Design Principles and System Structure

[查看简体中文版本](./Arch-CN.md)

_This document provides high-level architectural guidance for the Kardx game, outlining the core design principles, system structure, and component interactions. For detailed information about the data model, see [Data.md](./Data.md)._

## Overview

The Kardx architecture is designed to create a maintainable, testable, and scalable codebase that can evolve with the game's requirements. At its core, the architecture emphasizes clean separation of concerns, unidirectional data flow, and event-driven communication between components.

## Core Architectural Principles

### 1. Separation of Concerns

- **Data Model**: Responsible for maintaining game state (e.g., `Card`, `Player`, `MatchManager`)
- **Game Logic**: Orchestrates game flow and rules (e.g., `StrategyPlanner`, `Decision`)
- **UI Layer**: Responsible for visualizing the game state (e.g., `CardView`, `MatchView`)
- **Clear Boundaries**: The data model should never be concerned with how it's displayed, and the UI should never modify the data model directly

### 2. Unidirectional Data Flow

- Data flows in one direction: from the data model to the UI
- UI components reflect the state of the data model, not the other way around
- Changes to the game state should only happen through well-defined methods in the data model
- The UI reacts to changes in the data model through the event system

### 3. Event-Driven Architecture

- The data model notifies the UI of state changes through events
- UI components subscribe to these events and update accordingly
- This decouples the data model from the UI, making both more maintainable
- Events help orchestrate complex game flows without tight coupling
- **To maintain simplicity, only `MatchManager` and `MatchView` serve as event senders**
- **All other data classes and view classes are aggregated to these two key classes with simple method calls**
- **This concentrated approach prevents event proliferation and maintains clear communication pathways**

### 4. Type System Consistency

- Consistent use of types across the codebase (e.g., string IDs instead of int for card identifiers)
- Immutable collection interfaces (e.g., `IReadOnlyList<T>`) for public APIs to prevent unintended modifications
- Type-safe enumerations (e.g., `DecisionType`, `CardType`) to represent well-defined states
- Judicious use of Generic collections balancing between performance and clarity

## System Architecture

```
┌─────────────────────────────────────────┐
│              User Interface             │
│  ┌─────────────┐       ┌─────────────┐  │
│  │  MatchView  │       │  CardView   │  │
│  └─────────────┘       └─────────────┘  │
└───────────────┬─────────────┬───────────┘
                │             │
                │ Events      │ Reflects
                │             │
┌───────────────▼─────────────▼───────────┐
│               Game Logic                │
│  ┌─────────────┐       ┌─────────────┐  │
│  │MatchManager │       │StrategyPlanner│ │
│  └─────────────┘       └─────────────┘  │
└───────────────┬─────────────┬───────────┘
                │             │
                │ Manages     │ Operates on
                │             │
┌───────────────▼─────────────▼───────────┐
│               Data Model                │
│  ┌─────────────┐       ┌─────────────┐  │
│  │    Card     │       │   Player    │  │
│  └─────────────┘       └─────────────┘  │
└─────────────────────────────────────────┘
```

## Key Components

### Data Model Layer

1. **Card**: Represents a playable card in the game

   - Encapsulates both static data (CardType) and runtime state
   - Uses Guid for unique instance identification
   - Maintains its own state (FaceDown, CurrentDefense, etc.)

2. **Player**: Manages a player's state including cards and resources

   - Provides read-only collection views (Hand, Battlefield) to prevent external modification
   - Enforces game rules at the data level (e.g., max hand size)
   - Contains methods for all valid player actions (DrawCard, DeployCard, etc.)

3. **Board**: Represents the complete game state for a match
   - Tracks turn information and current player
   - Acts as a container for both players
   - Provides controlled access to game state elements

### Game Logic Layer

1. **MatchManager**: Orchestrates the overall game flow

   - Manages turn transitions and game state progression
   - Exposes events for major game state changes
   - Coordinates between player actions and UI updates

2. **StrategyPlanner**: Handles AI and planning systems

   - Executes strategies via coroutines to integrate with Unity's frame system
   - Plans and executes sequences of decisions
   - Provides extension points for different AI strategies

3. **Decision**: Represents a single action in the game
   - Uses consistent string-based IDs for card references
   - Follows a command pattern approach to encapsulate actions
   - Forms the basis of both player and AI decision-making

### UI Layer

1. **CardView**: Visual representation of a card

   - Updates its display based on the underlying Card model
   - Handles card-specific interactions (e.g., selecting, flipping)
   - Maintains clear separation between visual state and data model

2. **MatchView**: Manages the overall match UI
   - Subscribes to MatchManager events to update the display
   - Orchestrates card UI creation and management
   - Avoids direct data model manipulation

## Implementation Guidelines

### Data Model Classes (e.g., `Card`, `Player`, `MatchManager`)

1. **Encapsulate State**:

   ```csharp
   // Good: State is encapsulated with private setter
   public bool FaceDown { get; private set; }

   // Good: Public method to modify state
   public void SetFaceDown(bool isFaceDown)
   {
       FaceDown = isFaceDown;
   }
   ```

2. **Single Source of Truth**:

   - Each piece of state should be owned by exactly one component
   - Example: `Player.DeployCard()` is the only place that should set a card's face-down state when deploying

3. **Notify on State Changes**:

   ```csharp
   // Good: Notify listeners when state changes
   public bool DeployCard(Card card, int position)
   {
       // Change state
       if (!currentPlayer.DeployCard(card, position))
           return false;

       // Notify listeners
       OnCardDeployed?.Invoke(card, position);
       return true;
   }
   ```

4. **Use Appropriate Collection Types**:

   ```csharp
   // Good: Private collection with public read-only access
   private readonly List<Card> hand = new();
   public IReadOnlyList<Card> Hand => hand;

   // Good: Using ToList() when manipulating IReadOnlyList in UI
   var position = hand.ToList().IndexOf(card);
   ```

### UI Classes (e.g., `CardView`, `MatchView`)

1. **Reflect, Don't Modify**:

   ```csharp
   // Good: UI reflects the model state
   cardView.SetFaceDown(card.FaceDown);

   // Bad: UI modifies the model state
   card.SetFaceDown(false);
   cardView.SetFaceDown(false);
   ```

2. **Subscribe to Events**:

   ```csharp
   // Good: UI subscribes to model events
   matchManager.OnCardDeployed += HandleCardDeployed;

   private void HandleCardDeployed(Card card, int position)
   {
       // Update UI based on the new state
       // ...
   }
   ```

3. **Initialize with Current State**:

   ```csharp
   // Good: Initialize UI with current model state
   public void Initialize(Card card)
   {
       this.card = card;
       UpdateCardView(); // Reflect the current state
   }
   ```

4. **Use Coroutines for Unity Integration**:
   ```csharp
   // Good: Use coroutines for async operations in Unity
   public IEnumerator ExecuteNextStrategyCoroutine(Board board)
   {
       // Implementation that works with Unity's frame system
       yield return null;
   }
   ```

## Anti-Patterns to Avoid

### 1. Direct State Modification from UI

```csharp
// Bad: UI directly modifies data model
private void HandleCardDeployed(Card card, int position)
{
    card.SetFaceDown(false); // UI should not modify the data model
    cardView.SetFaceDown(false);
}
```

### 2. Redundant State Setting

```csharp
// Bad: Setting state in multiple places
public bool DeployCard(Card card, int position)
{
    currentPlayer.DeployCard(card, position); // Already sets face-down state
    card.SetFaceDown(false); // Redundant, creates potential for inconsistency
    // ...
}
```

### 3. Bidirectional Dependencies

```csharp
// Bad: CardView modifies Card, creating circular dependency
public void SetFaceDown(bool faceDown)
{
    if (card != null)
    {
        card.SetFaceDown(faceDown); // UI modifying data model
    }
    cardBackOverlay.gameObject.SetActive(faceDown);
}
```

### 4. Type Inconsistency

```csharp
// Bad: Mixing int and string IDs creates confusion and errors
public class Decision
{
    public int SourceCardId { get; set; }  // Uses int
    public string TargetCardId { get; set; }  // Uses string
}
```

## Real-World Examples

### Example 1: Card Deployment

**Correct Flow**:

1. User drags a card to a battlefield slot
2. UI calls `matchManager.DeployCard(card, position)`
3. `MatchManager` calls `Player.DeployCard(card, position)`
4. `Player` updates the card state and sets `card.SetFaceDown(false)`
5. `MatchManager` fires `OnCardDeployed` event
6. `MatchView` handles the event and updates the UI to reflect the new state

**Benefits**:

- Clear responsibility chain
- Single source of truth for state changes
- UI always reflects the current state of the data model

### Example 2: Card Drawing

**Correct Flow**:

1. Game logic determines a card should be drawn
2. `MatchManager` calls `Player.DrawCard(faceDown)`
3. `Player` sets the card's face-down state and adds it to the hand
4. `MatchManager` fires `OnCardDrawn` event
5. `MatchView` handles the event and creates UI for the card, reflecting its current state

### Example 3: Strategy Execution

**Correct Flow**:

1. `MatchManager` determines it's the AI player's turn
2. `MatchManager` requests a strategy from `StrategyPlanner` via coroutine
3. `StrategyPlanner` determines and executes a sequence of `Decision` objects
4. Each decision triggers appropriate events via `MatchManager`
5. `MatchView` updates the UI based on those events

## Future Architecture Considerations

### 1. Networking and Multiplayer

The event-driven architecture provides a foundation for future networking capabilities:

- Events can be serialized and sent over the network
- The clear separation between model and view allows for client-server architecture
- The state encapsulation enables validation of player actions

### 2. Modular Card Effects

Future card effect systems could leverage:

- The modifier system already in place
- The decision-based action system for complex card abilities
- The event architecture for triggering and responding to game events

### 3. Save/Load and Persistence

The architecture supports persistence through:

- Clear state encapsulation making serialization straightforward
- Event system that can replay actions from saved games
- Separation of concerns allowing different persistence strategies
