using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Kardx.Core;

namespace Kardx.UI.Components
{
    /// <summary>
    /// UI component representing a slot in the player's battlefield.
    /// </summary>
    public class PlayerCardSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;
        
        [SerializeField]
        private Transform cardContainer;
        
        private int slotIndex;
        private PlayerBattlefieldView battlefieldView;
        private CardView currentCardView;
        private bool isHighlighted = false;
        
        public int SlotIndex => slotIndex;
        public Transform CardContainer => cardContainer ?? transform;
        
        public void Initialize(int slotIndex, PlayerBattlefieldView battlefieldView)
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
            if (!isHighlighted)
            {
                Debug.Log("[PlayerCardSlot] Card slot is not a valid drop target");
                return;
            }
            
            // Get the card view from the dragged object
            var cardView = eventData.pointerDrag.GetComponent<CardView>();
            if (cardView == null || cardView.Card == null)
            {
                Debug.Log("[PlayerCardSlot] Dropped item is not a valid card");
                return;
            }

            // Get access to the match manager directly from battlefield view
            var matchManager = battlefieldView.GetMatchManager();
            if (matchManager != null)
            {
                Card card = cardView.Card;
                
                // Call the appropriate game logic method directly on MatchManager
                bool success = false;
                
                if (card.IsUnitCard)
                {
                    // For unit cards, deploy to the specific slot
                    success = matchManager.DeployUnitCard(card, slotIndex);
                }
                else if (card.IsOrderCard)
                {
                    // For order cards, just deploy 
                    success = matchManager.DeployOrderCard(card);
                }
                
                // If deployment was successful, we can destroy the dragged card
                // The UI will be updated through the OnCardDeployed event from MatchManager
                if (success && eventData.pointerDrag != null)
                {
                    Destroy(eventData.pointerDrag);
                }
            }
            
            // Clear all highlights - we access this through the battlefield view now 
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
