using System;

namespace Kardx.Models.Match
{
    public class Modifier
    {
        private string id;
        private string name;
        private string attribute;
        private int value;
        private ModifierType type;
        private int? duration;
        private int turnsRemaining;
        private DateTime? startTime;

        public string Id => id;
        public string Name => name;
        public string Attribute => attribute;
        public int Value => value;
        public ModifierType Type => type;
        public int? Duration => duration;
        public int TurnsRemaining => turnsRemaining;
        public DateTime? StartTime => startTime;
        public bool ExpiresAtEndOfTurn => duration.HasValue && duration.Value == 1 && turnsRemaining <= 1;

        public Modifier(
            string id,
            string name,
            string attribute,
            int value,
            ModifierType type,
            int? duration = null
        )
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.name = name ?? "Unnamed Modifier";
            this.attribute = attribute;
            this.value = value;
            this.type = type;
            this.duration = duration;
            this.turnsRemaining = duration ?? 0;
            this.startTime = duration.HasValue ? DateTime.UtcNow : null;
        }

        public bool IsActive()
        {
            if (!duration.HasValue)
            {
                return true; // Permanent modifier
            }

            return turnsRemaining > 0;
        }

        public void OnTurnEnd()
        {
            if (duration.HasValue && turnsRemaining > 0)
            {
                turnsRemaining--;
            }
        }

        public void ResetDuration()
        {
            if (duration.HasValue)
            {
                turnsRemaining = duration.Value;
                startTime = DateTime.UtcNow;
            }
        }

        public Modifier Clone()
        {
            return new Modifier(Guid.NewGuid().ToString(), name, attribute, value, type, duration);
        }
    }
}
