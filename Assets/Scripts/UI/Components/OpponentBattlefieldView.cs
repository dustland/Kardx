using System.Collections.Generic;
using UnityEngine;
using Kardx.Core;
using Kardx.UI.Scenes;

namespace Kardx.UI.Components
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
            
            // Ensure matchManager is not null before accessing Opponent
            if (matchManager != null)
            {
                this.opponent = matchManager.Opponent;
            }
            else
            {
                Debug.LogError("OpponentBattlefieldView: Cannot initialize with null MatchManager");
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
                slotGO.name = $"OpponentCardSlot_{i}";
                
                var cardSlot = slotGO.GetComponent<OpponentCardSlot>();
                if (cardSlot != null)
                {
                    cardSlot.Initialize(i, this);
                    cardSlots.Add(cardSlot);
                }
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
        
        public override void ClearCardHighlights()
        {
            foreach (var slot in cardSlots)
            {
                slot.ClearHighlight();
            }
        }
        
        public void ClearHighlights()
        {
            // Clear any highlights if present
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
    }
}
