using UnityEngine;
using System;
using System.Collections.Generic;
using Kardx.Core.Data.Abilities;

namespace Kardx.Core.Data.Cards
{
  [CreateAssetMenu(fileName = "CardType", menuName = "Kardx/Card Type")]
  public class CardType : ScriptableObject
  {
    [Header("Identity")]
    [SerializeField] private string id;  // Unique identifier (GUID or slug)
    [SerializeField] private string name;  // Localization key for the card name
    [SerializeField] private string description;  // Localization key for the card description
    [SerializeField] private CardCategory category;  // e.g., Unit, Order, Countermeasure
    [SerializeField] private string subtype;  // Archetype (e.g., Warrior, Mage)

    [Header("Costs")]
    [SerializeField, Min(0)] private int deploymentCost;  // Resource cost to play the card
    [SerializeField, Min(0)] private int operationCost;  // Resource cost to use abilities

    [Header("Stats")]
    [SerializeField, Min(1)] private int baseDefence;  // Base defense of the card
    [SerializeField, Min(0)] private int baseAttack;  // Attack power of the card
    [SerializeField, Min(0)] private int baseCounterAttack;  // Power used for counterattacks

    [Header("Metadata")]
    [SerializeField] private CardRarity rarity;  // Card rarity level
    [SerializeField] private string setId;  // Card edition or set identifier

    [Header("Visuals")]
    [SerializeField] private string imageUrl;  // Optimized WebP image URL

    [Header("Attributes & Abilities")]
    [SerializeField] private SerializableDictionary<string, int> attributes = new();
    [SerializeField] private List<AbilityType> abilities = new();

    // Public properties with validation
    public string Id => id;
    public string Name => name;
    public string Description => description;
    public CardCategory Category => category;
    public string Subtype => subtype;
    public int DeploymentCost => deploymentCost;
    public int OperationCost => operationCost; // Orders and Countermeasures do not have operational costs.
    public int BaseDefence => baseDefence;
    public int BaseAttack => baseAttack;
    public int BaseCounterAttack => baseCounterAttack;
    public CardRarity Rarity => rarity;
    public string SetId => setId;
    public string ImageUrl => imageUrl;
    public IReadOnlyDictionary<string, int> Attributes => attributes;
    public IReadOnlyList<AbilityType> Abilities => abilities;

#if UNITY_EDITOR
    private void OnValidate()
    {
      // Ensure minimum values
      deploymentCost = Mathf.Max(0, deploymentCost);
      operationCost = Mathf.Max(0, operationCost);
      baseDefence = Mathf.Max(1, baseDefence);
      baseAttack = Mathf.Max(0, baseAttack);
      baseCounterAttack = Mathf.Max(0, baseCounterAttack);

      // Generate ID if empty
      if (string.IsNullOrEmpty(id))
      {
          id = Guid.NewGuid().ToString();
      }
    }
#endif

    // Methods to modify abilities and attributes (only during card creation/editing)
    public void AddAbility(AbilityType ability)
    {
      if (ability != null && !abilities.Contains(ability))
      {
        abilities.Add(ability);
      }
    }

    public void RemoveAbility(AbilityType ability)
    {
      abilities.Remove(ability);
    }

    public void SetAttribute(string key, int value)
    {
      if (!string.IsNullOrEmpty(key))
      {
        attributes[key] = value;
      }
    }

    public void RemoveAttribute(string key)
    {
      if (!string.IsNullOrEmpty(key))
      {
        attributes.Remove(key);
      }
    }

    // Clone this card type
    public CardType Clone()
    {
      var clone = Instantiate(this);
      clone.name = $"{name} (Clone)";
      clone.id = Guid.NewGuid().ToString();
      return clone;
    }

    // Factory method to create from JSON
    public static CardType FromJson(string json)
    {
      return JsonUtility.FromJson<CardType>(json);
    }

    // Method to convert to JSON
    public string ToJson()
    {
      return JsonUtility.ToJson(this);
    }
  }

  // Helper class for serializing dictionaries in Unity
  [Serializable]
  public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
  {
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
      keys.Clear();
      values.Clear();
      foreach (KeyValuePair<TKey, TValue> pair in this)
      {
        keys.Add(pair.Key);
        values.Add(pair.Value);
      }
    }

    public void OnAfterDeserialize()
    {
      Clear();
      for (int i = 0; i < keys.Count; i++)
      {
        Add(keys[i], values[i]);
      }
    }
  }
}