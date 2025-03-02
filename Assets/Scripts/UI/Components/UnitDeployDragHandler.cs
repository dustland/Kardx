using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kardx.UI.Scenes;

namespace Kardx.UI.Components
{
    using Kardx.Core;

    /// <summary>
    /// Specialized drag handler for deploying unit cards.
    /// </summary>
    public class UnitDeployDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private float dragOffset = 0.5f;

        private CardView cardView;
        private MatchManager matchManager;
        private Vector3 originalPosition;
        private Transform originalParent;
        private CanvasGroup canvasGroup;
        private bool isDragging = false;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Get the MatchManager through the containing hierarchy
            var matchView = GetComponentInParent<MatchView>();
            if (matchView != null)
            {
                matchManager = matchView.MatchManager;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag())
                return;

            isDragging = true;
            originalPosition = transform.position;
            originalParent = transform.parent;

            // Disable raycast blocking so we can detect drop targets underneath
            canvasGroup.blocksRaycasts = false;
            
            // Move to front of UI
            transform.SetParent(transform.root);
            
            // Highlight valid drop targets through MatchView
            var matchView = GetComponentInParent<MatchView>();
            if (matchView != null)
            {
                matchView.HighlightValidUnitDropSlots(cardView.Card);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            // Move the card with the cursor
            transform.position = eventData.position + new Vector2(0, dragOffset);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            isDragging = false;
            
            // Re-enable raycast blocking
            canvasGroup.blocksRaycasts = true;
            
            // Check if it was dropped on a valid target
            bool wasDroppedOnTarget = false;
            PlayerCardSlot targetSlot = null;
            
            if (eventData.pointerEnter != null)
            {
                targetSlot = eventData.pointerEnter.GetComponent<PlayerCardSlot>();
                wasDroppedOnTarget = targetSlot != null;
            }
            
            if (wasDroppedOnTarget && targetSlot != null)
            {
                // Get the card
                Card card = cardView.Card;
                
                if (matchManager != null)
                {
                    // Try to deploy the card directly in the game state
                    bool deploySuccess = matchManager.DeployUnitCard(card, targetSlot.SlotIndex);
                    
                    // If deployment was successful, the card is removed from hand and will be created 
                    // in the battlefield by UpdateUI, so we can destroy this instance
                    if (deploySuccess)
                    {
                        Destroy(this.gameObject);
                    }
                    else
                    {
                        // If deployment failed, return to original position
                        transform.SetParent(originalParent);
                        transform.position = originalPosition;
                    }
                }
            }
            else
            {
                // If not dropped on a valid target, return to original position
                transform.SetParent(originalParent);
                transform.position = originalPosition;
            }
            
            // Get the parent battlefield view to clear highlights if possible
            // Otherwise we'll try to find a MatchView to do it
            var playerBattlefieldView = GetComponentInParent<PlayerBattlefieldView>();
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.ClearHighlights();
            }
            else
            {
                var matchView = GetComponentInParent<MatchView>();
                if (matchView != null)
                {
                    matchView.ClearAllHighlights();
                }
            }
        }

        private bool CanDrag()
        {
            if (cardView == null || cardView.Card == null || matchManager == null)
                return false;
                
            var card = cardView.Card;
            
            // Only unit cards in player's hand can be dragged during player's turn
            return card.IsUnitCard && 
                   matchManager.IsPlayerTurn() && 
                   matchManager.CurrentPlayer.Hand.Contains(card);
        }
    }
}
