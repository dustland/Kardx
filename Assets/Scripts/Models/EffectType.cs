using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kardx.Models
{
    public class EffectType
    {
        [JsonProperty("id")]
        private string id;
        [JsonProperty("name")]
        private string name;
        [JsonProperty("description")]
        private string description;
        [JsonProperty("category")]
        private EffectCategory category;
        [JsonProperty("attribute")]
        private string attribute;
        [JsonProperty("value")]
        private int value;
        [JsonProperty("duration")]
        private int? duration;
        [JsonProperty("conditions")]
        private List<Condition> conditions = new();

        public string Id => id;
        public string Name => name;
        public string Description => description;
        public EffectCategory Category => category;
        public string Attribute => attribute;
        public int Value => value;
        public int? Duration => duration;
        public IReadOnlyList<Condition> Conditions => conditions;

        public EffectType(
            string id,
            string name,
            string description,
            EffectCategory category,
            string attribute,
            int value,
            int? duration = null
        )
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.name = name;
            this.description = description;
            this.category = category;
            this.attribute = attribute;
            this.value = value;
            this.duration = duration;
        }

        public void AddCondition(Condition condition)
        {
            if (condition != null)
            {
                conditions.Add(condition);
            }
        }

        public void RemoveCondition(Condition condition)
        {
            conditions.Remove(condition);
        }

        public EffectType Clone()
        {
            var clone = new EffectType(
                Guid.NewGuid().ToString(),
                $"{name} (Clone)",
                description,
                category,
                attribute,
                value,
                duration
            );

            foreach (var condition in conditions)
            {
                clone.AddCondition(condition.Clone());
            }

            return clone;
        }

        // Factory method to create from JSON
        public static EffectType FromJson(string json)
        {
            return JsonConvert.DeserializeObject<EffectType>(json);
        }

        // Method to convert to JSON
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
