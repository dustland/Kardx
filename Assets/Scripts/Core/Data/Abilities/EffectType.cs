using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kardx.Core.Data.Abilities
{
  [Serializable]
  public class EffectType
  {
    [SerializeField] private EffectCategory category;  // Effect type (e.g., "damage", "heal", "buff")
    [SerializeField] private string target;  // "single", "area", "self"
    [SerializeField] private string formula;  // Mathematical formula for effect calculation
    [SerializeField] private List<EffectAttribute> attributes = new();  // Parameters used in formula
    [SerializeField] private int? cooldown;  // Turns until reuse (null = no cooldown)
    [SerializeField] private string animationKey;  // Reference to visual effect
    [SerializeField] private string soundKey;  // Reference to audio effect

    // Public properties
    public EffectCategory Category => category;
    public string Target => target;
    public string Formula => formula;
    public IReadOnlyList<EffectAttribute> Attributes => attributes;
    public int? Cooldown => cooldown;
    public string AnimationKey => animationKey;
    public string SoundKey => soundKey;

    // Constructor
    public EffectType(
        EffectCategory category,
        string target,
        string formula,
        string animationKey,
        string soundKey,
        int? cooldown = null)
    {
      this.category = category;
      this.target = target;
      this.formula = formula;
      this.animationKey = animationKey;
      this.soundKey = soundKey;
      this.cooldown = cooldown;
    }

    // Methods to modify attributes
    public void AddAttribute(string name, object value)
    {
      if (!string.IsNullOrEmpty(name) && value != null)
      {
        attributes.Add(new EffectAttribute(name, value));
      }
    }

    public void ClearAttributes()
    {
      attributes.Clear();
    }
  }

  [Serializable]
  public class EffectAttribute
  {
    [SerializeField] private string name;  // Attribute identifier
    [SerializeField] private string valueType;  // Type of the value (int, float, string)
    [SerializeField] private string serializedValue;  // Serialized value as string

    public string Name => name;

    // Get the value with proper type
    public object Value
    {
      get
      {
        return valueType switch
        {
          "int" => int.Parse(serializedValue),
          "float" => float.Parse(serializedValue),
          "string" => serializedValue,
          _ => throw new InvalidOperationException($"Unsupported value type: {valueType}")
        };
      }
    }

    public EffectAttribute(string name, object value)
    {
      this.name = name;

      // Determine value type and serialize
      switch (value)
      {
        case int i:
          valueType = "int";
          serializedValue = i.ToString();
          break;
        case float f:
          valueType = "float";
          serializedValue = f.ToString();
          break;
        case string s:
          valueType = "string";
          serializedValue = s;
          break;
        default:
          throw new ArgumentException($"Unsupported value type: {value.GetType()}");
      }
    }
  }
}