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

            // We disable blocksRaycasts during drag so that:
            // 1. The card itself doesn't block raycasts to potential drop targets underneath
            // 2. This allows the EventSystem to detect the drop zones (CardSlot components)
            // NOTE: This is the standard approach for drag and drop in Unity UI
            canvasGroup.blocksRaycasts = false;

            OnDragStarted?.Invoke();
            Debug.Log(
                $"[CardDragHandler] Started dragging card: {cardView.Card.Title}, blocksRaycasts set to false"
            );
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
                    // Check if this is a valid drop target
                    bool isValid = slot.IsValidDropTarget(cardView.Card);

                    // Only highlight if it's valid
                    if (isValid)
                    {
                        slot.SetHighlight(true);
                        lastHighlightedSlot = slot;
                        Debug.Log(
                            $"[CardDragHandler] Found valid drop target at slot {hit.gameObject.name}"
                        );
                        break; // Break after finding a valid slot
                    }
                    else
                    {
                        // Make sure invalid slots are not highlighted
                        slot.SetHighlight(false);
                        Debug.Log(
                            $"[CardDragHandler] Invalid drop target at slot {hit.gameObject.name}"
                        );
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

            // Re-enable raycast blocking when drag ends
            // This is critical for the card to receive click events after being dropped
            canvasGroup.blocksRaycasts = true;
            Debug.Log($"[CardDragHandler] End drag - blocksRaycasts restored to true");

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            bool wasSuccessful = false;
            CardSlot targetSlot = null;

            foreach (var hit in raycastResults)
            {
                var dropZone = hit.gameObject.GetComponent<CardSlot>();
                if (dropZone != null)
                {
                    // IsValidDropTarget already checks if the slot is droppable (isDroppable)
                    if (dropZone.IsValidDropTarget(cardView.Card))
                    {
                        wasSuccessful = true;
                        targetSlot = dropZone;

                        Debug.Log(
                            $"[CardDragHandler] Card dropped successfully on valid slot: {dropZone.name}"
                        );
                        break;
                    }
                    else
                    {
                        // This is not a valid drop target
                        Debug.Log(
                            $"[CardDragHandler] Card dropped on invalid slot: {dropZone.name}"
                        );
                    }
                }
            }

            if (!wasSuccessful)
            {
                // Only reset the position if the drop was not successful
                transform.SetParent(originalParent);
                transform.position = originalPosition;
                Debug.Log("[CardDragHandler] Card drop was not successful, resetting position");
            }

            // Double-check that blocksRaycasts is enabled
            Debug.Log(
                $"[CardDragHandler] End drag - blocksRaycasts set to: {canvasGroup.blocksRaycasts}"
            );

            OnDragEnded?.Invoke(wasSuccessful);
        }
    }
}
