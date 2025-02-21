# Kardx System Data Design

## Overview

This document details the enhanced data system design for a [Kards](https://kards.com)-alike card game. The design emphasizes modularity, a data-driven approach to abilities, and extensibility by clearly separating the following layers:

- **Data Layer**: Manages static card definitions, zones, and base attributes.
- **Logic Layer**: Handles game rules, turn phases, and the dynamic application of card abilities and effects.
- **UI Layer**: Presents the data to the player while supporting animations, lazy-loaded images (using WebP), and responsive design.

Game designers can now add or modify abilities simply by updating JSON configurations, which enhances iteration speed and flexibility.

## Card Definitions

### Card Type

Each card is defined by a static type that serves as a blueprint for all of its instances. The updated CardType now includes metadata for localization, resource cost, rarity, and associated abilities.

```cs
public class CardType
{
    public string Id; // Unique identifier (GUID or slug)
    public string Name; // Localization key for the card name
    public string Description; // Localization key for the card description
    public string Category; // e.g., Creature, Spell, Artifact, etc.
    public string Subtype; // Archetype (e.g., Warrior, Mage)
    public int Cost; // Resource cost to play the card
    public int BaseHealth;    // Was "Defense"
    public int AttackPower;   // Was "Attack"
    public int Rarity; // e.g., Common = 1, Rare = 2, Epic = 3, Legendary = 4
    public string Set; // Card edition or set identifier
    public string ImageUrl; // Optimized WebP image URL with size metadata
    public Dictionary<string, int> BaseAttributes; // Additional base attributes (e.g., health, speed)
    public List<AbilityDefinition> Abilities; // Ability definitions associated with the card
}
```

### Card Instance

A card instance represents a live card in the system—whether in a deck, hand, or on the battlefield. In addition to linking back to the static definition, the instance maintains dynamic state such as current level, modifiers, and computed attributes.

```cs
public class Card
{
    public Guid InstanceId;        // Unique instance identifier
    public CardType CardType;      // Reference to the static card definition
    public int Level;
    public int Experience;
    public string OwnerId;
    public string ControllerId;
    public List<Modifier> Modifiers; // Active temporary modifiers (buffs/debuffs)
    public Dictionary<string, int> DynamicAttributes; // Computed attributes (from buffs, equipment, etc.)
    public HealthValue Health;
}
```

### Modifier

Modifiers are temporary stat changes that can be applied to cards. They are used to implement card abilities and effects.

```cs
// Auxiliary modifier class for temporary stat changes
public class Modifier {
    public string SourceCardId;  // Identifier for the card that applied the modifier
    public int RemainingTurns;   // Number of turns the modifier will last
    public int Value;            // The value of the modifier (e.g., +2 attack)
    public string Attribute;     // The attribute being modified (e.g., "attack", "defense")
    public ModifierType Type;    // Type of modifier (e.g., Buff, Debuff)

    // Additional methods for handling modifier logic
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

### Ability Definition

```cs
public class AbilityDefinition
{
    public string Id;          // Unique ability identifier (e.g., "ambush")
    public string Trigger;     // Event that activates the ability (e.g., "onDeployment")
    public string Category;    // Ability type (e.g., "tactic", "passive")
    public int Cost;           // Resource cost to activate
    public string Target;      // Targeting scope (e.g., "enemy", "ally", "self")
    public EffectDefinition Effect; // Effect to apply when triggered
    public List<Condition> Conditions; // Activation requirements

    /// <summary>
    /// Cooldown configuration - if null, no cooldown
    /// </summary>
    public int? Cooldown;
}

public class Condition
{
    public string Type;    // Condition type (e.g., "zone", "healthThreshold")
    public string Value;   // Condition value (e.g., "forest", "50")

    /// <summary>
    /// Optional comparison operator for numeric conditions
    /// Default: "==" for string comparisons
    /// </summary>
    public string Operator; // ">", "<", ">=", "<=", "!=", "=="
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

```cs
public class EffectDefinition
{
    public string Type;         // Effect type (e.g., "damage", "heal", "buff")
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
  "formula": "base + (caster.magic * multiplier) + (caster.attack * bonusFactor)",
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
        // Evaluate "base + (caster.magic * multiplier) + (caster.attack * bonusFactor)"
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

    public void TriggerAbility(AbilityDefinition ability, Card caster, Card target)
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
var ambushAbility = new AbilityDefinition {
    Id = "ambush",
    Trigger = "onDeployment",
    Cost = 2,
    Target = "enemy",
    Effect = new EffectDefinition {
        Type = "damage",
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

The board state is responsible for storing the state of the board, i.e. the cards on the board, the cards in the hand, the cards in the deck, etc.

```cs
public class BoardState {
    public Stack<Card> Deck;          // Proper draw mechanism
    public Dictionary<Position, Card> Battlefield;
    public Queue<Card> DiscardPile;     // Last-in ordering
}
```

## Battle System and Turn Phases

The battle system manages the flow of the game using distinct phases (draw, main, combat, and end). This modular approach allows for granular control of events and state updates.

```cs
public enum TurnPhase {
    Begin, Draw, Main, Battle, End, Cleanup
}

public class BattleManager {
    public BoardState Board;         // The current state of game zones
    public int TurnNumber;
    public string CurrentTurnPlayerId;
    public TurnPhase CurrentPhase;

    public void StartBattle() {
        // Initialize board state, shuffle decks, and select the starting player
    }

    public void NextTurn() {
        // Transition to the next turn and cycle through phases
    }

    public void ExecuteCombatPhase() {
        // Handle combat interactions such as attack resolution and trigger combat effects
    }

    public void EndTurn() {
        // Cleanup end-of-turn states (e.g., expire modifiers, resolve end-turn triggers, draw cards)
    }

    public void ProcessModifiers() {  // Was "CleanupModifiers"
        // Handle turn progression and removal
    }
}
```

## Conclusion

This document provides a realistic and flexible architecture suitable for a Kardx-alike card game. By cleanly separating static card definitions from dynamic game state and encapsulating ability logic in data, the system supports rapid iteration and easier extensibility. Designers can now introduce new cards, abilities, or gameplay mechanics purely through data updates, while developers maintain a robust backend that manages complex game rules.
