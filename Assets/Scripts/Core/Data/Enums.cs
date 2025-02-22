namespace Kardx.Core
{
  public enum CardCategory
  {
    Unit,
    Order,
    Countermeasure,
    Headquarters
  }

  public enum CardRarity
  {
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
  }

  public enum ZoneType
  {
    Deck,
    Hand,
    Battlefield,
    DiscardPile,
    Exile,
    Limbo
  }

  public enum ModifierType
  {
    Buff,
    Debuff,
    Status
  }

  public enum EffectType
  {
    Damage,
    Heal,
    Buff,
    Debuff,
    Draw,
    Discard
  }

  public enum TriggerType
  {
    OnDeployment,
    OnDeath,
    OnTurnStart,
    OnTurnEnd,
    OnDamageDealt,
    OnDamageTaken
  }
}