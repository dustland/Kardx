# Kardx Game Data Architecture

[查看简体中文版本](./Data-CN.md)

## Introduction

This document presents the data architecture for Kardx, a card game inspired by [Kards](https://kards.com). The architecture emphasizes modularity, data-driven abilities, and extensibility, structured across three primary layers:

- **Data Layer**: Manages static definitions, zones, and baseline attributes.
- **Logic Layer**: Implements game rules, turn phases, and the dynamic application of abilities and effects.
- **UI Layer**: Responsible for data visualization, supporting animations, lazy-loaded images (WebP), and responsive design.

The Data and Logic Layers together form the Core System, designed to operate independently of UnityEngine.

## Core Data Model

### Board State

The board encapsulates the core game state, enforcing a strict two-player model with centralized turn management and effect tracking. It maintains player references, active global effects, and overall game progression.

```csharp
public class Board
{
    private readonly Player[] players = new Player[2];
    private readonly List<GameEffect> activeEffects = new();
    private int turnNumber = 1;
    private string currentPlayerId;

    public Player CurrentPlayer => GetPlayerById(currentPlayerId);
    public Player Player => players[0];
    public Player Opponent => players[1];
}
```

### Card Data Model

Cards are implemented using a two-layer design that distinguishes static data (defined by CardType) from runtime state. This separation enables efficient memory usage and clear delineation between card definitions and instance-specific modifications. Each card comprises:

- **Static Data**: Base statistics, abilities, and costs
- **Runtime State**: Current values, active modifiers, and visibility status
- **Instance Data**: Unique identifier, ownership details, and dynamic effects

```csharp
public class Card
{
    private Guid instanceId;
    private CardType cardType;
    private bool isFaceDown;
    private Faction ownerFaction;
    private List<Modifier> modifiers = new();
    private int currentDefence;
    private string currentAbilityId;

    public int Defence => cardType.BaseDefence + GetAttributeModifier("defence");
    public int Attack => cardType.BaseAttack + GetAttributeModifier("attack");
    public int DeploymentCost => cardType.DeploymentCost;
}
```

### Player State

The player model administers all player-specific resources and card collections. Exposing these collections as read-only ensures that state modifications occur exclusively through validated methods. Key attributes include:

- **Battlefield Capacity**: A fixed size of 5 slots
- **Hand Limit**: Maximum of 10 cards
- **Credit Limit**: 9 credits per player
- **Faction Alignment**
- **Headquarters Card**

```csharp
public class Player
{
    public const int BATTLEFIELD_SLOT_COUNT = 5;
    private const int MAX_HAND_SIZE = 10;
    private const int MAX_CREDITS = 9;

    private string playerId;
    private int credits;
    private Faction faction;

    private readonly List<Card> hand = new();
    private readonly Card[] battlefield = new Card[BATTLEFIELD_SLOT_COUNT];
    private readonly Stack<Card> deck = new();
    private readonly Queue<Card> discardPile = new();
    private Card headquartersCard;

    public IReadOnlyList<Card> Hand => hand;
    public IReadOnlyList<Card> Battlefield => Array.AsReadOnly(battlefield);
    public IReadOnlyList<Card> Deck => deck.ToList();
    public IReadOnlyList<Card> DiscardPile => discardPile.ToList();
}
```

## Data Types

### Enumerations

The game utilizes several enumerations to define categorical data, ensuring clarity and consistency across the system:

```csharp
public enum CardCategory
{
    Unit,           // Deployable units
    Order,          // One-time effects
    Countermeasure, // Designed to nullify opponent actions
    Headquarter,    // Special card deployed on the battlefield
}

public enum Faction
{
    UnitedStates,    // American forces
    SovietUnion,     // Soviet forces
    BritishEmpire,   // British forces
    ThirdReich,      // German forces
    Empire,          // Japanese forces
    Neutral          // Non-aligned cards or special cases
}

public enum CardRarity
{
    Standard = 1,
    Limited = 2,
    Special = 3,
    Elite = 4,
}

public enum ZoneType
{
    Deck,
    Hand,
    Battlefield,
    DiscardPile,
    Exile,
    Limbo,
}

public enum ModifierType
{
    Buff,
    Debuff,
    Status,
}

public enum AbilityCategory
{
    Tactic,
    Passive,
}

public enum EffectCategory
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Draw,
    Discard,
}

public enum TriggerType
{
    OnDeployment,
    OnDeath,
    OnTurnStart,
    OnTurnEnd,
    OnDamageDealt,
    OnDamageTaken,
}
```

## Core Systems

### Game State Management

The game state is organized hierarchically, from the match level down to individual card attributes. This structure ensures clear ownership, efficient state updates, and proper scoping of effects.

```csharp
Board
├── Players[2]                    // Exactly 2 players
│   ├── Deck                     // Cards available for drawing
│   ├── Hand                     // Cards held in hand (max 10)
│   ├── Battlefield[5]           // Fixed deployment slots
│   │   └── Card                 // Deployed card
│   │       ├── CardType         // Static data definition
│   │       ├── RuntimeState     // Dynamic values (e.g., Defence, Attack)
│   │       │   └── Modifiers    // Temporary effects
│   │       └── InstanceData     // Unique properties (ID, Ownership, FaceDown)
│   └── DiscardPile             // Cards discarded or removed from play
└── ActiveEffects               // Global effects influencing game rules
```

This structure guarantees:

1. Unambiguous card ownership via zone placement
2. Separation between static definitions and dynamic game state
3. Localized updates to enhance efficiency
4. Appropriate scoping for both global and card-specific effects
5. Consistent state tracking across game components

### State Transitions

#### Card Movement

The card movement mechanism enforces a strict validation hierarchy. The `Player` class manages basic zone transitions and resource validations, while the `MatchManager` enforces overall game rules and turn order. Movements are atomic and rigorously validated.

Key validations include:

- Maximum hand size (10 cards)
- Sufficient resources (credits available for deployment)
- Valid battlefield slot availability (5 positions)
- Consistency of card ownership and game state

```csharp
public class Player
{
    public Card DrawCard(bool faceDown = false);
    public bool DeployCard(Card card, int position);
    public bool DiscardFromHand(Card card);
    public bool RemoveFromBattlefield(Card card);
}

public class MatchManager
{
    public bool CanDeployCard(Card card);
    public bool DeployCard(Card card, int position);
}
```

These measures ensure that:

1. Cards transition only between legitimate zones
2. All transitions are validated before effecting changes
3. Resource costs are duly applied
4. State modifications are executed atomically

#### Turn Phases

Turn progression follows a structured sequence, defined by the following phases:

```csharp
public enum TurnPhase
{
    StartTurn,    // Upkeep and card draw
    MainPhase,    // Deployment and activation
    CombatPhase,  // Attack declarations
    ResponsePhase,// Counter-actions
    EndPhase      // Cleanup and effect resolution
}
```

### Resource Management

Resource management follows a stringent hierarchical model. Individual players manage their credits and card slots, while the `MatchManager` sets global constraints such as turn limits and starting conditions.

**Game Constants:**

- Battlefield slots: 5 per player
- Maximum hand size: 10 cards
- Credit limit: 9 per player
- Starting hand: 4 cards
- Maximum turns: 50

```csharp
public class Player
{
    public const int BATTLEFIELD_SLOT_COUNT = 5;
    private const int MAX_HAND_SIZE = 10;
    private const int MAX_CREDITS = 9;

    private int credits;
    private readonly List<Card> hand = new();
    private readonly Card[] battlefield = new Card[BATTLEFIELD_SLOT_COUNT];
    private readonly Stack<Card> deck = new();
    private readonly Queue<Card> discardPile = new();

    public bool SpendCredits(int amount);
    public void AddCredits(int amount);
    public void StartTurn(int turnNumber);
}

public class MatchManager
{
    private readonly int startingHandSize = 4;
    private readonly int maxTurns = 50;

    public void StartMatch(string player1Id, string player2Id,
                         Faction player1Faction, Faction player2Faction);
}
```

Key Features:

1. Enforced resource limits
2. Turn-based credit generation
3. Rigorous validation for resource consumption
4. Atomic resource transactions
5. Scoped resource state tracking

### Data Persistence

Card definitions are stored in a structured format, ensuring consistency and facilitating serialization. For example:

```json
{
  "id": "INF-001",
  "title": "Veteran Infantry",
  "category": "Unit",
  "rarity": "Standard",
  "stats": {
    "attack": 2,
    "defence": 3,
    "deploymentCost": 2
  },
  "abilities": [
    {
      "id": "FirstStrike",
      "trigger": "OnAttack",
      "effect": "DamageFirst"
    }
  ]
}
```

In the future, it will be essential to persist gameplay history to enhance player experience and analytics. This will allow tracking of player progress, achievements, and provide valuable insights for game balancing and improvements.

## Best Practices

1. **Data Immutability:**

   - Load card definitions as read-only
   - Maintain immutable instance IDs
   - Record historical actions in an append-only manner

2. **State Validation**

   - Enforce defined paths for zone transitions
   - Validate resource limits at all boundaries
   - Verify card states before applying changes

3. **Data Consistency**

   - Ensure all state changes are atomic
   - Validate all references
   - Clearly distinguish derived data

4. **Performance Considerations**
   - Batch state updates where feasible
   - Cache frequently accessed data
   - Pre-size collections appropriately

## Data-driven Battle System

The data-driven battle system leverages external JSON configuration files to define game rules, card properties, and abilities, making the game highly tunable without altering the engine code.

For example, a JSON configuration file might look like this:

```json
{
  "cards": [
    {
      "id": "INF-001",
      "title": "Veteran Infantry",
      "category": "Unit",
      "rarity": "Standard",
      "stats": {
        "attack": 2,
        "defence": 3,
        "deploymentCost": 2
      },
      "abilities": [
        {
          "id": "FirstStrike",
          "trigger": "OnAttack",
          "effect": "DamageFirst",
          "parameters": {
            "multiplier": 1.5
          }
        }
      ]
    },
    {
      "id": "ORD-001",
      "title": "Order: Swift Maneuver",
      "category": "Order",
      "rarity": "Limited",
      "stats": {
        "deploymentCost": 1
      },
      "abilities": [
        {
          "id": "SpeedBoost",
          "trigger": "OnDeployment",
          "effect": "IncreaseSpeed",
          "parameters": {
            "duration": 2
          }
        }
      ]
    }
  ]
}
```

This JSON file defines the rules for each card:

- **Card Definitions:** Each card is identified by an ID, title, category, and rarity, situating it within the game's ecosystem.
- **Stats:** Base attributes (attack, defence, deploymentCost) determine combat performance.
- **Abilities:** Each ability is specified with a trigger (such as OnAttack or OnDeployment), an effect, and parameters to fine-tune its behavior.

1. **Parsing and Validation:**

   - At game startup, the rule engine reads and deserializes the JSON file into structured data objects.
   - These objects are validated to ensure that all required properties are present and well-formed.

2. **Rule Compilation:**

   - For each card, the engine compiles the base stats and registers abilities. For instance, abilities with the trigger "OnAttack" are linked to the card's attack action.

3. **Dynamic Execution:**

   - During gameplay, when an event occurs (e.g., a card attacks or is deployed), the engine looks up the appropriate rules from the compiled data.
   - It then applies any modifiers or extra effects as defined in the JSON, computing damage or triggering additional effects as required.

4. **Balance Tuning:**
   - Game designers can update the JSON to tweak stats and abilities, enabling rapid iteration on game mechanics without modifying engine code.

This approach decouples game logic from data definitions, promoting a flexible and maintainable battle system design.

## UI Components

The UI Layer is responsible for visualizing data and processing user interactions that lead to state mutations. Although its implementation is not the focal point of this document, a brief overview is provided below.

### CardView Component

The CardView component renders a card's state, handling:

- Visibility (face up/down)
- Content updates
- Interaction events
- Visual effects (e.g., drag-and-drop, animations)

Notable features include automatic refreshes based on card state and a dedicated view for displaying card details.

```csharp
public class CardView : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Sprite cardBackSprite;

    private void ShowCardBack()
    {
        contentRoot.gameObject.SetActive(false);
        backgroundImage.sprite = cardBackSprite;
    }

    public void UpdateCardView()
    {
        if (card != null && card.FaceDown)
        {
            ShowCardBack();
            return;
        }
        // Display standard card content...
    }
}
```

## Conclusion

This document provides a comprehensive overview of the data architecture for Kardx, a card game inspired by Kards. By delineating the architecture into distinct layers—the Data, Logic, and UI Layers—we ensure a modular, extensible, and maintainable codebase. The separation between static definitions and runtime state enables efficient memory usage and supports dynamic gameplay features while maintaining consistency across game elements.

Key benefits of this design include:

- **Clarity and Modularity:** The hierarchical structure clearly defines the responsibilities at the board, player, and card levels, allowing for straightforward state management and updates.
- **Robust Validation:** Rigorous validation mechanisms are integrated into the card movement and resource management systems, ensuring that all state changes occur atomically and in accordance with game rules.
- **Scalability:** The separation of concerns permits future enhancements and scalability, accommodating more complex game dynamics without compromising maintainability.
- **Performance and Consistency:** Through strategic caching, pre-sizing of collections, and precise state transitions, the design balances performance with consistency across game data.

Looking forward, potential areas for future development include the incorporation of adaptive difficulty mechanisms, deeper integration of global effect management, and refinement of data persistence strategies. These improvements aim to further boost performance and enhance the overall player experience.

This design lays a solid foundation for building robust and scalable gaming systems, offering insights that may be extended to a broader range of data-driven interactive applications.
