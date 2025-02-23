using System;
using UnityEngine;

namespace Kardx.Core.Data.Abilities
{
    [Serializable]
    public class Condition
    {
        [SerializeField]
        private string type; // Condition type (e.g., "zone", "healthThreshold")

        [SerializeField]
        private string value; // Condition value

        [SerializeField]
        private string operatorType; // Comparison operator (e.g., "==", ">=", "<=")

        // Public properties
        public string Type => type;
        public string Value => value;
        public string OperatorType => operatorType;

        // Constructor
        public Condition(string type, string value, string operatorType = "==")
        {
            this.type = type;
            this.value = value;
            this.operatorType = operatorType;
        }

        // Helper method to create numeric conditions
        public static Condition CreateNumericCondition(
            string type,
            float value,
            string operatorType
        )
        {
            if (!IsValidOperator(operatorType))
            {
                throw new ArgumentException($"Invalid operator type: {operatorType}");
            }
            return new Condition(type, value.ToString(), operatorType);
        }

        // Helper method to create equality conditions
        public static Condition CreateEqualityCondition(string type, string value)
        {
            return new Condition(type, value, "==");
        }

        // Validate operator type
        private static bool IsValidOperator(string op)
        {
            return op switch
            {
                "==" or "!=" or ">" or "<" or ">=" or "<=" => true,
                _ => false,
            };
        }

        // ToString override for debugging
        public override string ToString()
        {
            return $"{type} {operatorType} {value}";
        }
    }
}
