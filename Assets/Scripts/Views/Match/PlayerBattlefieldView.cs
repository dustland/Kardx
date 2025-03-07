using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;
using Kardx.Views.Hand;
using Kardx.Managers;
using UnityEngine.UI;

namespace Kardx.Views.Match
{
    /// <summary>
    /// View component for the player's battlefield.
    /// </summary>
    public class PlayerBattlefieldView : BaseBattlefieldView
    {
        [SerializeField]
        private List<PlayerCardSlot> cardSlots = new List<PlayerCardSlot>();

        [SerializeField]
        private Color validDropHighlightColor = new Color(0.0f, 1.0f, 0.0f, 0.3f);

        [SerializeField]
        private CardView cardPrefab; // Add card prefab reference for instantiation

        private Player player;
        private bool isHighlightingEmptySlots = false;

        public override void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;

            cardSlots = GetComponentsInChildren<PlayerCardSlot>().ToList();

            if (cardSlots == null || cardSlots.Count == 0)
            {
                string errorMsg = "[PlayerBattlefieldView] Failed to find any PlayerCardSlot components";
                Debug.LogError(errorMsg);
                throw new System.InvalidOperationException(errorMsg);
            }

            // Initialize all slots
            for (int i = 0; i < cardSlots.Count; i++)
            {
                cardSlots[i].Initialize(i, this);
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

            Battlefield battlefield = matchManager.Player.Battlefield;
            if (battlefield == null)
                return;

            for (int i = 0; i < cardSlots.Count && i < Battlefield.SLOT_COUNT; i++)
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

            // This method should only update the visual state of the slot
            // based on the card's presence or absence

            // Note: The actual card manipulation (creation/destruction/assignment)
            // should be handled by MatchView, not here

            if (card == null)
            {
                if (isHighlightingEmptySlots)
                {
                    cardSlot.SetHighlight(validDropHighlightColor, true);
                }
                else
                {
                    cardSlot.ClearHighlight();
                }
            }
            else
            {
                // Update the CardView if it exists
                CardView cardView = cardSlot.transform.GetComponentInChildren<CardView>();
                if (cardView != null && cardView.Card == card)
                {
                    cardView.UpdateUI();
                }

                cardSlot.ClearHighlight();
            }
        }

        /// <summary>
        /// Check if a card can be deployed to this battlefield (only unit cards)
        /// </summary>
        public bool IsValidCardForDeployment(Card card)
        {
            if (card == null)
                return false;

            // Only unit cards can be deployed to battlefield slots
            return card.IsUnitCard;
        }

        /// <summary>
        /// Highlight empty slots in the battlefield only if the card is a unit card.
        /// </summary>
        public void HighlightEmptySlotsForCard(Card card)
        {
            if (card == null)
            {
                Debug.LogError("[PlayerBattlefieldView] Cannot highlight empty slots - card is null");
                return;
            }

            // Only highlight slots for unit cards
            if (!IsValidCardForDeployment(card))
            {
                Debug.Log($"[PlayerBattlefieldView] Not highlighting slots for {card.Title} - not a unit card");
                ClearCardHighlights();
                return;
            }

            // Continue with normal battlefield slot highlighting
            if (matchManager == null)
            {
                Debug.LogError("[PlayerBattlefieldView] Cannot highlight empty slots - matchManager is null");
                return;
            }

            Battlefield battlefield = matchManager.Player.Battlefield;
            HighlightEmptySlots(battlefield);
        }

        /// <summary>
        /// Highlight empty slots in the battlefield.
        /// </summary>
        public void HighlightEmptySlots(Battlefield battlefield)
        {
            if (battlefield == null)
            {
                Debug.LogError("[PlayerBattlefieldView] Cannot highlight empty slots - battlefield is null");
                return;
            }

            Debug.Log($"[PlayerBattlefieldView] Highlighting empty slots. Total slots: {cardSlots.Count}");
            isHighlightingEmptySlots = true;

            int highlightedCount = 0;
            for (int i = 0; i < cardSlots.Count; i++)
            {
                if (battlefield.GetCardAt(i) == null)
                {
                    cardSlots[i].SetHighlightState(PlayerCardSlot.HighlightType.Available);
                    highlightedCount++;
                    Debug.Log($"[PlayerBattlefieldView] Highlighted slot {i}");
                }
            }

            Debug.Log($"[PlayerBattlefieldView] Highlighted {highlightedCount} empty slots");
        }

