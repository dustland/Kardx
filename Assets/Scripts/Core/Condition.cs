using System;

namespace Kardx.Core
{
    public class Condition
    {
        private string id;
        private string description;
        private string attribute;
        private ComparisonType comparison;
        private int threshold;

        public string Id => id;
        public string Description => description;
        public string Attribute => attribute;
        public ComparisonType Comparison => comparison;
        public int Threshold => threshold;

        public Condition(
            string id,
            string description,
            string attribute,
            ComparisonType comparison,
            int threshold
        )
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.description = description;
            this.attribute = attribute;
            this.comparison = comparison;
            this.threshold = threshold;
        }

        public bool Evaluate(int value)
        {
            return comparison switch
            {
                ComparisonType.Equal => value == threshold,
                ComparisonType.NotEqual => value != threshold,
                ComparisonType.GreaterThan => value > threshold,
                ComparisonType.LessThan => value < threshold,
                ComparisonType.GreaterThanOrEqual => value >= threshold,
                ComparisonType.LessThanOrEqual => value <= threshold,
                _ => false
            };
        }

        public Condition Clone()
        {
            return new Condition(
                Guid.NewGuid().ToString(),
                description,
                attribute,
                comparison,
                threshold
            );
        }
    }

    public enum ComparisonType
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }
}
