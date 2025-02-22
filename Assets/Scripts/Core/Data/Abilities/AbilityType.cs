using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kardx.Core.Data.Abilities
{
  [Serializable]
  public class AbilityType
  {
    [SerializeField] private string id;  // Unique ability identifier
    [SerializeField] private string name;  // Localization key for ability name
    [SerializeField] private string description;  // Localization key for description
    [SerializeField] private TriggerType trigger;  // Event that activates the ability
    [SerializeField] private AbilityCategory category;  // Ability type (e.g., "tactic", "passive")
    [SerializeField] private int cost;  // Resource cost to activate
    [SerializeField] private string target;  // Targeting scope (e.g., "enemy", "ally", "self")
    [SerializeField] private EffectType effect;  // Effect to apply when triggered
    [SerializeField] private List<Condition> conditions = new();  // Activation requirements
    [SerializeField] private int? cooldown;  // Optional cooldown in turns

    // Public properties
    public string Id => id;
    public string Name => name;
    public string Description => description;
    public TriggerType Trigger => trigger;
    public AbilityCategory Category => category;
    public int Cost => Mathf.Max(0, cost);
    public string Target => target;
    public EffectType Effect => effect;
    public IReadOnlyList<Condition> Conditions => conditions;
    public int? Cooldown => cooldown;

    // Constructor
    public AbilityType(
        string id,
        string name,
        string description,
        TriggerType trigger,
        AbilityCategory category,
        int cost,
        string target,
        EffectType effect,
        int? cooldown = null)
    {
      this.id = id;
      this.name = name;
      this.description = description;
      this.trigger = trigger;
      this.category = category;
      this.cost = cost;
      this.target = target;
      this.effect = effect;
      this.cooldown = cooldown;
    }

    // Methods to modify conditions (only during ability creation/editing)
    public void AddCondition(Condition condition)
    {
      if (condition != null)
      {
        conditions.Add(condition);
      }
    }

    public void ClearConditions()
    {
      conditions.Clear();
    }

    // Validation method
    public bool IsValid()
    {
      return !string.IsNullOrEmpty(id) &&
             !string.IsNullOrEmpty(name) &&
             !string.IsNullOrEmpty(description) &&
             effect != null;
    }
  }
}