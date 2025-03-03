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
        private PlayerBattlefieldView playerBattlefieldView;
        private Vector3 originalPosition;
        private Transform originalParent;
        private CanvasGroup canvasGroup;
        private bool isDragging = false;
        private PlayerCardSlot currentHoverSlot;

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
                playerBattlefieldView = matchView.GetComponentInChildren<PlayerBattlefieldView>();
            }
            else
            {
                // If we can't find it directly, try finding it in the scene
                matchView = FindAnyObjectByType<MatchView>();
                if (matchView != null)
                {
                    matchManager = matchView.MatchManager;
                    playerBattlefieldView = matchView.GetComponentInChildren<PlayerBattlefieldView>();
                }
                else
                {
                    Debug.LogError("UnitDeployDragHandler: Cannot find MatchView in hierarchy or scene");
                }
            }
            
            if (playerBattlefieldView == null)
            {
                playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
                if (playerBattlefieldView == null)
                {
                    Debug.LogError("UnitDeployDragHandler: Cannot find PlayerBattlefieldView");
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag())
                return;

            isDragging = true;
            originalPosition = transform.position;
            originalParent = transform.parent;
            currentHoverSlot = null;

            // Disable raycast blocking so we can detect drop targets underneath
            canvasGroup.blocksRaycasts = false;
            
            // Move to front of UI
            transform.SetParent(transform.root);
            
            // Highlight valid drop targets directly using PlayerBattlefieldView
            if (playerBattlefieldView != null && matchManager != null)
            {
                playerBattlefieldView.HighlightEmptySlots(matchManager.Player.Battlefield);
            }
            else
            {
                // Fallback to the MatchView if we couldn't find PlayerBattlefieldView
                var matchView = FindAnyObjectByType<MatchView>();
                if (matchView != null)
                {
                    matchView.HighlightValidUnitDropSlots(cardView.Card);
                }
                else
                {
                    Debug.LogError("UnitDeployDragHandler: Cannot find MatchView for highlighting");
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            // Move the card with the cursor
            transform.position = eventData.position + new Vector2(0, dragOffset);
            
            // Check if we're hovering over a card slot
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                PlayerCardSlot hoveredSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<PlayerCardSlot>();
                
                // If we've changed which slot we're hovering over
                if (hoveredSlot != currentHoverSlot)
                {
                    // Reset any enhanced highlight on the previous slot
                    if (currentHoverSlot != null)
                    {
                        // Set back to normal "Available" highlight
                        currentHoverSlot.SetHighlightState(PlayerCardSlot.HighlightType.Available);
                    }
                    
                    // Set enhanced highlight for the new slot if it's valid
                    if (hoveredSlot != null)
                    {
                        // Check if the slot is empty
                        if (matchManager != null && 
                            matchManager.Player != null && 
                            matchManager.Player.Battlefield.GetCardAt(hoveredSlot.SlotIndex) == null)
                        {
                            // Set to "DropTarget" highlight state
                            hoveredSlot.SetHighlightState(PlayerCardSlot.HighlightType.DropTarget);
                            currentHoverSlot = hoveredSlot;
                        }
                    }
                    else
                    {
                        currentHoverSlot = null;
                    }
                }
            }
            else if (currentHoverSlot != null)
            {
                // We're no longer hovering over any slot, reset to "Available"
                currentHoverSlot.SetHighlightState(PlayerCardSlot.HighlightType.Available);
                currentHoverSlot = null;
            }
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
            
            // Clear highlights directly through PlayerBattlefieldView if possible
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.ClearHighlights();
            }
            else
            {
                // Try to find the MatchView directly in the scene
                var matchView = FindAnyObjectByType<MatchView>();
                if (matchView != null)
                {
                    matchView.ClearAllHighlights();
                }
                else
                {
                    Debug.LogError("UnitDeployDragHandler: Cannot find MatchView for clearing highlights");
                }
            }
            
            // Reset current hover slot
            currentHoverSlot = null;
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
