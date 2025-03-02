using System;
using System.Collections.Generic;
using System.Linq;

namespace Kardx.Core
{
    public class Card
    {
        private Guid instanceId; // Unique instance identifier
        private CardType cardType; // Reference to static card definition
        public bool FaceDown { get; private set; } // Whether the card is hidden
        private Faction ownerFaction; // The faction that owns this card
        private List<Modifier> modifiers = new(); // Active temporary modifiers
        private Dictionary<string, int> dynamicAttributes = new(); // Computed attributes
        private int currentDefence; // Current defence value
        private string currentAbilityId; // Current applied ability, should be one of the abilities in the card type
        private Player owner; // Reference to the Player who owns this card
        private bool hasAttackedThisTurn; // Whether this card has attacked this turn

        // Public properties
        public Guid InstanceId => instanceId;
        public CardType CardType => cardType;
        public AbilityType CurrentAbility =>
            cardType.Abilities.FirstOrDefault(a => a.Id == currentAbilityId);
        public Faction OwnerFaction => ownerFaction;
        public IReadOnlyList<Modifier> Modifiers => modifiers;
        public IReadOnlyDictionary<string, int> DynamicAttributes => dynamicAttributes;
        public Player Owner => owner;
        public bool HasAttackedThisTurn
        {
            get => hasAttackedThisTurn;
            set => hasAttackedThisTurn = value;
        }

        // Computed properties
        // When current defence is 0, the card is destroyed
        public int CurrentDefence
        {
            get => currentDefence;
            private set => currentDefence = Math.Max(0, Math.Min(value, Defence));
        }

        public int Defence => cardType.BaseDefence + GetAttributeModifier("defence");
        public int Attack => cardType.BaseAttack + GetAttributeModifier("attack");
        public int CounterAttack =>
            cardType.BaseCounterAttack + GetAttributeModifier("counterAttack");

        public int DeploymentCost => cardType.DeploymentCost;
        public int OperationCost => cardType.OperationCost;
        public string ImageUrl => cardType.ImageUrl;
        public string Title => cardType.Title;
        public string Description => cardType.Description;

        // Constructor
        public Card(CardType cardType, Faction ownerFaction = Faction.Neutral, Player owner = null)
        {
            this.cardType = cardType;
            this.instanceId = Guid.NewGuid();
            this.currentDefence = cardType.BaseDefence;
            this.FaceDown = false; // Default to face up
            this.ownerFaction = ownerFaction;
            this.owner = owner;
            this.currentAbilityId =
                cardType.Abilities.Count > 0 ? cardType.Abilities[0].Id : string.Empty;
            this.hasAttackedThisTurn = false; // Initialize to false
        }

        // Add the missing SetFaceDown method
        public void SetFaceDown(bool isFaceDown)
        {
            FaceDown = isFaceDown;
        }

        // Set the owner of the card
        public void SetOwner(Player player)
        {
            this.owner = player;
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

        // Take damage
        public void TakeDamage(int amount)
        {
            if (amount <= 0)
                return;

            int previousDefence = CurrentDefence;
            CurrentDefence -= amount;

            // Log the damage
            UnityEngine.Debug.Log(
                $"[Card] {Title} took {amount} damage. Defence reduced from {previousDefence} to {CurrentDefence}"
            );
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
