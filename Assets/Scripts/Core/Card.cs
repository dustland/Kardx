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
            private set => currentDefence = Math.Max(0, value);
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

        // Heal damage
        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            int previousDefence = CurrentDefence;
            CurrentDefence = Math.Min(CurrentDefence + amount, Defence);

            // Log the healing
            UnityEngine.Debug.Log(
                $"[Card] {Title} healed {amount} points. Defence increased from {previousDefence} to {CurrentDefence}"
            );
        }

        /// <summary>
        /// Activates all abilities of the card at once.
        /// This is primarily used for Order cards that trigger their effect and then are discarded.
        /// </summary>
        /// <param name="targets">Optional targets for the abilities, if required</param>
        /// <returns>True if at least one ability was activated successfully</returns>
        public bool ActivateAllAbilities(List<Card> targets = null)
        {
            bool anyActivated = false;

            // Create ability instances for each ability type
            foreach (var abilityType in CardType.Abilities)
            {
                var ability = new Ability(abilityType, this);

                // Check if the ability can be used
                if (ability.CanUse())
                {
                    // For abilities that require targets but none were provided
                    if (
                        ability.AbilityType.Targeting != TargetingType.None
                        && ability.AbilityType.Targeting != TargetingType.Self
                        && (targets == null || targets.Count == 0)
                    )
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[Card] Ability {ability.AbilityType.Name} requires targets but none were provided"
                        );
                        continue;
                    }

                    // For self-targeting abilities
                    if (ability.AbilityType.Targeting == TargetingType.Self)
                    {
                        ability.Use(new List<Card> { this });
                        anyActivated = true;
                    }
                    // For abilities that target other cards
                    else if (targets != null && targets.Count > 0)
                    {
                        ability.Use(targets);
                        anyActivated = true;
                    }
                }
            }

            return anyActivated;
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
