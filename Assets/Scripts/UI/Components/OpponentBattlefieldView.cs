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
        
        private MatchView matchView;
        
        public void Initialize(MatchView matchView, GameObject cardSlotPrefab)
        {
            this.matchView = matchView;
            
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
                cardSlots[i].UpdateCardDisplay(card);
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
                // If there's highlight functionality in the opponent slots in the future
                // Add code to clear them here
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
        
        public MatchView GetMatchView()
        {
            return matchView;
        }
    }
}
