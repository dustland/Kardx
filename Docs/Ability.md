# Kardx Ability System Design Document

[简体中文版](./Ability-CN.md)

## Overview

The Kardx Ability System employs a **data-driven** design, providing a flexible framework for defining and executing card abilities in the game. Drawing inspiration from [Kards](https://kards.com) game mechanics, this system supports both basic ability types and complex strategic card effects.

### Core Design Principles

The Kardx Ability System is built around four core design principles:

Separation of Data and Logic: Clear distinction between ability definitions (static data) and ability instances (runtime state)
Declarative Configuration: Define abilities through JSON, creating new abilities without code changes
Composability: Complex abilities can be built by combining basic effects
Event-Driven Triggering: Abilities trigger automatically based on game state changes

## Data Structures

### AbilityType

`AbilityType` defines the static properties of an ability that can be attached to cards. This is the template or blueprint for abilities.

```csharp
public class AbilityType
{
    // Basic Information
    public string Id { get; private set; }           // Unique identifier
    public string Name { get; private set; }         // Display name
    public string Description { get; private set; }  // Text description
    public string IconPath { get; private set; }     // Ability icon path

    // Activation Parameters
    public TriggerType Trigger { get; private set; } // Trigger type
    public int CooldownTurns { get; private set; }   // Cooldown turns
    public int UsesPerTurn { get; private set; }     // Uses per turn
    public int UsesPerMatch { get; private set; }    // Uses per match
    public bool RequiresFaceUp { get; private set; } // Whether card must be face up
    public int OperationCost { get; private set; }   // Cost to use

    // Targeting Parameters
    public TargetingType Targeting { get; private set; } // Target type
    public RangeType Range { get; private set; }         // Range type
    public bool CanTargetFaceDown { get; private set; }  // Whether can target face down cards

    // Effect Parameters
    public EffectType Effect { get; private set; }      // Effect type
    public int EffectValue { get; private set; }        // Effect value
    public int EffectDuration { get; private set; }     // Effect duration in turns

    // Special Parameters
    public string SpecialEffectId { get; private set; }              // Special effect ID
    public Dictionary<string, object> CustomParameters { get; }      // Custom parameters

    // Methods
    public AbilityType Clone();                         // Create a copy of this ability type
    public void SetCustomParameter(string key, object value);  // Set a custom parameter
    public T GetCustomParameter<T>(string key, T defaultValue = default);  // Get a custom parameter with type conversion

    // Serialization
    public static AbilityType FromJson(string json);    // Create ability type from JSON
    public string ToJson();                             // Convert to JSON
}
```

In the game, ability types are typically defined via JSON configuration, for example:

```json
{
  "id": "ability_strategic_bombing",
  "name": "Strategic Bombing",
  "description": "Deal {value} damage to all enemy units and 2 additional damage to their HQ",
  "iconPath": "icons/abilities/strategic_bombing.png",
  "trigger": "Manual",
  "cooldownTurns": 2,
  "usesPerTurn": 1,
  "usesPerMatch": 3,
  "requiresFaceUp": true,
  "operationCost": 3,
  "targeting": "AllEnemies",
  "range": "Any",
  "canTargetFaceDown": false,
  "effect": "Damage",
  "effectValue": 1,
  "effectDuration": 0,
  "specialEffectId": "",
  "customParameters": {
    "additionalHQDamage": 2,
    "requiresAircraft": true
  }
}
```

### Ability

```csharp
public class Ability
{
    // Reference to ability type definition
    public AbilityType AbilityType { get; }

    // Runtime state
    public Card OwnerCard { get; }
    public int UsesThisTurn { get; private set; }
    public int TotalUses { get; private set; }
    public int TurnsSinceLastUse { get; private set; }
    public bool IsActive { get; private set; }

    // Ability validation and execution methods
    public bool CanUse(Card target = null);
    public void Execute(List<Card> targets);
    public List<Card> GetValidTargets();
}
```

## Ability System Components

### AbilitySystem

The central management system responsible for ability registration and querying, ability trigger detection, ability execution and effect application, and custom effect handler management.

```csharp
public class AbilitySystem
{
    // Register custom effect handler
    public void RegisterSpecialEffectHandler(string effectId, ISpecialEffectHandler handler);

    // Process various trigger points
    public void ProcessTurnStart(Player player);
    public void ProcessTurnEnd(Player player);
    public void ProcessCardDeployed(Card card);
    public void ProcessCardDamaged(Card card, int damage, Card source);

    // Execute ability
    public bool ExecuteAbility(Ability ability, List<Card> targets = null);
}
```

### ISpecialEffectHandler

```csharp
public interface ISpecialEffectHandler
{
    // Execute custom effect
    bool ExecuteEffect(Ability ability, Card source, List<Card> targets, Dictionary<string, object> parameters);
}
```

## Triggers and Effect Types

### Trigger Types

| Type                | Description                     | Kards Equivalent |
| ------------------- | ------------------------------- | ---------------- |
| `Manual`            | Manually activated by player    | Order card       |
| `OnDeploy`          | Triggered when card is deployed | Deploy effect    |
| `OnTurnStart`       | Triggered at turn start         | Turn start       |
| `OnTurnEnd`         | Triggered at turn end           | Turn end         |
| `OnDamaged`         | Triggered when damaged          | Damage trigger   |
| `OnDestroyed`       | Triggered when destroyed        | Death effect     |
| `OnAttack`          | Triggered when attacking        | Attack trigger   |
| `OnDefend`          | Triggered when defending        | Defense trigger  |
| `OnDraw`            | Triggered when drawn            | Draw trigger     |
| `OnDiscard`         | Triggered when discarded        | Discard trigger  |
| `OnCombatDamage`    | When dealing combat damage      | Damage trigger   |
| `OnOrderPlay`       | When order card is played       | Command trigger  |
| `OnFrontlineChange` | When frontline changes          | Frontline change |

### Targeting Types

| Type            | Description            | Use Case             |
| --------------- | ---------------------- | -------------------- |
| `None`          | No target required     | Global effects       |
| `SingleAlly`    | Single allied unit     | Buffs, healing       |
| `SingleEnemy`   | Single enemy unit      | Single target damage |
| `AllAllies`     | All allied units       | Mass buffs           |
| `AllEnemies`    | All enemy units        | Mass damage          |
| `Row`           | Entire row of units    | Area effects         |
| `Column`        | Entire column of units | Area effects         |
| `Self`          | Self targeting         | Self enhancement     |
| `RandomEnemy`   | Random enemy target    | Random effects       |
| `FrontlineUnit` | Frontline unit         | Frontline tactics    |
| `HQ`            | Headquarters target    | Strategic strikes    |
| `SameNation`    | Same nation units      | National synergy     |

### Effect Types

| Type            | Description        | Parameter Example                                  |
| --------------- | ------------------ | -------------------------------------------------- |
| `Damage`        | Deal damage        | `{"value": 2, "damageType": "physical"}`           |
| `Heal`          | Heal unit          | `{"value": 2, "overHeal": false}`                  |
| `Buff`          | Positive effect    | `{"attack": 1, "defense": 2, "duration": 2}`       |
| `Debuff`        | Negative effect    | `{"attack": -1, "defense": -1, "duration": 2}`     |
| `Draw`          | Draw cards         | `{"count": 1, "specific": "order"}`                |
| `Discard`       | Discard cards      | `{"count": 1, "random": true}`                     |
| `Move`          | Move card          | `{"destination": "battlefield", "position": 2}`    |
| `Summon`        | Summon unit        | `{"cardTypeId": "INF-001", "position": 0}`         |
| `Transform`     | Transform          | `{"cardTypeId": "INF-002", "keepModifiers": true}` |
| `Modifier`      | Attribute modifier | `{"attack": 2, "defense": -1}`                     |
| `Counter`       | Add counter        | `{"counterType": "charge", "value": 1}`            |
| `Destroy`       | Directly destroy   | `{"ignoreEffects": false}`                         |
| `ReturnToHand`  | Return to hand     | `{"position": "top"}`                              |
| `CopyCard`      | Copy card          | `{"destination": "hand"}`                          |
| `GainOperation` | Gain resource      | `{"amount": 2, "resourceType": "credits"}`         |
| `Special`       | Custom effect      | _Depends on special effect handler_                |

## Data-Driven Examples

### Basic Unit Ability

```json
{
  "id": "bombardier_strike",
  "name": "Bombardier Strike",
  "description": "Deal 2 damage to an enemy unit",
  "trigger": "Manual",
  "targeting": "SingleEnemy",
  "effect": "Damage",
  "effectValue": 2,
  "usesPerTurn": 1
}
```

### Advanced Strategic Ability

```json
{
  "id": "total_mobilization",
  "name": "Total Mobilization",
  "description": "Give your HQ +2 defense and generate 1 additional credit per turn for 3 turns",
  "trigger": "OnDeploy",
  "targeting": "None",
  "effect": "Special",
  "specialEffectId": "strategicDecision",
  "customParameters": {
    "hqDefenseBonus": 2,
    "creditIncrement": 1,
    "duration": 3,
    "victoryPointCost": 2
  }
}
```

## Ability Execution Flow

1. **Trigger Detection**: AbilitySystem monitors game events and identifies which abilities should trigger
2. **Target Validation**: Check if targets are legal and collect valid targets
3. **Condition Validation**: Validate ability use conditions (cooldown, available uses, etc.)
4. **Cost Payment**: Deduct operation cost
5. **Effect Application**: Execute the ability effect
6. **State Update**: Update ability state (use count, cooldown, etc.)
7. **Event Notification**: Trigger events related to ability execution

## Extension: Strategic Decision System

The Strategic Decision system is a Kards-style advanced ability mechanism allowing players to make major decisions that affect the overall battlefield.

```json
{
  "strategicDecision": {
    "name": "Blitzkrieg",
    "description": "After deployment, your units get +1 attack but lose 1 health at end of each turn",
    "intelCost": 2,
    "victoryPoints": 1,
    "escalationEffects": {
      "blitzkrieg": {
        "unitAttackBonus": 1,
        "endOfTurnDamage": 1,
        "duration": "permanent"
      }
    }
  }
}
```

## Design Best Practices

The following design best practices should be followed when creating abilities:

Consistency First: Maintain consistent semantics for ability triggers and effects
Modular Design: Break down complex abilities into multiple simple effects
Balance Considerations: Limit frequency and conditions for powerful abilities
Performance Optimization: Delay target evaluation and condition validation to avoid unnecessary calculations
Extensible Definitions: Extend basic ability types through custom parameters

---

_Document Last Updated: 2025-02-26_
