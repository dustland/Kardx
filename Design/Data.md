# Kardx System Data Design

## Overview

This document details the enhanced data system design for a [Kards](https://kards.com)-alike card game. The design emphasizes modularity, a data-driven approach to abilities, and extensibility by clearly separating the following layers:

- **Data Layer**: Manages static card definitions, zones, and base attributes.
- **Logic Layer**: Handles game rules, turn phases, and the dynamic application of card abilities and effects.
- **UI Layer**: Presents the data to the player while supporting animations, lazy-loaded images (using WebP), and responsive design.

## Card System

### Card Type

Each card is defined by a static type that serves as a blueprint for all of its instances:

```cs
public class CardType
{
    public string Id; // Unique identifier (GUID or slug)
    public string Title; // Card title
    public string Description; // Card description
    public string Category; // e.g., Unit, Order, Countermeasure, Headquarter
    public string Subtype; // Archetype (e.g., Warrior, Mage)
    public int DeploymentCost; // Resource cost to play the card onto the battlefield
    public int OperationCost; // Resource cost to use the card's abilities or actions
    public int BaseDefence; // Base defence of the card
    public int BaseAttack; // Base attack power of the card
    public int BaseCounterAttack; // Power used for counterattacks
    public CardRarity Rarity; // e.g., Standard = 1, Limited = 2, Special = 3, Elite = 4
    public string SetId; // Card edition or set identifier
    public string ImageUrl; // Optimized WebP image URL with size metadata
    public Dictionary<string, int> Attributes; // Additional base attributes (e.g., defence, speed)
    public List<AbilityType> Abilities; // Ability definitions associated with the card
}
```

### Card Instance

A card instance represents a live card in the system—whether in a deck, hand, or on the battlefield. In addition to linking back to the static definition, the instance maintains dynamic state such as current defence value, modifiers, and computed attributes.

```cs
public class Card
{
    public Guid InstanceId;        // Unique instance identifier
    public CardType CardType;      // Reference to the static card definition
    public bool FaceDown;         // Whether the card is hidden
    public List<Modifier> Modifiers; // Active temporary modifiers (buffs/debuffs)
    public Dictionary<string, int> DynamicAttributes; // Computed attributes (from buffs, equipment, etc.)
    public int CurrentDefence;    // Current defence value
    public string CurrentAbilityId; // Current applied ability
}
```

### Modifier

Modifiers are temporary stat changes that can be applied to cards. They are used to implement card abilities and effects.

```cs
public class Modifier {
    public string SourceCardId;  // Identifier for the card that applied the modifier
    public int RemainingTurns;   // Number of turns the modifier will last
    public int Value;            // The value of the modifier (e.g., +2 attack)
    public string Attribute;     // The attribute being modified (e.g., "attack", "defence")
    public ModifierType Type;    // Type of modifier (e.g., Buff, Debuff, Status)

    public bool IsActive() {
        // Determine if the modifier is currently active based on conditions
    }

    public void ApplyEffect(Card targetCard) {
        // Apply the modifier's effect to the target card
    }

    public void Expire() {
        // Handle expiration logic, such as removing the modifier from the target
    }
}
```

## Data-Driven Ability System

Card abilities are defined as data, enabling designers to adjust functionality without code changes. Abilities include triggers, costs, visual effects, and preconditions.

### Ability Type

```cs
public class AbilityType
{
    public string Id;          // Unique ability identifier (e.g., "ambush")
    public string Trigger;     // Event that activates the ability (e.g., "onDeployment")
    public AbilityCategory Category;    // Ability type (e.g., "tactic", "passive")
    public int Cost;           // Resource cost to activate
    public string Target;      // Targeting scope (e.g., "enemy", "ally", "self")
    public EffectDefinition Effect; // Effect to apply when triggered
    public List<Condition> Conditions; // Activation requirements
    public int? Cooldown;      // Cooldown configuration - if null, no cooldown
}

public class Condition
{
    public string Type;    // Condition type (e.g., "zone", "defenceThreshold")
    public string Value;   // Condition value (e.g., "forest", "50")
    public string Operator; // Optional comparison operator for numeric conditions
}
```

The following is an example of a condition:

```json
{
  "type": "zone",
  "value": "forest",
  "operator": "=="
}
```

### Effect Definition

The effect definition specifies how the ability's impact is calculated and applied. The dynamic calculation can incorporate multiple attributes from the caster.

Effect types:

- `damage`: Deals damage to a target card.
- `heal`: Heals a target card.
- `buff`: Applies a buff to a target card.
- `debuff`: Applies a debuff to a target card.

You can also define custom effect types by extending the `EffectDefinition` class.

```cs
public class EffectDefinition
{
    public EffectCategory Type;         // Effect type (e.g., "damage", "heal", "buff")
    public string Target;       // "single", "area", "self"
    public string Formula;      // Mathematical formula for effect calculation
    public List<EffectAttribute> Attributes; // Parameters used in the formula
    public int? Cooldown;       // Turns until reuse (null = no cooldown)
    public string Animation;    // Reference to visual effect
    public string Sound;        // Reference to audio effect
}

public class EffectAttribute
{
    public string Name;  // Attribute identifier (e.g., "base", "multiplier")
    public object Value; // Can be int, float, or string reference
}
```

The following is an example of an ambush ability:

```json
{
  "type": "damage",
  "target": "single",
  "formula": "base + (caster.stealth * multiplier) + (caster.baseAttack * bonusFactor)",
  "attributes": [
    { "name": "base", "value": 5 },
    { "name": "multiplier", "value": 1.5 },
    { "name": "bonusFactor", "value": 0.5 }
  ],
  "cooldown": 2,
  "animation": "explosion_anim",
  "sound": "damage_sound"
}
```

Key features:

1. `Formula` uses mathematical expressions with attributes from both caster and target
2. `Attributes` provide configurable parameters for the formula
3. `Value` is `object` to support different data types (int, float, string references)
4. Nullable `Cooldown` matches the design document's conditional cooldown logic

## Rule Engine and Effect Application

The rule engine dynamically interprets and applies ability effects based on their JSON definitions. It validates conditions, computes effect values, and alters game state accordingly.

```cs
public class RuleEngine
{
    public int CalculateEffect(EffectDefinition effect, Card caster)
    {
        // Parse the calculation expression (e.g., using an expression parser)
        // Evaluate "base + (caster.stealth * multiplier) + (caster.baseAttack * bonusFactor)"
        // Return the computed numeric result.
    }

    public void ApplyEffect(EffectDefinition effect, Card caster, Card target)
    {
        // Evaluate any prerequisites and then apply the calculated effect value
        // Update the target card's state (e.g., damage, buffs/debuffs).
    }

    public bool ValidateAbilityConditions(List<Condition> conditions, Card card)
    {
        // Check if all conditions (resource cost, cooldowns, etc.) pass
        // Return true if the card is permitted to use this ability.
    }

    public void TriggerAbility(AbilityType ability, Card caster, Card target)
    {
        if (ValidateAbilityConditions(ability.Conditions, caster))
        {
            ApplyEffect(ability.Effect, caster, target);
            // Optionally initiate visual effects, sound, and set ability cooldown.
        }
    }
}
```

### Example Usage

Below is an updated example showing how to define and trigger an ambush ability using the proper class definitions:

```cs
// Define a ambush ability with dynamic scaling
var ambushAbility = new AbilityType {
    Id = "ambush",
    Trigger = TriggerType.OnDeployment,
    Cost = 2,
    Target = "enemy",
    Effect = new EffectDefinition {
        Type = EffectCategory.Damage,
        Formula = "base + (caster.stealth * multiplier)",
        Attributes = new List<EffectAttribute> {
            new EffectAttribute { Name = "base", Value = 3 },
            new EffectAttribute { Name = "multiplier", Value = 1.2 }
        },
        Animation = "ambush_anim",
        Sound = "ambush_sound"
    },
    Conditions = new List<Condition> {
        new Condition {
            Type = "zone",
            Value = "forest",
            Operator = "=="
        }
    },
    Cooldown = 1
};

// Calculate the damage using the rule engine and apply the ability
int damage = ruleEngine.CalculateEffect(ambushAbility.Effect, caster);
ruleEngine.TriggerAbility(ambushAbility, caster, target);
```

## Zones and Board State

The board state manages separate game zones for each player and shared game areas. This ensures proper isolation of player-specific card collections while maintaining shared game elements.

```cs
public struct PlayerState {
    public Stack<Card> Deck;          // Private draw pile (random access via shuffle)
    public List<Card> Hand;           // Current playable cards (max 10)
    public Card[] Battlefield;        // Fixed size array of played cards (5 slots)
    public Queue<Card> DiscardPile;   // Public graveyard (last-in ordering)
    public int Credits;               // Credits available for the player to spend
    public Card Headquarter;          // Special card representing the player's headquarter
}

public class BoardState {
    public Dictionary<string, PlayerState> Players; // Key: player ID
    public SharedZones Global;        // Neutral game areas
    public List<GameEffect> ActiveEffects; // Ongoing game-wide effects
}

public class SharedZones {
    public Stack<Card> NeutralDeck;   // Environment/quest cards
    public List<Card> Exile;          // Removed-from-game cards
    public List<Card> Limbo;          // Cards in transition between zones
}

```

Key considerations:

1. Separate zones per player (deck/hand/battlefield/discard)
2. Clear distinction between player-specific and shared zones
3. Fixed battlefield size of 5 slots per player
4. Explicit exile zone for removed cards
5. Transitional limbo zone for cards moving between areas

The concept of a "Limbo" zone in card games typically refers to a temporary state where cards are in transition between different zones or states. This can be useful for handling complex game mechanics where cards need to be temporarily removed from play but are not yet in their final destination zone.

Example usage:

```cs
var board = new BoardState {
    Players = new Dictionary<string, PlayerState> {
        ["P1"] = new PlayerState {
            Deck = new Stack<Card>(p1Deck),
            Hand = new List<Card>(),
            Battlefield = new Card[] { null, null, null, null, null },
            DiscardPile = new Queue<Card>(),
            Credits = 10,
            Headquarter = p1Headquarter
        },
        ["P2"] = new PlayerState { /* ... */ }
    },
    Global = new SharedZones {
        NeutralDeck = new Stack<Card>(environmentCards),
        Exile = new List<Card>(),
        Limbo = new List<Card>()
    }
};
```

## Additional Enumerations

```cs
public enum CardCategory
{
    Unit, // Represents a deployable unit
    Order, // Trigger a one-time effect and are then discarded
    Countermeasure, // can be activated to cancel out opponent Orders
    Headquarter, // Headquarter is a special card that can be deployed on the battlefield and is not a unit
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
