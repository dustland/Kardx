using System;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Core.Data.Abilities;

namespace Kardx.Core.Data.Cards
{
  [Serializable]
  public class Card
  {
    [SerializeField] private Guid instanceId;  // Unique instance identifier
    [SerializeField] private CardType cardType;  // Reference to static card definition
    [SerializeField] private int level;  // Current level of the card
    [SerializeField] private int experience;  // Current experience points
    [SerializeField] private string ownerId;  // Player who owns the card
    [SerializeField] private string controllerId;  // Player currently controlling the card
    [SerializeField] private List<Modifier> modifiers = new();  // Active temporary modifiers
    [SerializeField] private Dictionary<string, int> dynamicAttributes = new();  // Computed attributes
    [SerializeField] private int currentHealth;  // Current health value

    // Public properties
    public Guid InstanceId => instanceId;
    public CardType CardType => cardType;
    public int Level => level;
    public int Experience => experience;
    public string OwnerId => ownerId;
    public string ControllerId => controllerId;
    public IReadOnlyList<Modifier> Modifiers => modifiers;
    public IReadOnlyDictionary<string, int> DynamicAttributes => dynamicAttributes;

    // Computed properties
    public int CurrentHealth
    {
      get => currentHealth;
      private set => currentHealth = Mathf.Clamp(value, 0, MaxHealth);
    }

    public int MaxHealth => cardType.BaseHealth + GetAttributeModifier("health");
    public int Attack => cardType.BaseAttack + GetAttributeModifier("attack");
    public int CounterAttack => cardType.CounterAttack + GetAttributeModifier("counterAttack");

    // Constructor
    public Card(CardType cardType, string ownerId)
    {
      this.instanceId = Guid.NewGuid();
      this.cardType = cardType;
      this.ownerId = ownerId;
      this.controllerId = ownerId;
      this.level = 1;
      this.experience = 0;
      this.currentHealth = cardType.BaseHealth;
    }

    // Health modification methods
    public void TakeDamage(int amount)
    {
      if (amount > 0)
      {
        CurrentHealth -= amount;
        // Trigger any relevant effects
        OnHealthChanged();
      }
    }

    public void Heal(int amount)
    {
      if (amount > 0)
      {
        CurrentHealth += amount;
        // Trigger any relevant effects
        OnHealthChanged();
      }
    }

    // Modifier management
    public void AddModifier(Modifier modifier)
    {
      if (modifier != null)
      {
        modifiers.Add(modifier);
        RecalculateAttributes();
      }
    }

    public void RemoveModifier(Modifier modifier)
    {
      if (modifier != null && modifiers.Remove(modifier))
      {
        RecalculateAttributes();
      }
    }

    public void ClearExpiredModifiers()
    {
      modifiers.RemoveAll(m => !m.IsActive());
      RecalculateAttributes();
    }

    // Experience and leveling
    public void AddExperience(int amount)
    {
      if (amount > 0)
      {
        experience += amount;
        CheckLevelUp();
      }
    }

    // Private helper methods
    private void CheckLevelUp()
    {
      int experienceNeeded = level * 100; // Simple level-up formula
      if (experience >= experienceNeeded)
      {
        level++;
        experience -= experienceNeeded;
        // Apply level-up effects
        OnLevelUp();
      }
    }

    private int GetAttributeModifier(string attribute)
    {
      int modifier = 0;
      foreach (var mod in modifiers)
      {
        if (mod.IsActive() && mod.Attribute == attribute)
        {
          modifier += mod.Value;
        }
      }
      return modifier;
    }

    private void RecalculateAttributes()
    {
      dynamicAttributes.Clear();
      foreach (var mod in modifiers)
      {
        if (mod.IsActive())
        {
          if (!dynamicAttributes.ContainsKey(mod.Attribute))
          {
            dynamicAttributes[mod.Attribute] = 0;
          }
          dynamicAttributes[mod.Attribute] += mod.Value;
        }
      }
    }

    // Event handlers
    protected virtual void OnHealthChanged()
    {
      // Override in derived classes or use events if needed
    }

    protected virtual void OnLevelUp()
    {
      // Override in derived classes or use events if needed
    }

    // Clone method for creating copies of cards (e.g., for deck building)
    public Card Clone(string newOwnerId)
    {
      return new Card(cardType, newOwnerId)
      {
        level = this.level,
        experience = this.experience
      };
    }
  }
}