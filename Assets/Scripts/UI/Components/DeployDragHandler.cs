using System;
using System.Linq;
using Kardx.UI.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;
    using CardCategory = Kardx.Core.CardCategory;

    public class DeployDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // Events
        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        // References
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private CardView cardView;
        private MatchView matchView;

        // Drag state
        private Vector3 originalPosition;
        private Transform originalParent;
        private PlayerCardSlot lastHighlightedSlot;

        private void Awake()
        {
            // Find Canvas by searching up through all parents
            canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                Debug.LogError(
                    "[DeployDragHandler] No Canvas found in parents. Card dragging won't work."
                );
            }

            // Find MatchView at the root Canvas level
            matchView = canvas?.GetComponent<MatchView>();
            if (matchView == null)
            {
                Debug.LogError(
                    "[DeployDragHandler] No MatchView found on Canvas. Card dragging won't work."
                );
            }

            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Only check if we have the CardView component
            if (cardView == null)
            {
                Debug.LogError("[DeployDragHandler] CardView component is missing");
            }

            // Ensure we have a CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            // Make sure it blocks raycasts by default
            canvasGroup.blocksRaycasts = true;

            Debug.Log(
                $"[DeployDragHandler] Initialized - Canvas: {canvas?.name ?? "null"}, MatchView: {matchView?.name ?? "null"}"
            );
        }

        private void OnEnable()
        {
            // No need for flag reset
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // If this component is disabled, don't handle the event
            if (!enabled)
                return;

            Debug.Log("[DeployDragHandler] OnBeginDrag");

            if (canvas == null)
            {
                Debug.LogError("[DeployDragHandler] Cannot drag: No Canvas reference");
                return;
            }

            if (matchView == null)
            {
                Debug.LogError("[DeployDragHandler] Cannot drag: No MatchView found on Canvas");
                return;
            }

            // Check card status when we actually need to drag
            if (cardView == null)
            {
                Debug.LogError("[DeployDragHandler] Cannot drag: CardView component is missing");
                return;
            }

            if (cardView.Card == null)
            {
                Debug.LogError("[DeployDragHandler] Cannot drag: No card data available");
                return;
            }

            // Check if the card can be deployed based on its type
            bool canDeploy =
                cardView.Card.CardType.Category == CardCategory.Unit
                    ? matchView.CanDeployUnitCard(cardView.Card, -1) // Pass a default value, actual slot is determined when dropping
                : cardView.Card.CardType.Category == CardCategory.Order
                    ? matchView.CanDeployOrderCard(cardView.Card)
                : false;

            if (!canDeploy)
            {
                Debug.Log("[DeployDragHandler] Cannot drag: cannot deploy card");
                return;
            }

            originalPosition = transform.position;
            originalParent = transform.parent;
            transform.SetParent(canvas.transform);

            // We disable blocksRaycasts during drag so that:
            // 1. The card itself doesn't block raycasts to potential drop targets underneath
            // 2. This allows the EventSystem to detect the drop zones (PlayerCardSlot components)
            // NOTE: This is the standard approach for drag and drop in Unity UI
            canvasGroup.blocksRaycasts = false;

            OnDragStarted?.Invoke();
            Debug.Log(
                $"[DeployDragHandler] Started dragging card: {cardView.Card.Title}, blocksRaycasts set to false"
            );
        }

        public void OnDrag(PointerEventData eventData)
        {
            // If this component is disabled, don't handle the event
            if (!enabled)
                return;

            // Update the card's position to follow the mouse/touch
            transform.position = eventData.position;

            // For Order cards, highlight the order drop zone
            if (cardView.Card.CardType.Category == CardCategory.Order)
            {
                // Find and highlight the OrderDropHandler if it exists
                var orderDropHandler = canvas.GetComponentInChildren<OrderDropHandler>();
                if (orderDropHandler != null)
                {
                    orderDropHandler.SetHighlight(true);

                    // Clear any previously highlighted unit slot
                    if (lastHighlightedSlot != null)
                    {
                        lastHighlightedSlot.SetHighlight(Color.clear, false);
                        lastHighlightedSlot = null;
                    }
                }
                return;
            }

            // For Unit cards, find the slot under the pointer
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            // Clear previous highlight
            if (lastHighlightedSlot != null)
            {
                lastHighlightedSlot.SetHighlight(Color.clear, false);
                lastHighlightedSlot = null;
            }

            // Find the first valid slot
            foreach (var hit in raycastResults)
            {
                var slot = hit.gameObject.GetComponent<PlayerCardSlot>();
                if (slot != null && slot.IsValidDropTarget(cardView.Card))
                {
                    slot.SetHighlight(Color.green, true);
                    lastHighlightedSlot = slot;
                    break;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // If this component is disabled, don't handle the event
            if (!enabled)
                return;

            if (originalParent == null)
                return;

            // Clear any remaining highlight
            if (lastHighlightedSlot != null)
            {
                lastHighlightedSlot.SetHighlight(Color.clear, false);
                lastHighlightedSlot = null;
            }

            // Clear any OrderDropHandler highlight
            var orderDropHandler = canvas.GetComponentInChildren<OrderDropHandler>();
            if (orderDropHandler != null)
            {
                orderDropHandler.SetHighlight(false);
            }

            // Re-enable raycast blocking when drag ends
            // This is critical for the card to receive click events after being dropped
            canvasGroup.blocksRaycasts = true;
            Debug.Log($"[DeployDragHandler] End drag - blocksRaycasts restored to true");

            // In Unity's event system, OnDrop is called before OnEndDrag
            // If the card was successfully deployed by a drop handler, it would no longer be in the player's hand
            // We can use this to determine if we need to reset the card position
            if (IsCardInHand(cardView.Card))
            {
                // Card is still in hand, meaning it wasn't deployed
                // Return it to its original position
                transform.SetParent(originalParent);
                transform.position = originalPosition;
                Debug.Log("[DeployDragHandler] Card drop was not successful, resetting position");
                OnDragEnded?.Invoke(false);
            }
            else
            {
                // Card is no longer in hand, meaning it was successfully deployed by a drop handler
                Debug.Log("[DeployDragHandler] Card was successfully deployed");
                OnDragEnded?.Invoke(true);
            }
        }

        // Helper method to check if a card is in the player's hand
        private bool IsCardInHand(Card card)
        {
            if (matchView == null || card == null)
                return false;

            var currentPlayer = matchView.GetCurrentPlayer();
            if (currentPlayer == null)
                return false;

            return currentPlayer.Hand.Cards.Any(c => c == card);
        }
    }
}
