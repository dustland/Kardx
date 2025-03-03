using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kardx.Core;
using Kardx.UI.Scenes;

namespace Kardx.UI.Components
{
    /// <summary>
    /// Specialized drag handler for deploying order cards.
    /// </summary>
    public class OrderDeployDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private float dragOffset = 0.5f;
        
        [SerializeField]
        private Transform orderDropArea;
        
        private CardView cardView;
        private Vector3 originalPosition;
        private Transform originalParent;
        private CanvasGroup canvasGroup;
        private bool isDragging = false;
        private HandView handView;
        
        private void Awake()
        {
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            InitializeHandView();
        }
        
        private void InitializeHandView()
        {
            // Get the HandView through the containing hierarchy
            handView = GetComponentInParent<HandView>();
            
            if (handView == null)
            {
                // If we can't find it directly, try finding it in the scene
                handView = FindAnyObjectByType<HandView>();
                if (handView == null)
                {
                    Debug.LogError("OrderDeployDragHandler: Cannot find HandView in hierarchy or scene");
                }
                else
                {
                    Debug.Log("[OrderDeployDragHandler] Found HandView in scene directly");
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
            
            // Disable raycast blocking so we can detect drop targets underneath
            canvasGroup.blocksRaycasts = false;
            
            // Move to front of UI
            transform.SetParent(transform.root);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;
                
            // Move the card with the cursor
            transform.position = eventData.position + new Vector2(0, dragOffset);
        }
        
        /// <summary>
        /// Handles the end of drag operation. Only responsible for UI behavior.
        /// This method only handles returning the card to its original position if not dropped on a valid target.
        /// The actual deployment logic happens in OrderDropHandler.OnDrop.
        /// </summary>
        /// <param name="eventData">Data about the pointer event</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("[OrderDeployDragHandler] End Drag");
            
            if (!isDragging)
                return;
                
            isDragging = false;
            
            // Re-enable raycasts now that we're done dragging
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }
            
            // Check if we dropped on a valid target
            bool wasDroppedOnTarget = eventData.pointerEnter != null && 
                                     (eventData.pointerEnter.GetComponent<OrderDropHandler>() != null);
            
            // If not dropped on a valid target, return the card to its original position
            if (!wasDroppedOnTarget)
            {
                Debug.Log("[OrderDeployDragHandler] Not dropped on valid target, returning to original position");
                ReturnToOriginalPosition();
                
                // If we have a reference to HandView, we can ask it to clear highlights
                if (handView != null)
                {
                    handView.ClearHighlights();
                }
            }
            
            // Note: We don't need to handle deployment here
            // OrderDropHandler.OnDrop will handle the game logic for deployment
            // and the card will be handled appropriately after that
        }
        
        private bool CanDrag()
        {
            // Check if this is a valid order card
            if (cardView == null || cardView.Card == null || !cardView.Card.IsOrderCard)
            {
                Debug.LogWarning("[OrderDeployDragHandler] Cannot drag non-order card");
                return false;
            }
            
            return true;
        }
        
        private void ReturnToOriginalPosition()
        {
            Debug.Log("[OrderDeployDragHandler] Returning to original position");
            
            // Return the card to its original parent and position
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }
    }
}
