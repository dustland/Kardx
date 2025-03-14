using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        private int currentDefense; // Current defense value
        private string currentAbilityId; // Current applied ability, should be one of the abilities in the card type
        private Player owner; // Reference to the Player who owns this card
        private bool hasAttackedThisTurn; // Whether this card has attacked this turn

        // Events
        public event Action<Card> OnDeath;

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
        public bool IsUnitCard => CardType.Category == CardCategory.Unit;
        public bool IsOrderCard => CardType.Category == CardCategory.Order;
        public bool IsAlive => CurrentDefense > 0;
        public int Cost => CardType.Cost;
        // When current defense is 0, the card is destroyed
        public int CurrentDefense
        {
            get => currentDefense;
            set
            {
                bool wasAlive = IsAlive;
                currentDefense = Math.Max(0, value);
                if (wasAlive && !IsAlive)
                {
                    Debug.Log($"[Card] {Title} has been destroyed!");
                    OnDeath?.Invoke(this);
                }
            }
        }

        // Property to access card abilities
        public IReadOnlyList<AbilityType> Abilities => CardType.Abilities;

        public int Defense => cardType.BaseDefense + GetAttributeModifier("defense");
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
            this.currentDefense = cardType.BaseDefense;
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
            // Only update the property if it's changing
            if (FaceDown != isFaceDown)
            {
                Debug.Log($"[Card] {Title} SetFaceDown({isFaceDown}) called from: {new System.Diagnostics.StackTrace().ToString()}");
                FaceDown = isFaceDown;
            }
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

            int previousDefense = CurrentDefense;
            CurrentDefense -= amount;

            // Log the damage
            UnityEngine.Debug.Log(
                $"[Card] {Title} took {amount} damage. Defense reduced from {previousDefense} to {CurrentDefense}"
            );
        }

        // Heal damage
        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            int previousDefense = CurrentDefense;
            CurrentDefense = Math.Min(CurrentDefense + amount, Defense);

            // Log the healing
            UnityEngine.Debug.Log(
                $"[Card] {Title} healed {amount} points. Defense increased from {previousDefense} to {CurrentDefense}"
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

        /// <summary>
        /// Processes end of turn effects for this card.
        /// </summary>
        public void ProcessEndOfTurnEffects()
        {
            // Process any end of turn effects
            // This could include processing modifiers, abilities, etc.
            
            // Example: Remove temporary modifiers that expire at end of turn
            var expiredModifiers = modifiers.Where(m => m.ExpiresAtEndOfTurn).ToList();
            foreach (var modifier in expiredModifiers)
            {
                RemoveModifier(modifier);
            }
        }
        
        /// <summary>
        /// Processes start of turn effects for this card.
        /// </summary>
        public void ProcessStartOfTurnEffects()
        {
            // Process any start of turn effects
            // This could include processing abilities that trigger at the start of a turn
            
            // Reset attack status for the new turn
            hasAttackedThisTurn = false;
        }

        // Event handlers
        protected virtual void OnDefenseChanged()
        {
            // Override in derived classes or use events if needed
        }

        protected virtual void OnLevelUp()
        {
            // Override in derived classes or use events if needed
        }
    }
}
