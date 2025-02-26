using System;
using Kardx.UI.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;

    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Vector3 originalPosition;
        private Transform originalParent;
        private Canvas canvas;
        private CardView cardView;
        private CanvasGroup canvasGroup;
        private CardSlot lastHighlightedSlot; // Track the last highlighted slot

        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        private void Awake()
        {
            // Find Canvas by searching up through all parents
            canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                Debug.LogError(
                    "[CardDragHandler] No Canvas found in parents. Card dragging won't work."
                );
            }

            // Find MatchView at the root Canvas level
            var matchView = canvas?.GetComponent<MatchView>();
            if (matchView == null)
            {
                Debug.LogError(
                    "[CardDragHandler] No MatchView found on Canvas. Card dragging won't work."
                );
            }

            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Only check if we have the CardView component
            if (cardView == null)
            {
                Debug.LogError("[CardDragHandler] CardView component is missing");
            }

            // Ensure we have a CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            // Make sure it blocks raycasts by default
            canvasGroup.blocksRaycasts = true;

            Debug.Log(
                $"[CardDragHandler] Initialized - Canvas: {canvas?.name ?? "null"}, MatchView: {matchView?.name ?? "null"}"
            );
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("[CardDragHandler] OnBeginDrag");

            if (canvas == null)
            {
                Debug.LogError("[CardDragHandler] Cannot drag: No Canvas reference");
                return;
            }

            var matchView = canvas.GetComponent<MatchView>();
            if (matchView == null)
            {
                Debug.LogError("[CardDragHandler] Cannot drag: No MatchView found on Canvas");
                return;
            }

            // Check card status when we actually need to drag
            if (cardView == null)
            {
                Debug.LogError("[CardDragHandler] Cannot drag: CardView component is missing");
                return;
            }

            if (cardView.Card == null)
            {
                Debug.LogError("[CardDragHandler] Cannot drag: No card data available");
                return;
            }

            if (!matchView.CanDeployCard(cardView.Card))
            {
                Debug.Log("[CardDragHandler] Cannot drag: cannot deploy card");
                return;
            }

            originalPosition = transform.position;
            originalParent = transform.parent;
            transform.SetParent(canvas.transform);
            canvasGroup.blocksRaycasts = false; // Disable raycast blocking during drag
            OnDragStarted?.Invoke();
            Debug.Log($"[CardDragHandler] Started dragging card: {cardView.Card.Title}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (originalParent == null)
                return;

            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out pos
            );
            transform.position = canvas.transform.TransformPoint(pos);

            // Clear previous highlight if we had one
            if (lastHighlightedSlot != null)
            {
                lastHighlightedSlot.SetHighlight(false);
                lastHighlightedSlot = null;
            }

            // Highlight potential drop zones
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            // Check all hits for a valid card slot
            foreach (var hit in raycastResults)
            {
                var slot = hit.gameObject.GetComponent<CardSlot>();
                if (slot != null)
                {
                    bool isValid = slot.IsValidDropTarget(cardView.Card);
                    slot.SetHighlight(isValid);
                    if (isValid)
                    {
                        lastHighlightedSlot = slot;
                        break; // Break only after finding a valid slot
                    }
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (originalParent == null)
                return;

            // Clear any remaining highlight
            if (lastHighlightedSlot != null)
            {
                lastHighlightedSlot.SetHighlight(false);
                lastHighlightedSlot = null;
            }

            canvasGroup.blocksRaycasts = true;

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            bool wasSuccessful = false;
            CardSlot targetSlot = null;

            foreach (var hit in raycastResults)
            {
                var dropZone = hit.gameObject.GetComponent<CardSlot>();
                if (dropZone != null && dropZone.IsValidDropTarget(cardView.Card))
                {
                    wasSuccessful = true;
                    targetSlot = dropZone;
                    // When a card is successfully dropped, we need to hide it from the hand
                    // This prevents it from appearing in both the hand and the battlefield
                    gameObject.SetActive(false);
                    Debug.Log(
                        "[CardDragHandler] Card dropped successfully on valid target - hiding from hand"
                    );
                    break;
                }
            }

            if (!wasSuccessful)
            {
                // Only reset the position if the drop was not successful
                transform.SetParent(originalParent);
                transform.position = originalPosition;
                Debug.Log("[CardDragHandler] Card drop was not successful, resetting position");
            }
            // Note: We don't need to do anything for successful drops
            // The DeployCard method will handle moving the card to the battlefield
            // and the UpdateUI method will handle removing it from the hand

            OnDragEnded?.Invoke(wasSuccessful);
        }
    }
}
