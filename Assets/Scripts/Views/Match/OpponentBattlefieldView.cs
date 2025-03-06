using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;
using Kardx.Managers;
using UnityEngine.UI;

namespace Kardx.Views.Match
{
    /// <summary>
    /// View component for the opponent's battlefield.
    /// </summary>
    public class OpponentBattlefieldView : BaseBattlefieldView
    {
        [SerializeField]
        private List<OpponentCardSlot> slots = new List<OpponentCardSlot>();

        [SerializeField]
        private Color targetHighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.3f);

        [SerializeField]
        private Color validAbilityTargetColor = new Color(1.0f, 0.5f, 0.0f, 0.3f); // Orange highlight

        [SerializeField]
        private CardView cardPrefab; // Add card prefab reference for instantiation

        private Player opponentPlayer;
        private bool isHighlightingCards = false;
        private Color validTargetHighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.3f);

        public override void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;

            // Create slot list if needed
            if (slots == null || slots.Count == 0)
            {
                Debug.LogWarning("[OpponentBattlefieldView] No slots assigned in inspector, creating dynamically");
                slots = GetComponentsInChildren<OpponentCardSlot>(true).ToList();
            }

            // Initialize all slots
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Initialize(i, this);
            }

            // No event subscriptions needed - synchronization handles updates
        }

        private void OnDestroy()
        {
        }

        public override void UpdateBattlefield()
        {
            if (matchManager == null)
                return;

            Battlefield battlefield = matchManager.Opponent.Battlefield;
            if (battlefield == null)
                return;

            for (int i = 0; i < slots.Count && i < Battlefield.SLOT_COUNT; i++)
            {
                var card = battlefield.GetCardAt(i);
                UpdateCardInSlot(i, card);
            }
        }

        // Method to update a specific card slot with a card
        public void UpdateCardInSlot(int slotIndex, Card card)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return;

            var cardSlot = slots[slotIndex];

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

            for (int i = 0; i < slots.Count && i < Battlefield.SLOT_COUNT; i++)
            {
                // Only highlight slots that have cards
                Card card = battlefield.GetCardAt(i);
                if (card != null)
                {
                    slots[i].SetHighlight(targetHighlightColor, true);
                }
                else
                {
                    slots[i].ClearHighlight();
                }
            }
        }

        /// <summary>
        /// Clears all highlights from the battlefield.
        /// </summary>
        public override void ClearCardHighlights()
        {
            isHighlightingCards = false;

            foreach (var slot in slots)
            {
                slot.ClearHighlight();
            }
        }

        public OpponentCardSlot GetSlot(int index)
        {
            if (index >= 0 && index < slots.Count)
            {
                return slots[index];
            }
            return null;
        }

        public Player GetOpponent()
        {
            return opponentPlayer;
        }

        public override void RemoveCard(Card card)
        {
            foreach (var slot in slots)
            {
                // Find the CardView component in the children of the slot
                CardView cardView = slot.GetComponentInChildren<CardView>();
                if (cardView != null && cardView.Card == card)
                {
                    // Play death animation
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
            for (int i = 0; i < slots.Count; i++)
            {
                Card opponentCard = matchManager.Opponent.Battlefield.GetCardAt(i);
                if (opponentCard != null)
                {
                    // Check if this opponent card can be attacked
                    slots[i].SetHighlight(highlightColor, true);
                    Debug.Log($"[OpponentBattlefieldView] Highlighted target: {opponentCard.Title}");
                }
                else
                {
                    // Make sure empty slots are NOT highlighted
                    slots[i].ClearHighlight();
                }
            }
        }

        /// <summary>
        /// Creates a card UI element from the card data
        /// </summary>
        /// <param name="card">The card to create UI for</param>
        /// <param name="slotIndex">The index of the slot to place the card in</param>
        /// <returns>The created card view</returns>
        public CardView CreateCardUI(Card card, int slotIndex)
        {
            if (card == null || slotIndex < 0 || slotIndex >= slots.Count)
            {
                Debug.LogError($"[OpponentBattlefieldView] Invalid parameters for CreateCardUI: card={card}, slotIndex={slotIndex}");
                return null;
            }

            OpponentCardSlot slot = slots[slotIndex];
            Transform slotTransform = slot.transform;

            // Check if there's already a view for this card
            CardView existingView = null;
            if (matchManager != null && matchManager.ViewRegistry != null)
            {
                existingView = matchManager.ViewRegistry.GetCardView(card);
            }

            if (existingView != null)
            {
                // Reparent the existing view to this slot
                existingView.transform.SetParent(slotTransform);
                existingView.transform.localPosition = Vector3.zero;
                existingView.transform.localRotation = Quaternion.identity;
                existingView.transform.localScale = Vector3.one;

                // Make sure it's visible and not face down
                existingView.SetFaceDown(false);

                return existingView;
            }

            // Create a new view if we don't have an existing one
            if (cardPrefab != null && matchManager != null)
            {
                CardView cardViewPrefab = cardPrefab.GetComponent<CardView>();
                if (cardViewPrefab == null)
                {
                    Debug.LogError("[OpponentBattlefieldView] Card prefab does not have a CardView component");
                    return null;
                }

                // Create the card view
                CardView newView = matchManager.CreateCardView(card, slotTransform, cardViewPrefab);
                return newView;
            }

            Debug.LogError("[OpponentBattlefieldView] Cannot create card view - missing prefab or manager");
            return null;
        }

        /// <summary>
        /// Handles a card being deployed to the battlefield
        /// </summary>
        public bool OnCardDeployed(Card card, int slotIndex)
        {
            if (card == null)
            {
                Debug.LogError("[OpponentBattlefieldView] Cannot deploy null card");
                return false;
            }

            // Early return for order cards - they don't go on the battlefield
            if (card.CardType.Category == CardCategory.Order)
            {
                Debug.Log($"[OpponentBattlefieldView] Skipping battlefield deployment for order card: {card.Title}");
                return true; // Return true since this isn't an error
            }

            // Try to find the card view in the registry first
            CardView cardView = matchManager.ViewRegistry.GetCardView(card);

            // Verify the slot is valid
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                Debug.LogError($"[OpponentBattlefieldView] Invalid slot index: {slotIndex}");
                return false;
            }

            var targetSlot = slots[slotIndex];
            if (targetSlot == null)
            {
                Debug.LogError($"[OpponentBattlefieldView] Slot {slotIndex} is null");
                return false;
            }

            if (cardView != null)
            {
                Debug.Log($"[OpponentBattlefieldView] Deploying card {card.Title} to slot {slotIndex}");

                // Place the card in the slot
                cardView.transform.SetParent(targetSlot.CardContainer, false);
                cardView.transform.localPosition = Vector3.zero;

                return true;
            }
            else
            {
                // If we couldn't find a card view, we need to create one
                CardView newCardView = CreateCardUI(card, slotIndex);
                if (newCardView != null)
                {
                    return true;
                }

                Debug.Log($"[OpponentBattlefieldView] No card view found for {card.Title}");
                return false;
            }
        }

        /// <summary>
        /// Highlights opponent cards that can be attacked by the selected player card
        /// </summary>
        /// <param name="targetHighlightEnabled">Whether to highlight valid targets</param>
        public void SetHighlightTargets(bool targetHighlightEnabled)
        {
            isHighlightingCards = targetHighlightEnabled;
            UpdateHighlights();
        }

        /// <summary>
        /// Updates the visual highlights of all card slots based on game state
        /// </summary>
        public void UpdateHighlights()
        {
            if (matchManager == null) return;

            // Check if it's the player's turn and combat phase
            bool isPlayerTurn = matchManager.IsPlayerTurn();

            // Only highlight opponent cards during player's combat phase
            bool canTarget = isPlayerTurn;

            // Update each slot's highlight state
            foreach (var slot in slots)
            {
                slot.UpdateHighlight(canTarget && slot.HasCard());
            }
        }

        // Update GetCardViews to use the registry when possible
        public override CardView[] GetCardViews()
        {
            List<CardView> cardViews = new List<CardView>();

            Battlefield battlefield = matchManager?.Opponent?.Battlefield;
            if (battlefield == null) return cardViews.ToArray();

            // Try to get cards from the registry first
            foreach (var card in battlefield.Cards)
            {
                var cardView = matchManager.ViewRegistry.GetCardView(card);
                if (cardView != null)
                {
                    cardViews.Add(cardView);
                }
            }

            // If we couldn't find all cards in the registry, also search in the hierarchy
            if (cardViews.Count < battlefield.Cards.Count())
            {
                foreach (var slot in slots)
                {
                    var cardView = slot.GetCardView();
                    if (cardView != null && !cardViews.Contains(cardView))
                    {
                        cardViews.Add(cardView);
                    }
                }
            }

            return cardViews.ToArray();
        }

        public override CardView GetCardViewAt(int position)
        {
            if (position < 0 || position >= slots.Count)
                return null;

            // First try to get the card from the model
            Battlefield battlefield = matchManager?.Opponent?.Battlefield;
            if (battlefield != null)
            {
                var card = battlefield.GetCardAt(position);
                if (card != null)
                {
                    // Try to get from registry first
                    var cardView = matchManager.ViewRegistry.GetCardView(card);
                    if (cardView != null)
                    {
                        return cardView;
                    }
                }
            }

            // Fallback to searching in the hierarchy
            return slots[position].GetCardView();
        }

        /// <summary>
        /// Get all card slots in this battlefield
        /// </summary>
        public OpponentCardSlot[] GetSlots()
        {
            return slots.ToArray();
        }
    }
}
