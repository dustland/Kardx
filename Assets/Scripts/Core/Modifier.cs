using System;

namespace Kardx.Core
{
    public class Modifier
    {
        private string id;
        private string attribute;
        private int value;
        private int? duration;
        private DateTime? startTime;

        public string Id => id;
        public string Attribute => attribute;
        public int Value => value;
        public int? Duration => duration;
        public DateTime? StartTime => startTime;

        public Modifier(string id, string attribute, int value, int? duration = null)
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            this.attribute = attribute;
            this.value = value;
            this.duration = duration;
            this.startTime = duration.HasValue ? DateTime.UtcNow : null;
        }

        public bool IsActive()
        {
            if (!duration.HasValue || !startTime.HasValue)
            {
                return true;
            }

            var elapsedTime = DateTime.UtcNow - startTime.Value;
            return elapsedTime.TotalSeconds < duration.Value;
        }

        public Modifier Clone()
        {
            return new Modifier(
                Guid.NewGuid().ToString(),
                attribute,
                value,
                duration
            );
        }
    }
}
