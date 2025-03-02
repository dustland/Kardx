using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Kardx.Core;

namespace Kardx.UI.Components
{
    /// <summary>
    /// UI component representing a slot in the opponent's battlefield.
    /// Handles targeting for player abilities and attacks.
    /// </summary>
    public class OpponentCardSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;
        
        [SerializeField]
        private Transform cardContainer;
        
        private int slotIndex;
        private OpponentBattlefieldView battlefieldView;
        private CardView currentCardView;
        private bool isHighlighted = false;
        
        public int SlotIndex => slotIndex;
        public Transform CardContainer => cardContainer ?? transform;
        
        public void Initialize(int slotIndex, OpponentBattlefieldView battlefieldView)
        {
            this.slotIndex = slotIndex;
            this.battlefieldView = battlefieldView;
            
            // Initialize highlight
            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }
            
            // Initialize card container if not set
            if (cardContainer == null)
            {
                cardContainer = transform;
            }
        }
        
        public void SetHighlight(Color color, bool active)
        {
            isHighlighted = active;
            
            if (highlightImage != null)
            {
                highlightImage.color = color;
                highlightImage.enabled = active;
            }
        }
        
        public void ClearHighlight()
        {
            isHighlighted = false;
            
            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // Get the card view from the dragged object
            CardView sourceCardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (sourceCardView == null || sourceCardView.Card == null)
            {
                return;
            }

            // Get the attacking card
            var attackingCard = sourceCardView.Card;
            
            // Get access to the match manager directly from battlefield view
            var matchManager = battlefieldView.GetMatchManager();
            
            // Check if there's a card in this slot to target
            var targetPlayer = matchManager?.Opponent;
            var targetCard = targetPlayer?.Battlefield.GetCardAt(slotIndex);
            
            if (targetCard != null && matchManager != null)
            {
                // Try to attack from the attacking card to the target card
                bool attackSuccess = matchManager.AttackCard(attackingCard, targetCard);
                
                // The UI will be updated through events from MatchManager
            }
            
            // Return the attacking card to its original position (handled by UnitAttackDragHandler)
            
            // Clear any highlights
            battlefieldView.ClearHighlights();
        }
        
        // Method to clear the current card view
        public void ClearCardView()
        {
            // CardSlot should not be responsible for destroying GameObjects
            // It should only update its reference, while actual destruction
            // should be handled by MatchView
            
            // Simply clear the reference to the card
            currentCardView = null;
        }
        
        // Method to set a new card view
        public void SetCardView(CardView cardView)
        {
            currentCardView = cardView;
        }
    }
}
