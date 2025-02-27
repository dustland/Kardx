using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kardx.Core
{
    public class AbilityType
    {
        // 基本信息
        [JsonProperty("id")]
        private string id;
        [JsonProperty("name")]
        private string name;
        [JsonProperty("description")]
        private string description;
        [JsonProperty("iconPath")]
        private string iconPath;

        // 激活参数
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

        // 目标参数
        [JsonProperty("targeting")]
        private TargetingType targeting;
        [JsonProperty("range")]
        private RangeType range;
        [JsonProperty("canTargetFaceDown")]
        private bool canTargetFaceDown;

        // 效果参数
        [JsonProperty("effect")]
        private EffectType effect;
        [JsonProperty("effectValue")]
        private int effectValue;
        [JsonProperty("effectDuration")]
        private int effectDuration;

        // 特殊参数
        [JsonProperty("specialEffectId")]
        private string specialEffectId;
        [JsonProperty("customParameters")]
        private Dictionary<string, object> customParameters = new Dictionary<string, object>();

        // 公共属性
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
        public EffectType Effect => effect;
        public int EffectValue => effectValue;
        public int EffectDuration => effectDuration;
        public string SpecialEffectId => specialEffectId;
        public IReadOnlyDictionary<string, object> CustomParameters => customParameters;

        // 构造函数
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
            this.specialEffectId = string.Empty;
        }

        // 设置目标参数
        public void SetTargetingParameters(TargetingType targeting, RangeType range, bool canTargetFaceDown)
        {
            this.targeting = targeting;
            this.range = range;
            this.canTargetFaceDown = canTargetFaceDown;
        }

        // 设置效果参数
        public void SetEffectParameters(EffectType effect, int effectValue, int effectDuration = 0)
        {
            this.effect = effect;
            this.effectValue = effectValue;
            this.effectDuration = Math.Max(0, effectDuration);
        }

        // 设置特殊效果ID
        public void SetSpecialEffectId(string specialEffectId)
        {
            this.specialEffectId = specialEffectId ?? string.Empty;
        }

        // 设置图标路径
        public void SetIconPath(string iconPath)
        {
            this.iconPath = iconPath;
        }

        // 自定义参数管理
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

        // 克隆方法
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
            clone.SetEffectParameters(effect, effectValue, effectDuration);
            clone.SetSpecialEffectId(specialEffectId);
            clone.SetIconPath(iconPath);

            foreach (var param in customParameters)
            {
                clone.SetCustomParameter(param.Key, param.Value);
            }

            return clone;
        }

        // 序列化方法
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
