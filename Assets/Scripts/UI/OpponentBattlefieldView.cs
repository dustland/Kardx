using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kardx.Core;

namespace Kardx.UI
{
    /// <summary>
    /// View component for the opponent's battlefield.
    /// </summary>
    public class OpponentBattlefieldView : BaseBattlefieldView
    {
        [SerializeField]
        private List<OpponentCardSlot> cardSlots = new List<OpponentCardSlot>();

        [SerializeField]
        private Color targetHighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.3f);

        private Player opponent;
        private bool isHighlightingCards = false;
        private Color validTargetHighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.3f);

        public override void Initialize(MatchManager matchManager)
        {
            base.Initialize(matchManager);
            
            // Find all card slots in children
            cardSlots.Clear();
            cardSlots.AddRange(GetComponentsInChildren<OpponentCardSlot>());
            
            // Initialize all card slots with their index and reference to this view
            for (int i = 0; i < cardSlots.Count; i++)
            {
                if (cardSlots[i] != null)
                {
                    cardSlots[i].Initialize(i, this);
                }
            }
        }

        private void OnDestroy()
        {
        }

        // private void OnCardDeployed(Card card, int slotIndex)
        // {
        //     if (card.Owner != matchManager.Opponent)
        //         return;

        //     // Update the UI for the deployed card
        //     if (slotIndex >= 0 && slotIndex < cardSlots.Count)
        //     {
        //         Debug.Log($"[OpponentBattlefieldView] Card deployed: {card.Title} at slot {slotIndex}");

        //         // Use the DeployCard method for opponent's battlefield
        //         bool handled = DeployCard(card, slotIndex);
        //         Debug.Log($"[OpponentBattlefieldView] Opponent card deployment {(handled ? "succeeded" : "not needed")}");
        //     }
        // }

        public override void UpdateBattlefield()
        {
            if (matchManager == null)
                return;

            var battlefield = matchManager.Opponent.Battlefield;
            if (battlefield == null)
                return;

            for (int i = 0; i < cardSlots.Count && i < Player.BATTLEFIELD_SLOT_COUNT; i++)
            {
                var card = battlefield.GetCardAt(i);
                UpdateCardInSlot(i, card);
            }
        }

        // Method to update a specific card slot with a card
        public void UpdateCardInSlot(int slotIndex, Card card)
        {
            if (slotIndex < 0 || slotIndex >= cardSlots.Count)
                return;

            var cardSlot = cardSlots[slotIndex];

            // This method should only update visual properties of the slot
            // based on the card's presence or absence

            // Note: Actual card creation/destruction should be handled by MatchView
            // The BattlefieldView should not create or destroy card GameObjects directly

            if (card == null)
            {
                // No card in this slot, make sure it's not highlighted
                cardSlot.ClearHighlight();
            }
            else
            {
                // Card is present, update visual state as needed
                // but don't create/destroy the card GameObject

                // Update the CardView if it exists
                CardView cardView = cardSlot.transform.GetComponentInChildren<CardView>();
                if (cardView != null && cardView.Card == card)
                {
                    cardView.UpdateUI();
                }

                // We might still need to highlight based on state
                // For example, if this card is targetable by an ability
                if (isHighlightingCards)
                {
                    cardSlot.SetHighlight(validTargetHighlightColor, true);
                }
                else
                {
                    cardSlot.ClearHighlight();
                }
            }
        }

        public void HighlightCards(Battlefield battlefield)
        {
            if (battlefield == null)
                return;

            for (int i = 0; i < cardSlots.Count && i < Player.BATTLEFIELD_SLOT_COUNT; i++)
            {
                // Only highlight slots that have cards
                Card card = battlefield.GetCardAt(i);
                if (card != null)
                {
                    cardSlots[i].SetHighlight(targetHighlightColor, true);
                }
                else
                {
                    cardSlots[i].ClearHighlight();
                }
            }
        }

        /// <summary>
        /// Clears all highlights from the battlefield.
        /// </summary>
        public override void ClearCardHighlights()
        {
            isHighlightingCards = false;

            foreach (var slot in cardSlots)
            {
                slot.ClearHighlight();
            }
        }

        public OpponentCardSlot GetSlot(int index)
        {
            if (index >= 0 && index < cardSlots.Count)
            {
                return cardSlots[index];
            }
            return null;
        }

        public Player GetOpponent()
        {
            return opponent;
        }

        public override void RemoveCard(Card card)
        {
            foreach (var slot in cardSlots)
            {
                // Find the CardView component in the children of the slot
                CardView cardView = slot.GetComponentInChildren<CardView>();
                if (cardView != null && cardView.Card == card)
                {
                    // Play death animation instead of immediately destroying
                    cardView.DieWithAnimation();
                    Debug.Log($"[OpponentBattlefieldView] Removed card UI for {card.Title} with animation");
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if a source card can target a specific opponent card
        /// </summary>
        public bool CanTargetCard(Card sourceCard, Card targetCard)
        {
            if (matchManager == null || sourceCard == null || targetCard == null)
                return false;

            // Check if it's the player's turn
            if (matchManager.CurrentPlayer != matchManager.Player)
                return false;

            // Check if the source card is on the player's battlefield
            if (!matchManager.Player.Battlefield.Contains(sourceCard))
                return false;

            // Check if the target card is on the opponent's battlefield
            if (!matchManager.Opponent.Battlefield.Contains(targetCard))
                return false;

            // Check if the source card has already attacked this turn
            if (sourceCard.HasAttackedThisTurn)
                return false;
                
            // Check if the player has enough credits for the operation cost
            if (sourceCard.OperationCost > matchManager.Player.Credits)
                return false;

            return true;
        }

        /// <summary>
        /// Get valid target cards for a source card
        /// </summary>
        public List<Card> GetValidTargets(Card sourceCard)
        {
            List<Card> validTargets = new List<Card>();

            // Check if source card can attack
            if (sourceCard == null || !sourceCard.IsUnitCard || sourceCard.HasAttackedThisTurn)
                return validTargets;

            // Get the opponent battlefield
            Battlefield opponentBattlefield = matchManager.Opponent.Battlefield;

            // Get valid targets
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                Card targetCard = opponentBattlefield.GetCardAt(i);
                if (targetCard != null)
                {
                    validTargets.Add(targetCard);
                }
            }

            return validTargets;
        }

        /// <summary>
        /// Highlights all valid attack targets for the given source card
        /// </summary>
        /// <param name="sourceCard">The card that would be attacking</param>
        public void HighlightValidTargets(Card sourceCard)
        {
            if (matchManager == null || sourceCard == null)
                return;

            // First clear any existing highlights
            ClearCardHighlights();

            // Only highlight if it's the player's turn and the source card is on the player's battlefield
            if (!matchManager.IsPlayerTurn() || !matchManager.Player.Battlefield.Contains(sourceCard))
                return;

            // Don't highlight if the card has already attacked
            if (sourceCard.HasAttackedThisTurn)
                return;

            // Don't highlight if player doesn't have enough credits for the operation cost
            if (sourceCard.OperationCost > matchManager.Player.Credits)
                return;

            isHighlightingCards = true;

            // Use a brighter, more noticeable color for valid targets
            Color highlightColor = new Color(1f, 0.2f, 0f, 0.9f); // Bright orange-red with high alpha

            Debug.Log($"[OpponentBattlefieldView] Highlighting valid targets for {sourceCard.Title}");

            // Highlight all cards on the opponent's battlefield that can be targeted
            for (int i = 0; i < cardSlots.Count; i++)
            {
                Card opponentCard = matchManager.Opponent.Battlefield.GetCardAt(i);
                if (opponentCard != null)
                {
                    // Check if this opponent card can be attacked
                    cardSlots[i].SetHighlight(highlightColor, true);
                    Debug.Log($"[OpponentBattlefieldView] Highlighted target: {opponentCard.Title}");
                }
                else
                {
                    // Make sure empty slots are NOT highlighted
                    cardSlots[i].ClearHighlight();
                }
            }
        }

        /// <summary>
        /// Deploys a card to a specific slot in the battlefield
        /// </summary>
        /// <param name="card">The card to deploy</param>
        /// <param name="slotIndex">The slot index to deploy to</param>
        /// <returns>True if the card was deployed, false otherwise</returns>
        public bool OnCardDeployed(Card card, int slotIndex)
        {
            if (card == null)
            {
                Debug.LogError("[OpponentBattlefieldView] Cannot deploy null card");
                return false;
            }

            // Find detached card view
            CardView detachedCardView = null;
            // Look for CardViews at the root level that match our card
            foreach (CardView cardView in FindObjectsByType<CardView>(FindObjectsSortMode.None))
            {
                // Check if this card view represents our card
                if (cardView.Card == card)
                {
                    detachedCardView = cardView;
                    break;
                }
            }

            // Verify the slot is valid
            if (slotIndex < 0 || slotIndex >= cardSlots.Count)
            {
                Debug.LogError($"[OpponentBattlefieldView] Invalid slot index: {slotIndex}");
                return false;
            }

            var targetSlot = cardSlots[slotIndex];
            if (targetSlot == null)
            {
                Debug.LogError($"[OpponentBattlefieldView] Slot {slotIndex} is null");
                return false;
            }

            if (detachedCardView != null)
            {
                Debug.Log($"[OpponentBattlefieldView] Deploying card {card.Title} to slot {slotIndex}");

                // Place the card in the slot
                detachedCardView.transform.SetParent(targetSlot.CardContainer, false);
                detachedCardView.transform.localPosition = Vector3.zero;

                // Set the card face-up in the UI
                detachedCardView.SetFaceDown(false);

                // Explicitly disable all drag handlers for opponent cards
                detachedCardView.SetDraggable(false);

                // Update opponent's hand to reflect the card being removed
                var handView = FindAnyObjectByType<HandView>();
                if (handView != null)
                {
                    handView.UpdateHand(matchManager.Opponent.Hand);
                }

                return true;
            }

            Debug.Log($"[OpponentBattlefieldView] No detached card found for {card.Title}");
            return false;
        }
    }
}
