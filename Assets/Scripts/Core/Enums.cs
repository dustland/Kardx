namespace Kardx.Core
{
    public enum CardCategory
    {
        Unit, // Represents a deployable unit
        Order, // Trigger a one-time effect and are then discarded
        Countermeasure, // can be activated to cancel out opponent Orders
        Headquarters, // Headquarters is a special card that can be deployed on the battlefield and is not a unit
    }

    public enum Faction
    {
        UnitedStates, // American forces
        SovietUnion, // Soviet forces
        BritishEmpire, // British forces
        ThirdReich, // German forces
        Empire, // Japanese forces
        Neutral, // For special cards or non-aligned cards
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
        Move,
        Summon,
        Transform,
        Modifier,
        Counter,
        Destroy,
        ReturnToHand,
        CopyCard,
        GainOperation,
        Special,
    }

    public enum TriggerType
    {
        Manual, // Manually activated by player
        OnDeploy, // Triggered when card is deployed
        OnTurnStart, // Triggered at start of turn
        OnTurnEnd, // Triggered at end of turn
        OnDamaged, // Triggered when damaged
        OnDestroyed, // Triggered when destroyed
        OnAttack, // Triggered when attacking
        OnDefend, // Triggered when defending
        OnDraw, // Triggered when drawn
        OnDiscard, // Triggered when discarded
        OnCombatDamage, // Triggered when dealing combat damage
        OnOrderPlay, // Triggered when an order card is played
        OnFrontlineChange, // Triggered when frontline changes
    }

    public enum TargetingType
    {
        None, // No target needed
        SingleAlly, // Single friendly unit
        SingleEnemy, // Single enemy unit
        AllAllies, // All friendly units
        AllEnemies, // All enemy units
        Row, // Entire row of units
        Column, // Entire column of units
        Self, // Self-targeting
        RandomEnemy, // Random enemy target
        FrontlineUnit, // Frontline unit
        HQ, // Headquarters target
        SameNation, // Units of the same nation
    }

    public enum RangeType
    {
        Any, // No range restriction
        Adjacent, // Adjacent cells only
        Melee, // Melee range (typically 1 cell)
        Ranged, // Ranged attack (typically 2+ cells)
        Line, // Straight line
        Diagonal, // Diagonal line
        Area, // Area effect (typically 3x3)
    }
}
