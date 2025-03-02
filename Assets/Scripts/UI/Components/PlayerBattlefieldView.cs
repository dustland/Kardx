using System;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Core;
using Kardx.UI.Scenes;

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
        
        private MatchView matchView;
        private Player player;
        
        private void Awake()
        {
            matchView = GetComponentInParent<MatchView>();
        }
        
        public void Initialize(MatchView matchView, GameObject cardSlotPrefab, Player player = null)
        {
            this.matchView = matchView;
            this.player = player;
            
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
            
            // Subscribe to player events
            if (player != null)
            {
                player.OnCardDeployed += OnCardDeployed;
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
                cardSlots[slotIndex].UpdateCardDisplay(card);
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
        
        public void HighlightEmptySlots(Battlefield battlefield)
        {
            if (battlefield == null)
                return;
                
            for (int i = 0; i < cardSlots.Count; i++)
            {
                bool isEmpty = battlefield.GetCardAt(i) == null;
                if (isEmpty)
                {
                    cardSlots[i].SetHighlight(validDropHighlightColor, true);
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
            foreach (var slot in cardSlots)
            {
                slot.ClearHighlight();
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
        
        public MatchView GetMatchView()
        {
            return matchView;
        }
        
        public Player GetPlayer()
        {
            return player;
        }
    }
}
