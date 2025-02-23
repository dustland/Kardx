using System;
using UnityEngine;

namespace Kardx.Core.Data.Cards
{
    [Serializable]
    public class Modifier
    {
        [SerializeField]
        private string sourceCardId; // ID of the card that applied the modifier

        [SerializeField]
        private int remainingTurns; // Number of turns remaining (-1 for permanent)

        [SerializeField]
        private int value; // The value of the modifier

        [SerializeField]
        private string attribute; // The attribute being modified

        [SerializeField]
        private ModifierType type; // Type of modifier

        [SerializeField]
        private string description; // Description of the modifier effect

        // Public properties
        public string SourceCardId => sourceCardId;
        public int RemainingTurns => remainingTurns;
        public int Value => value;
        public string Attribute => attribute;
        public ModifierType Type => type;
        public string Description => description;

        // Constructor
        public Modifier(
            string sourceCardId,
            int duration,
            int value,
            string attribute,
            ModifierType type,
            string description
        )
        {
            this.sourceCardId = sourceCardId;
            this.remainingTurns = duration;
            this.value = value;
            this.attribute = attribute;
            this.type = type;
            this.description = description;
        }

        // Check if the modifier is still active
        public bool IsActive()
        {
            return remainingTurns > 0 || remainingTurns == -1;
        }

        // Decrease the remaining duration
        public void DecrementDuration()
        {
            if (remainingTurns > 0)
            {
                remainingTurns--;
            }
        }

        // Create common modifier types
        public static Modifier CreateBuff(
            string sourceCardId,
            string attribute,
            int value,
            int duration
        )
        {
            return new Modifier(
                sourceCardId,
                duration,
                Mathf.Abs(value), // Ensure positive value for buffs
                attribute,
                ModifierType.Buff,
                $"+{value} {attribute} for {duration} turns"
            );
        }

        public static Modifier CreateDebuff(
            string sourceCardId,
            string attribute,
            int value,
            int duration
        )
        {
            return new Modifier(
                sourceCardId,
                duration,
                -Mathf.Abs(value), // Ensure negative value for debuffs
                attribute,
                ModifierType.Debuff,
                $"-{value} {attribute} for {duration} turns"
            );
        }

        public static Modifier CreatePermanentStatus(
            string sourceCardId,
            string attribute,
            int value
        )
        {
            return new Modifier(
                sourceCardId,
                -1, // Permanent duration
                value,
                attribute,
                ModifierType.Status,
                $"Permanent {value:+#;-#;0} {attribute}"
            );
        }

        // Clone the modifier (useful when applying to multiple targets)
        public Modifier Clone()
        {
            return new Modifier(sourceCardId, remainingTurns, value, attribute, type, description);
        }

        // ToString override for debugging
        public override string ToString()
        {
            string duration = remainingTurns == -1 ? "Permanent" : $"{remainingTurns} turns";
            return $"{type}: {value:+#;-#;0} {attribute} ({duration})";
        }
    }
}
