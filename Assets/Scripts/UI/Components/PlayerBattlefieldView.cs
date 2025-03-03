using System;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Core;

namespace Kardx.UI.Components
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
            
            // Ensure matchManager is not null before accessing Player
            if (matchManager != null)
            {
                this.player = matchManager.Player;
                
                // Subscribe to player events
                if (player != null)
                {
                    player.OnCardDeployed += OnCardDeployed;
                }
            }
            else
            {
                Debug.LogError("PlayerBattlefieldView: Cannot initialize with null MatchManager");
            }
        }

        public void InitializeSlots(GameObject cardSlotPrefab)
        {
            // Clear existing slots
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            cardSlots.Clear();
            
            // Create card slots
            for (int i = 0; i < Player.BATTLEFIELD_SLOT_COUNT; i++)
            {
                var slotGO = Instantiate(cardSlotPrefab, transform);
                slotGO.name = $"PlayerCardSlot_{i}";
                
                var cardSlot = slotGO.GetComponent<PlayerCardSlot>();
                if (cardSlot != null)
                {
                    cardSlot.Initialize(i, this);
                    cardSlots.Add(cardSlot);
                }
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from player events
            if (player != null)
            {
                player.OnCardDeployed -= OnCardDeployed;
            }
        }
        
        private void OnCardDeployed(Card card, int slotIndex)
        {
            // Update the UI for the deployed card
            if (slotIndex >= 0 && slotIndex < cardSlots.Count)
            {
                UpdateCardInSlot(slotIndex, card);
            }
        }
        
        public override void UpdateBattlefield(Battlefield battlefield)
        {
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
                cardSlot.ClearHighlight();
            }
        }
        
        /// <summary>
        /// Highlight empty slots in the battlefield.
        /// </summary>
        public void HighlightEmptySlots(Battlefield battlefield)
        {
            for (int i = 0; i < cardSlots.Count; i++)
            {
                if (battlefield.GetCardAt(i) == null)
                {
                    cardSlots[i].SetHighlightState(PlayerCardSlot.HighlightType.Available);
                }
            }
        }
        
        public override void ClearCardHighlights()
        {
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
            foreach (var slot in cardSlots)
            {
                slot.SetHighlightState(PlayerCardSlot.HighlightType.None);
            }
        }
        
        public PlayerCardSlot GetSlot(int index)
        {
            if (index >= 0 && index < cardSlots.Count)
            {
                return cardSlots[index];
            }
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
    }
}
