using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;
using Kardx.Views.Hand;
using Kardx.Views.Match;

namespace Kardx.Controllers.DragHandlers
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
        private PlayerHandView handView;
        private PlayerBattlefieldView battlefieldView;
        private MatchManager matchManager;
        private OrderDropHandler orderDropHandler;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Find the required views
            handView = FindAnyObjectByType<PlayerHandView>();
            battlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
            matchManager = FindAnyObjectByType<MatchManager>();
            
            // Find the OrderDropHandler in the scene
            orderDropHandler = FindAnyObjectByType<OrderDropHandler>();
            if (orderDropHandler != null)
            {
                // Initially disable the OrderDropHandler component
                orderDropHandler.enabled = false;
            }
            else
            {
                Debug.LogWarning("[OrderDeployDragHandler] Could not find OrderDropHandler in scene");
            }
        }

        /// <summary>
        /// Initialize the drag handler with a reference to the card view.
        /// </summary>
        /// <param name="cardView">The card view this drag handler is associated with</param>
        public void Initialize(CardView cardView)
        {
            this.cardView = cardView;
            this.canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = cardView.gameObject.AddComponent<CanvasGroup>();
            }

            // Find the required views
            handView = FindAnyObjectByType<PlayerHandView>();
            battlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
            matchManager = FindAnyObjectByType<MatchManager>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("[OrderDeployDragHandler] Begin Drag");

            // If we can't drag, ignore the event
            if (!CanDrag())
            {
                return;
            }

            // Set the CardView.IsBeingDragged flag
            cardView.IsBeingDragged = true;

            // Store original parent and position
            originalParent = transform.parent;
            originalPosition = transform.position;

            // Reparent to root canvas so card appears in front of everything
            Canvas rootCanvas = FindRootCanvas();
            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform);
            }

            // Disable raycast on this object so we can detect drops
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }

            // Disable raycasting on battlefield slots to ensure order cards pass through
            if (battlefieldView != null)
            {
                battlefieldView.SetSlotsRaycastActive(false);
            }
            
            // Enable the OrderDropHandler component when we start dragging an order card
            if (orderDropHandler != null)
            {
                orderDropHandler.enabled = true;
                Debug.Log("[OrderDeployDragHandler] Enabled OrderDropHandler component");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!cardView.IsBeingDragged)
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

            // ALWAYS re-enable raycasting on battlefield slots, regardless of other conditions
            if (battlefieldView != null)
            {
                battlefieldView.SetSlotsRaycastActive(true);
                Debug.Log("[OrderDeployDragHandler] Re-enabled raycasting on battlefield slots");
            }
            
            // Disable the OrderDropHandler component when we end dragging
            if (orderDropHandler != null)
            {
                orderDropHandler.enabled = false;
                Debug.Log("[OrderDeployDragHandler] Disabled OrderDropHandler component");
            }

            if (!cardView.IsBeingDragged)
                return;

            // Reset the CardView.IsBeingDragged flag
            cardView.IsBeingDragged = false;

            // Re-enable raycasts now that we're done dragging
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            // Modified check: Consider it dropped on a target only if it's the OrderDropHandler,
            // explicitly ignore PlayerCardSlot components
            bool wasDroppedOnTarget = false;

            // Cast rays to find all objects under the pointer
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            // Check if any of the results is an OrderDropHandler
            foreach (var result in raycastResults)
            {
                if (result.gameObject.GetComponent<OrderDropHandler>() != null)
                {
                    Debug.Log("[OrderDeployDragHandler] Dropped on valid target");
                    wasDroppedOnTarget = true;
                    break;
                }
            }

            // If not dropped on a valid target, return the card to its original position
            if (!wasDroppedOnTarget)
            {
                Debug.Log("[OrderDeployDragHandler] Not dropped on valid target, returning to original position");
                ReturnToOriginalPosition();
            }
        }

        private bool CanDrag()
        {
            // Check if this is a valid order card
            if (cardView == null || cardView.Card == null || !cardView.Card.IsOrderCard)
            {
                Debug.LogWarning("[OrderDeployDragHandler] Cannot drag non-order card");
                return false;
            }

            // Check if the card is on the battlefield
            // This prevents the handler from taking over cards on the battlefield that are trying to attack
            if (cardView.Card.Owner == null)
            {
                Debug.LogWarning("[OrderDeployDragHandler] Cannot drag card with no owner");
                return false;
            }

            // Check if the card is on the battlefield by checking if it's in the player's battlefield
            if (matchManager != null && 
                matchManager.Player != null && 
                matchManager.Player.Battlefield != null && 
                matchManager.Player.Battlefield.Contains(cardView.Card))
            {
                Debug.LogWarning("[OrderDeployDragHandler] Cannot drag card that is already on the battlefield");
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

        // Add a method to find the root canvas
        private Canvas FindRootCanvas()
        {
            // Try to find the root canvas in the scene
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.isRootCanvas)
                {
                    return canvas;
                }
            }

            Debug.LogWarning("[OrderDeployDragHandler] No root canvas found in the scene");
            return null;
        }

        private void OnDisable()
        {
            // Safety measure: ensure battlefieldView slots are re-enabled when this component is disabled
            if (battlefieldView != null)
            {
                battlefieldView.SetSlotsRaycastActive(true);
                Debug.Log("[OrderDeployDragHandler] Re-enabled raycasting on battlefield slots (from OnDisable)");
            }
        }
    }
}
