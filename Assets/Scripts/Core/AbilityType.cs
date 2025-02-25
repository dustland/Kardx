using System;
using System.Collections.Generic;

namespace Kardx.Core
{
    public class AbilityType
    {
        private string id;
        private string name;
        private string description;
        private int cost;
        private List<EffectType> effects = new();
        private List<string> conditions = new();

        public string Id => id;
        public string Name => name;
        public string Description => description;
        public int Cost => cost;
        public IReadOnlyList<EffectType> Effects => effects;
        public IReadOnlyList<string> Conditions => conditions;

        public AbilityType(string id, string name, string description, int cost)
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.name = name;
            this.description = description;
            this.cost = Math.Max(0, cost);
        }

        public void AddEffect(EffectType effect)
        {
            if (effect != null)
            {
                effects.Add(effect);
            }
        }

        public void AddCondition(string condition)
        {
            if (condition != null)
            {
                conditions.Add(condition);
            }
        }

        public void RemoveEffect(EffectType effect)
        {
            effects.Remove(effect);
        }

        public void RemoveCondition(string condition)
        {
            conditions.Remove(condition);
        }

        public AbilityType Clone()
        {
            var clone = new AbilityType(
                Guid.NewGuid().ToString(),
                $"{name} (Clone)",
                description,
                cost
            );

            foreach (var effect in effects)
            {
                clone.AddEffect(effect.Clone());
            }

            foreach (var condition in conditions)
            {
                clone.AddCondition(condition);
            }

            return clone;
        }
    }
}
