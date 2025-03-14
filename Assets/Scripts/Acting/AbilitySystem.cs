using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Models;
using Kardx.Models.Abilities;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using UnityEngine;

namespace Kardx.Acting
{
    /// <summary>
    /// Central system for managing ability registration, triggering, and execution
    /// </summary>
    public class AbilitySystem
    {
        private Dictionary<string, ISpecialEffectHandler> specialEffectHandlers =
            new Dictionary<string, ISpecialEffectHandler>();
        private List<Ability> activeAbilities = new List<Ability>();
        private MatchManager matchManager;

        public AbilitySystem(MatchManager matchManager)
        {
            this.matchManager =
                matchManager ?? throw new ArgumentNullException(nameof(matchManager));
        }

        /// <summary>
        /// Register a special effect handler
        /// </summary>
        public void RegisterSpecialEffectHandler(ISpecialEffectHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (string.IsNullOrEmpty(handler.EffectId))
                throw new ArgumentException("Effect ID cannot be null or empty", nameof(handler));

            specialEffectHandlers[handler.EffectId] = handler;
        }

        /// <summary>
        /// Register a special effect handler by ID and handler instance
        /// </summary>
        public void RegisterSpecialEffectHandler(string effectId, ISpecialEffectHandler handler)
        {
            if (string.IsNullOrEmpty(effectId))
                throw new ArgumentException("Effect ID cannot be null or empty", nameof(effectId));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            specialEffectHandlers[effectId] = handler;
        }

        /// <summary>
        /// Register an ability to be tracked by the system
        /// </summary>
        public void RegisterAbility(Ability ability)
        {
            if (ability == null)
                throw new ArgumentNullException(nameof(ability));
            if (!activeAbilities.Contains(ability))
            {
                activeAbilities.Add(ability);
            }
        }

        /// <summary>
        /// Unregister an ability from the system
        /// </summary>
        public void UnregisterAbility(Ability ability)
        {
            activeAbilities.Remove(ability);
        }

        /// <summary>
        /// Process turn start trigger for a player
        /// </summary>
        public void ProcessTurnStart(Player player)
        {
            if (player == null)
                return;

            // Update all abilities for turn start
            foreach (var ability in activeAbilities)
            {
                ability.OnTurnStart();
            }

            // Trigger abilities that activate on turn start
            TriggerAbilities(TriggerType.OnTurnStart, player);
        }

        /// <summary>
        /// Process turn end trigger for a player
        /// </summary>
        public void ProcessTurnEnd(Player player)
        {
            if (player == null)
                return;

            // Trigger abilities that activate on turn end
            TriggerAbilities(TriggerType.OnTurnEnd, player);
        }

        /// <summary>
        /// Process card deployment trigger
        /// </summary>
        public void ProcessCardDeployed(Card card)
        {
            if (card == null)
                return;

            // Trigger abilities that activate on deployment
            TriggerAbilities(TriggerType.OnDeploy, card);
        }

        /// <summary>
        /// Process card damaged trigger
        /// </summary>
        public void ProcessCardDamaged(Card card, int damage, Card source)
        {
            if (card == null)
                return;

            // Trigger abilities that activate when damaged
            TriggerAbilities(
                TriggerType.OnDamaged,
                card,
                new Dictionary<string, object> { { "damage", damage }, { "source", source } }
            );
        }

        /// <summary>
        /// Process card destroyed trigger
        /// </summary>
        public void ProcessCardDestroyed(Card card)
        {
            if (card == null)
                return;

            // Trigger abilities that activate when destroyed
            TriggerAbilities(TriggerType.OnDestroyed, card);
        }

        /// <summary>
        /// Execute an ability with optional targets
        /// </summary>
        public bool ExecuteAbility(Ability ability, List<Card> targets = null)
        {
            if (ability == null)
                return false;
            if (!ability.CanUse())
                return false;

            // Validate targets if needed
            if (
                ability.AbilityType.Targeting != TargetingType.None
                && (targets == null || targets.Count == 0)
            )
            {
                Debug.LogWarning(
                    $"Ability {ability.AbilityType.Name} requires targets but none were provided"
                );
                return false;
            }

            // Apply operation cost
            // This would typically involve the player's resources
            // For now, we'll assume it's always successful

            // Apply the effect
            bool success = ApplyEffect(ability, targets);
            if (success)
            {
                // Mark the ability as used
                ability.MarkAsUsed();
            }

            return success;
        }

        /// <summary>
        /// Apply the effect of an ability
        /// </summary>
        private bool ApplyEffect(Ability ability, List<Card> targets)
        {
            var abilityType = ability.AbilityType;
            var source = ability.OwnerCard;

            // Handle special effects
            if (
                abilityType.Effect == EffectCategory.Special
                && !string.IsNullOrEmpty(abilityType.SpecialEffectId)
            )
            {
                if (specialEffectHandlers.TryGetValue(abilityType.SpecialEffectId, out var handler))
                {
                    return handler.ExecuteEffect(
                        ability,
                        source,
                        targets,
                        new Dictionary<string, object>(abilityType.CustomParameters)
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"No handler registered for special effect ID: {abilityType.SpecialEffectId}"
                    );
                    return false;
                }
            }

            // Handle standard effects
            switch (abilityType.Effect)
            {
                case EffectCategory.Damage:
                    return ApplyDamageEffect(ability, targets);
                case EffectCategory.Heal:
                    return ApplyHealEffect(ability, targets);
                case EffectCategory.Buff:
                    return ApplyBuffEffect(ability, targets);
                case EffectCategory.Debuff:
                    return ApplyDebuffEffect(ability, targets);
                // Implement other effect types as needed
                default:
                    Debug.LogWarning($"Effect type {abilityType.Effect} not implemented");
                    return false;
            }
        }