        public override void ClearCardHighlights()
        {
            Debug.Log("[PlayerBattlefieldView] Clearing card highlights");
            isHighlightingEmptySlots = false;

            foreach (var slot in cardSlots)
            {
                slot.ClearHighlight();
            }
        }

        /// <summary>
        /// Get a specific card slot by index
        /// </summary>
        public PlayerCardSlot GetSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < cardSlots.Count)
            {
                return cardSlots[slotIndex];
            }

            Debug.LogError($"[PlayerBattlefieldView] Slot index {slotIndex} is out of range (0-{cardSlots.Count - 1})");
            return null;
        }

        public Player GetPlayer()
        {
            return player;
        }

        public Color GetValidDropHighlightColor()
        {
            return validDropHighlightColor;
        }

        /// <summary>
        /// Get the MatchManager reference
        /// </summary>
        public new MatchManager GetMatchManager()
        {
            return matchManager;
        }

        /// <summary>
        /// Creates a UI representation for a card in the battlefield
        /// </summary>
        public CardView CreateCardUI(Card card, int slotIndex)
        {
            if (card == null || slotIndex < 0 || slotIndex >= cardSlots.Count)
            {
                Debug.LogError($"[PlayerBattlefieldView] Invalid parameters for CreateCardUI: card={card}, slotIndex={slotIndex}");
                return null;
            }

            PlayerCardSlot slot = cardSlots[slotIndex];
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
                    Debug.LogError("[PlayerBattlefieldView] Card prefab does not have a CardView component");
                    return null;
                }

                // Create the card view
                CardView newView = matchManager.CreateCardView(card, slotTransform, cardViewPrefab);
                return newView;
            }

            Debug.LogError("[PlayerBattlefieldView] Cannot create card view - missing prefab or manager");
            return null;
        }

        /// <summary>
        /// Attempts to visually deploy a detached CardView into the appropriate battlefield slot.
        /// Used when a card is deployed through drag and drop.
        /// </summary>
        /// <param name="card">The card model that was deployed</param>
        /// <param name="slotIndex">The slot index where the card was deployed</param>
        /// <returns>True if the card was successfully deployed, false otherwise</returns>
        public bool OnCardDeployed(Card card, int slotIndex)
        {
            if (card == null)
            {
                Debug.LogError("[PlayerBattlefieldView] Cannot deploy null card");
                return false;
            }

            // Early return for order cards - they don't go on the battlefield
            if (card.CardType.Category == CardCategory.Order)
            {
                Debug.Log($"[PlayerBattlefieldView] Skipping battlefield deployment for order card: {card.Title}");
                return true; // Return true since this isn't an error
            }

            // Find detached card view
            CardView detachedCardView = matchManager.ViewRegistry.GetCardView(card);

            // If we couldn't find the card in the registry, look for detached views the old way
            if (detachedCardView == null)
            {
                // Look for CardViews at the root level that match our card
                foreach (CardView cardView in FindObjectsByType<CardView>(FindObjectsSortMode.None))
                {
                    // Check if this card view represents our card and is likely detached (at the root)
                    if (cardView.Card == card && cardView.transform.parent == cardView.transform.root)
                    {
                        detachedCardView = cardView;
                        break;
                    }
                }
            }

            // Verify the slot is valid
            if (slotIndex < 0 || slotIndex >= cardSlots.Count)
            {
                Debug.LogError($"[PlayerBattlefieldView] Invalid slot index: {slotIndex}");
                return false;
            }

            var targetSlot = cardSlots[slotIndex];
            if (targetSlot == null)
            {
                Debug.LogError($"[PlayerBattlefieldView] Slot {slotIndex} is null");
                return false;
            }

            if (detachedCardView != null)
            {
                Debug.Log($"[PlayerBattlefieldView] Deploying card {card.Title} to slot {slotIndex}");

                // Place the card in the slot
                detachedCardView.transform.SetParent(targetSlot.CardContainer, false);
                detachedCardView.transform.localPosition = Vector3.zero;

                // Explicitly switch from deployment drag handlers to ability drag handlers
                detachedCardView.SwitchToAbilityDragHandler();

                // Update player's hand to reflect the card being removed
                var handView = FindAnyObjectByType<HandView>();
                if (handView != null)
                {
                    handView.UpdateHand();
                }

                ClearCardHighlights();

                return true;
            }
            else
            {
                // If we couldn't find a detached card view, we need to create one
                CardView newCardView = CreateCardUI(card, slotIndex);
                if (newCardView != null)
                {
                    return true;
                }

                Debug.Log($"[PlayerBattlefieldView] No detached card found for {card.Title}");
                return false;
            }
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
                    Debug.Log($"[PlayerBattlefieldView] Removed card UI for {card.Title} with animation");
                    break;
                }
            }
        }

        /// <summary>
        /// Enables or disables raycasting on all battlefield slots
        /// Used to prevent order cards from getting blocked by battlefield during drops
        /// </summary>
        public void SetSlotsRaycastActive(bool active)
        {
            // When dragging an order card, we may want to disable raycasting on battlefield slots
            foreach (var slot in cardSlots)
            {
                // Get all graphics on the slot
                Graphic[] graphics = slot.GetComponentsInChildren<Graphic>();
                foreach (var graphic in graphics)
                {
                    graphic.raycastTarget = active;
                }

                // Toggle collider if present
                Collider2D collider = slot.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = active;
                }
            }
        }

        public void SetHighlightLogic(bool highlightEnabled)
        {
            isHighlightingEmptySlots = highlightEnabled;
            UpdateHighlights();
        }

        /// <summary>
        /// Update the visual highlights of all card slots based on game state
        /// </summary>
        public void UpdateHighlights()
        {
            if (matchManager == null) return;

            // Check if it's the player's turn
            bool isPlayerTurn = matchManager.IsPlayerTurn();
            bool hasCredits = matchManager.Player.Credits > 0;

            // Only highlight empty slots during player's turn if they have credits
            bool canDeploy = isPlayerTurn && hasCredits;

            // Update each slot's highlight state
            foreach (var slot in cardSlots)
            {
                // Only highlight empty slots
                bool isEmpty = !slot.HasCard();
                if (canDeploy && isEmpty)
                {
                    slot.SetHighlightState(PlayerCardSlot.HighlightType.Available);
                }
                else
                {
                    slot.SetHighlightState(PlayerCardSlot.HighlightType.None);
                }
            }
        }

        /// <summary>
        /// Gets the number of cards on the battlefield
        /// Properly implemented method with Count() call
        /// </summary>
        /// <returns>The number of cards on the battlefield</returns>
        public int GetCardCount()
        {
            if (matchManager == null || matchManager.Board == null) return 0;
            return matchManager.Board.CurrentTurnPlayer.Battlefield.Cards.Count();
        }

        // Update GetCardViews to use the registry when possible
        public override CardView[] GetCardViews()
        {
            List<CardView> cardViews = new List<CardView>();

            var battlefield = matchManager?.Player?.Battlefield;
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
                foreach (var slot in cardSlots)
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
            if (position < 0 || position >= cardSlots.Count)
                return null;

            // First try to get the card from the model
            var battlefield = matchManager?.Player?.Battlefield;
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
            return cardSlots[position].GetCardView();
        }

        /// <summary>
        /// Get all card slots in this battlefield
        /// </summary>
        public PlayerCardSlot[] GetSlots()
        {
            return cardSlots.ToArray();
        }
    }
}
