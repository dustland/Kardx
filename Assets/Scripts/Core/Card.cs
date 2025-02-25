using System;
using System.Collections.Generic;
using System.Linq;

namespace Kardx.Core
{
    public class Card
    {
        private Guid instanceId; // Unique instance identifier
        private CardType cardType; // Reference to static card definition
        private bool faceDown; // Whether the card is hidden
        private Faction ownerFaction; // The faction that owns this card
        private List<Modifier> modifiers = new(); // Active temporary modifiers
        private Dictionary<string, int> dynamicAttributes = new(); // Computed attributes
        private int currentDefence; // Current defence value
        private string currentAbilityId; // Current applied ability, should be one of the abilities in the card type

        // Public properties
        public Guid InstanceId => instanceId;
        public CardType CardType => cardType;
        public AbilityType CurrentAbility =>
            cardType.Abilities.FirstOrDefault(a => a.Id == currentAbilityId);
        public bool FaceDown => faceDown;
        public Faction OwnerFaction => ownerFaction;
        public IReadOnlyList<Modifier> Modifiers => modifiers;
        public IReadOnlyDictionary<string, int> DynamicAttributes => dynamicAttributes;

        // Computed properties
        // When current defence is 0, the card is destroyed
        public int CurrentDefence
        {
            get => currentDefence;
            private set => currentDefence = Math.Max(0, Math.Min(value, Defense));
        }

        public int Defense => cardType.BaseDefence + GetAttributeModifier("defense");
        public int Attack => cardType.BaseAttack + GetAttributeModifier("attack");
        public int CounterAttack =>
            cardType.BaseCounterAttack + GetAttributeModifier("counterAttack");

        public int DeploymentCost => cardType.DeploymentCost;
        public int OperationCost => cardType.OperationCost;
        public string ImageUrl => cardType.ImageUrl;
        public string Title => cardType.Title;
        public string Description => cardType.Description;

        // Constructor
        public Card(CardType cardType, Faction ownerFaction = Faction.Neutral)
        {
            this.cardType = cardType;
            this.instanceId = Guid.NewGuid();
            this.currentDefence = cardType.BaseDefence;
            this.faceDown = false; // Default to face up
            this.ownerFaction = ownerFaction;
            this.currentAbilityId =
                cardType.Abilities.Count > 0 ? cardType.Abilities[0].Id : string.Empty;
        }

        // Methods to control face-down state
        public void SetFaceDown(bool isFaceDown)
        {
            this.faceDown = isFaceDown;
        }

        public void Flip()
        {
            this.faceDown = !this.faceDown;
        }

        // Modifier management
        public void AddModifier(Modifier modifier)
        {
            if (modifier != null)
            {
                modifiers.Add(modifier);
                RecalculateAttributes();
            }
        }

        public void RemoveModifier(Modifier modifier)
        {
            if (modifier != null && modifiers.Remove(modifier))
            {
                RecalculateAttributes();
            }
        }

        public void ClearExpiredModifiers()
        {
            modifiers.RemoveAll(m => !m.IsActive());
            RecalculateAttributes();
        }


        private int GetAttributeModifier(string attribute)
        {
            int modifier = 0;
            foreach (var mod in modifiers)
            {
                if (mod.IsActive() && mod.Attribute == attribute)
                {
                    modifier += mod.Value;
                }
            }
            return modifier;
        }

        private void RecalculateAttributes()
        {
            dynamicAttributes.Clear();
            foreach (var mod in modifiers)
            {
                if (mod.IsActive())
                {
                    if (!dynamicAttributes.ContainsKey(mod.Attribute))
                    {
                        dynamicAttributes[mod.Attribute] = 0;
                    }
                    dynamicAttributes[mod.Attribute] += mod.Value;
                }
            }
        }

        // Event handlers
        protected virtual void OnDefenceChanged()
        {
            // Override in derived classes or use events if needed
        }

        protected virtual void OnLevelUp()
        {
            // Override in derived classes or use events if needed
        }
    }
}