        /// <summary>
        /// Apply damage effect to targets
        /// </summary>
        private bool ApplyDamageEffect(Ability ability, List<Card> targets)
        {
            int damageValue = ability.AbilityType.EffectValue;

            foreach (var target in targets)
            {
                // Apply damage to the target
                target.TakeDamage(damageValue);
                Debug.Log($"Applying {damageValue} damage to {target.Title}");
            }

            return true;
        }

        /// <summary>
        /// Apply heal effect to targets
        /// </summary>
        private bool ApplyHealEffect(Ability ability, List<Card> targets)
        {
            int healValue = ability.AbilityType.EffectValue;

            foreach (var target in targets)
            {
                // Apply healing to the target
                target.Heal(healValue);
                Debug.Log($"Healing {target.Title} for {healValue}");
            }

            return true;
        }

        /// <summary>
        /// Apply buff effect to targets
        /// </summary>
        private bool ApplyBuffEffect(Ability ability, List<Card> targets)
        {
            int buffValue = ability.AbilityType.EffectValue;
            int duration = ability.AbilityType.EffectDuration;

            foreach (var target in targets)
            {
                // Create and apply a buff modifier
                var modifier = new Modifier(
                    Guid.NewGuid().ToString(),
                    $"Buff from {ability.AbilityType.Name}",
                    ability.AbilityType.EffectAttribute,
                    buffValue,
                    ModifierType.Buff,
                    duration
                );

                target.AddModifier(modifier);
                Debug.Log(
                    $"Applied {buffValue} {ability.AbilityType.EffectAttribute} buff to {target.Title} for {duration} turns"
                );
            }

            return true;
        }

        /// <summary>
        /// Apply debuff effect to targets
        /// </summary>
        private bool ApplyDebuffEffect(Ability ability, List<Card> targets)
        {
            int debuffValue = ability.AbilityType.EffectValue;
            int duration = ability.AbilityType.EffectDuration;

            foreach (var target in targets)
            {
                // Create and apply a debuff modifier
                var modifier = new Modifier(
                    Guid.NewGuid().ToString(),
                    $"Debuff from {ability.AbilityType.Name}",
                    ability.AbilityType.EffectAttribute,
                    -debuffValue, // Negative value for debuffs
                    ModifierType.Debuff,
                    duration
                );

                target.AddModifier(modifier);
                Debug.Log(
                    $"Applied {debuffValue} {ability.AbilityType.EffectAttribute} debuff to {target.Title} for {duration} turns"
                );
            }

            return true;
        }

        /// <summary>
        /// Trigger abilities of a specific type for a card
        /// </summary>
        private void TriggerAbilities(
            TriggerType triggerType,
            Card card,
            Dictionary<string, object> parameters = null
        )
        {
            if (card == null)
                return;

            // Get all abilities on the card with matching trigger type
            var abilities = GetAbilitiesForCard(card)
                .Where(a => a.AbilityType.Trigger == triggerType)
                .ToList();

            foreach (var ability in abilities)
            {
                if (ability.CanUse())
                {
                    // For abilities that don't require targets or auto-target
                    List<Card> targets = null;
                    if (ability.AbilityType.Targeting != TargetingType.None)
                    {
                        // Get appropriate targets based on targeting type
                        targets = GetTargetsForAbility(ability);
                    }

                    ExecuteAbility(ability, targets);
                }
            }
        }

        /// <summary>
        /// Trigger abilities of a specific type for a player
        /// </summary>
        private void TriggerAbilities(
            TriggerType triggerType,
            Player player,
            Dictionary<string, object> parameters = null
        )
        {
            if (player == null)
                return;

            // Get all abilities on cards controlled by the player with matching trigger type
            var abilities = new List<Ability>();

            // Add abilities from cards in play
            foreach (var card in player.GetCardsInPlay())
            {
                abilities.AddRange(
                    GetAbilitiesForCard(card).Where(a => a.AbilityType.Trigger == triggerType)
                );
            }

            foreach (var ability in abilities)
            {
                if (ability.CanUse())
                {
                    // For abilities that don't require targets or auto-target
                    List<Card> targets = null;
                    if (ability.AbilityType.Targeting != TargetingType.None)
                    {
                        // Get appropriate targets based on targeting type
                        targets = GetTargetsForAbility(ability);
                    }

                    ExecuteAbility(ability, targets);
                }
            }
        }

        /// <summary>
        /// Get all abilities for a card
        /// </summary>
        private List<Ability> GetAbilitiesForCard(Card card)
        {
            return activeAbilities.Where(a => a.OwnerCard == card).ToList();
        }

        /// <summary>
        /// Get appropriate targets for an ability based on its targeting type
        /// </summary>
        private List<Card> GetTargetsForAbility(Ability ability)
        {
            var abilityType = ability.AbilityType;
            var source = ability.OwnerCard;

            // Use the Owner property directly
            var sourcePlayer = source.Owner;
            if (sourcePlayer == null)
            {
                Debug.LogWarning($"Card {source.Title} has no owner set");
                return new List<Card>();
            }

            var opponentPlayer = matchManager.Opponent;

            switch (abilityType.Targeting)
            {
                case TargetingType.Self:
                    return new List<Card> { source };

                case TargetingType.AllAllies:
                    return sourcePlayer.GetCardsInPlay().Where(c => c != source).ToList();

                case TargetingType.AllEnemies:
                    return opponentPlayer.GetCardsInPlay();

                // Other targeting types would require more context or user input
                default:
                    return new List<Card>();
            }
        }
    }
}
