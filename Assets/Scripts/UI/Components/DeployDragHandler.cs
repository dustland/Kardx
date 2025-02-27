using System;
using Kardx.UI.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;

    public class DeployDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Vector3 originalPosition;
        private Transform originalParent;
        private Canvas canvas;
        private CardView cardView;
        private CanvasGroup canvasGroup;
        private PlayerCardSlot lastHighlightedSlot; // Track the last highlighted slot

        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

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
            var matchView = canvas?.GetComponent<MatchView>();
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

            var matchView = canvas.GetComponent<MatchView>();
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

            if (!matchView.CanDeployCard(cardView.Card))
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
            Debug.Log($"[DeployDragHandler] Raycast results: {raycastResults.Count} hits");

            // Check all hits for a valid card slot
            foreach (var hit in raycastResults)
            {
                var slot = hit.gameObject.GetComponent<PlayerCardSlot>();
                if (slot != null)
                {
                    // Check if this is a valid drop target
                    bool isValid = slot.IsValidDropTarget(cardView.Card);
                    Debug.Log(
                        $"[DeployDragHandler] IsValidDropTarget: {isValid} for card {cardView.Card.Title} at slot {hit.gameObject.name}"
                    );

                    // Only highlight if it's valid
                    if (isValid)
                    {
                        slot.SetHighlight(true);
                        lastHighlightedSlot = slot;
                        Debug.Log(
                            $"[DeployDragHandler] Found valid drop target at slot {hit.gameObject.name}"
                        );
                        break; // Break after finding a valid slot
                    }
                    else
                    {
                        // Make sure invalid slots are not highlighted
                        slot.SetHighlight(false);
                        Debug.Log(
                            $"[DeployDragHandler] Invalid drop target at slot {hit.gameObject.name}"
                        );
                    }
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
                lastHighlightedSlot.SetHighlight(false);
                lastHighlightedSlot = null;
            }

            // Re-enable raycast blocking when drag ends
            // This is critical for the card to receive click events after being dropped
            canvasGroup.blocksRaycasts = true;
            Debug.Log($"[DeployDragHandler] End drag - blocksRaycasts restored to true");

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            bool wasSuccessful = false;
            PlayerCardSlot targetSlot = null;

            foreach (var hit in raycastResults)
            {
                var dropZone = hit.gameObject.GetComponent<PlayerCardSlot>();
                if (dropZone != null)
                {
                    // Check if the card can be deployed to this slot
                    if (dropZone.IsValidDropTarget(cardView.Card))
                    {
                        wasSuccessful = true;
                        targetSlot = dropZone;

                        Debug.Log(
                            $"[DeployDragHandler] Card dropped successfully on valid slot: {dropZone.name}"
                        );
                        break;
                    }
                    else
                    {
                        // This is not a valid drop target
                        Debug.Log(
                            $"[DeployDragHandler] Card dropped on invalid slot: {dropZone.name}"
                        );
                    }
                }
            }

            if (!wasSuccessful)
            {
                // Only reset the position if the drop was not successful
                transform.SetParent(originalParent);
                transform.position = originalPosition;
                Debug.Log("[DeployDragHandler] Card drop was not successful, resetting position");
            }

            // Double-check that blocksRaycasts is enabled
            Debug.Log(
                $"[DeployDragHandler] End drag - blocksRaycasts set to: {canvasGroup.blocksRaycasts}"
            );

            OnDragEnded?.Invoke(wasSuccessful);
        }
    }
}
