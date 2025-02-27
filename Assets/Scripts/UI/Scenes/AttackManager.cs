using System;
using System.Collections;
using UnityEngine;

namespace Kardx.UI.Scenes
{
    using Kardx.Core;
    using Card = Kardx.Core.Card;

    public class AttackManager : MonoBehaviour
    {
        [SerializeField]
        private float attackAnimationDuration = 0.3f;

        [SerializeField]
        private float damageDisplayDuration = 1.0f;

        private MatchManager matchManager;
        private MatchView matchView;

        // Event to notify when an attack is completed
        public event Action<Card, Card, int, int> OnAttackCompleted;

        private void Awake()
        {
            // Find the MatchView component
            matchView = GetComponent<MatchView>();
            if (matchView == null)
            {
                Debug.LogError(
                    "[AttackManager] No MatchView component found on the same GameObject."
                );
            }
        }

        private void Start()
        {
            // Get the MatchManager from MatchView when it's available
            if (matchView != null && matchView.GetMatchManager() != null)
            {
                SetMatchManager(matchView.GetMatchManager());
            }
        }

        public void SetMatchManager(MatchManager manager)
        {
            matchManager = manager;
            Debug.Log("[AttackManager] MatchManager has been set.");
        }

        public bool CanAttack(Card attackerCard, Card defenderCard)
        {
            if (matchManager == null)
            {
                Debug.LogError(
                    "[AttackManager] Cannot check attack validity: MatchManager is null"
                );
                return false;
            }

            // Check if the attacker has already attacked this turn
            if (attackerCard.HasAttackedThisTurn)
                return false;

            // Check if the cards belong to different players
            if (attackerCard.Owner == defenderCard.Owner)
                return false;

            // Check if both cards are on the battlefield
            bool attackerOnBattlefield = false;
            bool defenderOnBattlefield = false;

            // Check attacker
            foreach (var card in attackerCard.Owner.Battlefield)
            {
                if (card == attackerCard)
                {
                    attackerOnBattlefield = true;
                    break;
                }
            }

            // Check defender
            foreach (var card in defenderCard.Owner.Battlefield)
            {
                if (card == defenderCard)
                {
                    defenderOnBattlefield = true;
                    break;
                }
            }

            if (!attackerOnBattlefield || !defenderOnBattlefield)
                return false;

            // Add any other attack validation rules here

            return true;
        }

        // Method to initiate an attack between two cards
        public void InitiateAttack(Card attackerCard, Card defenderCard)
        {
            Debug.Log($"[AttackManager] Initiating attack from {attackerCard.Title} to {defenderCard.Title}");
            
            // Check if the attack is valid
            if (!CanAttack(attackerCard, defenderCard))
            {
                Debug.LogWarning($"[AttackManager] Invalid attack from {attackerCard.Title} to {defenderCard.Title}");
                return;
            }
            
            // Process the attack
            ProcessAttack(attackerCard, defenderCard);
        }

        public void ProcessAttack(Card attackerCard, Card defenderCard)
        {
            if (matchManager == null)
            {
                Debug.LogError("[AttackManager] Cannot process attack: MatchManager is null");
                return;
            }

            if (!CanAttack(attackerCard, defenderCard))
            {
                Debug.LogWarning(
                    $"[AttackManager] Invalid attack from {attackerCard.Title} to {defenderCard.Title}"
                );
                return;
            }

            // Mark the attacker as having attacked this turn
            attackerCard.HasAttackedThisTurn = true;

            // Calculate damage
            int attackDamage = attackerCard.Attack;
            int counterAttackDamage = defenderCard.CounterAttack;

            // Apply damage to defender
            defenderCard.TakeDamage(attackDamage);

            // Apply counter-attack damage to attacker if defender is still alive
            if (defenderCard.CurrentDefence > 0)
            {
                attackerCard.TakeDamage(counterAttackDamage);
            }

            // Notify listeners about the attack completion
            OnAttackCompleted?.Invoke(
                attackerCard,
                defenderCard,
                attackDamage,
                counterAttackDamage
            );

            // Check for destroyed cards
            CheckForDestroyedCards(attackerCard);
            CheckForDestroyedCards(defenderCard);
        }

        private void CheckForDestroyedCards(Card card)
        {
            if (card.CurrentDefence <= 0)
            {
                // Card is destroyed
                if (card.Owner != null)
                {
                    card.Owner.DestroyCard(card);
                }
            }
        }
    }
}
