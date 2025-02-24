namespace Kardx.Core
{
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
}
