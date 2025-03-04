using System;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Core;

namespace Kardx.UI
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

        private Player player;
        private bool isHighlightingEmptySlots = false;

        public override void Initialize(MatchManager matchManager)
        {
            base.Initialize(matchManager);
            
            // Find all card slots in children
            cardSlots.Clear();
            cardSlots.AddRange(GetComponentsInChildren<PlayerCardSlot>());
            
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

        public override void UpdateBattlefield()
        {
            if (matchManager == null)
                return;
                
            var battlefield = matchManager.Player.Battlefield;
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
        /// Clear all highlights in the battlefield.
        /// </summary>
        public void ClearHighlights()
        {
            Debug.Log("[PlayerBattlefieldView] Clearing all highlights");
            isHighlightingEmptySlots = false;

            foreach (var slot in cardSlots)
            {
                slot.SetHighlightState(PlayerCardSlot.HighlightType.None);
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

            // Find detached card view
            CardView detachedCardView = null;
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
                    handView.UpdateHand(matchManager.Player.Hand);
                }

                return true;
            }

            Debug.Log($"[PlayerBattlefieldView] No detached card found for {card.Title}");
            return false;
        }
    }
}
