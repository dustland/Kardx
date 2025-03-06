using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kardx.Models.Abilities
{
    public class AbilityType
    {
        // Basic Information
        [JsonProperty("id")]
        private string id;

        [JsonProperty("name")]
        private string name;

        [JsonProperty("description")]
        private string description;

        [JsonProperty("iconPath")]
        private string iconPath;

        // Activation Parameters
        [JsonProperty("trigger")]
        private TriggerType trigger;

        [JsonProperty("cooldownTurns")]
        private int cooldownTurns;

        [JsonProperty("usesPerTurn")]
        private int usesPerTurn;

        [JsonProperty("usesPerMatch")]
        private int usesPerMatch;

        [JsonProperty("requiresFaceUp")]
        private bool requiresFaceUp;

        [JsonProperty("operationCost")]
        private int operationCost;

        // Target Parameters
        [JsonProperty("targeting")]
        private TargetingType targeting;

        [JsonProperty("range")]
        private RangeType range;

        [JsonProperty("canTargetFaceDown")]
        private bool canTargetFaceDown;

        // Effect Parameters
        [JsonProperty("effect")]
        private EffectCategory effect;

        [JsonProperty("effectValue")]
        private int effectValue;

        [JsonProperty("effectDuration")]
        private int effectDuration;

        [JsonProperty("effectAttribute")]
        private string effectAttribute;

        // Special Parameters
        [JsonProperty("specialEffectId")]
        private string specialEffectId;

        [JsonProperty("customParameters")]
        private Dictionary<string, object> customParameters = new Dictionary<string, object>();

        // Public Properties
        public string Id => id;
        public string Name => name;
        public string Description => description;
        public string IconPath => iconPath;
        public TriggerType Trigger => trigger;
        public int CooldownTurns => cooldownTurns;
        public int UsesPerTurn => usesPerTurn;
        public int UsesPerMatch => usesPerMatch;
        public bool RequiresFaceUp => requiresFaceUp;
        public int OperationCost => operationCost;
        public TargetingType Targeting => targeting;
        public RangeType Range => range;
        public bool CanTargetFaceDown => canTargetFaceDown;
        public EffectCategory Effect => effect;
        public int EffectValue => effectValue;
        public int EffectDuration => effectDuration;
        public string EffectAttribute => effectAttribute;
        public string SpecialEffectId => specialEffectId;
        public IReadOnlyDictionary<string, object> CustomParameters => customParameters;

        // Constructor
        public AbilityType(
            string id,
            string name,
            string description,
            TriggerType trigger = TriggerType.Manual,
            int cooldownTurns = 0,
            int usesPerTurn = 1,
            int usesPerMatch = 0,
            bool requiresFaceUp = true,
            int operationCost = 0
        )
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.name = name;
            this.description = description;
            this.trigger = trigger;
            this.cooldownTurns = Math.Max(0, cooldownTurns);
            this.usesPerTurn = Math.Max(0, usesPerTurn);
            this.usesPerMatch = Math.Max(0, usesPerMatch);
            this.requiresFaceUp = requiresFaceUp;
            this.operationCost = Math.Max(0, operationCost);
            this.targeting = TargetingType.None;
            this.range = RangeType.Any;
            this.canTargetFaceDown = false;
            this.effectValue = 0;
            this.effectDuration = 0;
            this.effectAttribute = string.Empty;
            this.specialEffectId = string.Empty;
        }

        // Set Target Parameters
        public void SetTargetingParameters(
            TargetingType targeting,
            RangeType range,
            bool canTargetFaceDown
        )
        {
            this.targeting = targeting;
            this.range = range;
            this.canTargetFaceDown = canTargetFaceDown;
        }

        // Set Effect Parameters
        public void SetEffectParameters(EffectCategory effect, int effectValue, int effectDuration = 0, string effectAttribute = "")
        {
            this.effect = effect;
            this.effectValue = effectValue;
            this.effectDuration = Math.Max(0, effectDuration);
            this.effectAttribute = effectAttribute;
        }

        // Set Special Effect ID
        public void SetSpecialEffectId(string specialEffectId)
        {
            this.specialEffectId = specialEffectId ?? string.Empty;
        }

        // Set Icon Path
        public void SetIconPath(string iconPath)
        {
            this.iconPath = iconPath;
        }

        // Custom Parameter Management
        public void SetCustomParameter(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                customParameters[key] = value;
            }
        }

        public T GetCustomParameter<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key) || !customParameters.ContainsKey(key))
            {
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(customParameters[key], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        // Clone Method
        public AbilityType Clone()
        {
            var clone = new AbilityType(
                Guid.NewGuid().ToString(),
                $"{name} (Clone)",
                description,
                trigger,
                cooldownTurns,
                usesPerTurn,
                usesPerMatch,
                requiresFaceUp,
                operationCost
            );

            clone.SetTargetingParameters(targeting, range, canTargetFaceDown);
            clone.SetEffectParameters(effect, effectValue, effectDuration, effectAttribute);
            clone.SetSpecialEffectId(specialEffectId);
            clone.SetIconPath(iconPath);

            foreach (var param in customParameters)
            {
                clone.SetCustomParameter(param.Key, param.Value);
            }

            return clone;
        }

        // Serialization Method
        public static AbilityType FromJson(string json)
        {
            return JsonConvert.DeserializeObject<AbilityType>(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
