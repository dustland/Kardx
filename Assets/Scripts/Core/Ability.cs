using System;
using System.Collections.Generic;
using System.Linq;

namespace Kardx.Core
{
    public class Ability
    {
        private Guid instanceId; // Unique instance identifier
        private AbilityType abilityType; // Reference to static ability definition
        private Card ownerCard; // Card that owns this ability
        private bool isActive; // Whether the ability is currently active
        private int usesThisTurn; // Number of times used this turn
        private int totalUses; // Total uses since ability was created
        private int turnsSinceLastUse; // Turns since ability was last used
        private DateTime lastUseTime; // Last time the ability was used

        // Public properties
        public Guid InstanceId => instanceId;
        public AbilityType AbilityType => abilityType;
        public Card OwnerCard => ownerCard;
        public bool IsActive => isActive;
        public int UsesThisTurn => usesThisTurn;
        public int TotalUses => totalUses;
        public int TurnsSinceLastUse => turnsSinceLastUse;
        public DateTime LastUseTime => lastUseTime;

        // Constructor
        public Ability(AbilityType abilityType, Card ownerCard)
        {
            this.abilityType = abilityType ?? throw new ArgumentNullException(nameof(abilityType));
            this.ownerCard = ownerCard ?? throw new ArgumentNullException(nameof(ownerCard));
            this.instanceId = Guid.NewGuid();
            this.isActive = true;
            this.usesThisTurn = 0;
            this.totalUses = 0;
            this.turnsSinceLastUse = int.MaxValue; // Never used
            this.lastUseTime = DateTime.MinValue; // Never used
        }

        // Check if ability can be used based on cooldown, uses per turn, etc.
        public bool CanUse()
        {
            if (!isActive)
                return false;

            // Check if card is face up if required
            if (abilityType.RequiresFaceUp && ownerCard.FaceDown)
                return false;

            // Check cooldown
            if (turnsSinceLastUse < abilityType.CooldownTurns)
                return false;

            // Check uses per turn limitation
            if (abilityType.UsesPerTurn > 0 && usesThisTurn >= abilityType.UsesPerTurn)
                return false;

            // Check if it has reached maximum total uses
            if (abilityType.UsesPerMatch > 0 && totalUses >= abilityType.UsesPerMatch)
                return false;

            // Check if the owner can pay operation cost
            // Note: This would typically require access to the player's resources,
            // which would be handled by the AbilitySystem when executing

            return true;
        }

        // Get valid targets for this ability
        public List<Card> GetValidTargets(List<Card> potentialTargets)
        {
            if (potentialTargets == null)
                return new List<Card>();

            // Filter targets based on targeting type, range, face down status, etc.
            return potentialTargets
                .Where(target =>
                {
                    // Skip if target is face down and ability can't target face down cards
                    if (target.FaceDown && !abilityType.CanTargetFaceDown)
                        return false;

                    // Additional target validation logic would go here
                    // E.g., range checking, ally/enemy filtering, etc.

                    return true;
                })
                .ToList();
        }

        // Mark the ability as used, updating usage statistics
        public void MarkAsUsed()
        {
            usesThisTurn++;
            totalUses++;
            turnsSinceLastUse = 0;
            lastUseTime = DateTime.Now;
        }

        // Reset turn-based usage counters
        public void OnTurnStart()
        {
            usesThisTurn = 0;
            turnsSinceLastUse++;
        }

        // Activate or deactivate the ability
        public void SetActive(bool active)
        {
            isActive = active;
        }

        // Check if the ability is on cooldown
        public bool IsOnCooldown()
        {
            return turnsSinceLastUse < abilityType.CooldownTurns;
        }

        // Get cooldown turns remaining
        public int GetCooldownRemaining()
        {
            return Math.Max(0, abilityType.CooldownTurns - turnsSinceLastUse);
        }
    }
}
