using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Kardx.Core;
using System.Linq;

namespace Kardx.UI.Components
{
    /// <summary>
    /// Specialized drag handler for deploying unit cards.
    /// </summary>
    public class UnitDeployDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private float dragOffset = 0.5f;

        private CardView cardView;
        private PlayerBattlefieldView playerBattlefieldView;
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

            InitializeBattlefieldView();
        }

        private void InitializeBattlefieldView()
        {
            // Get the PlayerBattlefieldView through the containing hierarchy
            playerBattlefieldView = GetComponentInParent<PlayerBattlefieldView>();

            if (playerBattlefieldView == null)
            {
                // If we can't find it directly, try finding it in the scene
                playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
                if (playerBattlefieldView == null)
                {
                    Debug.LogError("UnitDeployDragHandler: Cannot find PlayerBattlefieldView in hierarchy or scene");
                }
                else
                {
                    Debug.Log("[UnitDeployDragHandler] Found PlayerBattlefieldView in scene directly");
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

            // Place the card on the root to ensure proper layering
            transform.SetParent(transform.root);

            // Highlight valid drop targets
            if (playerBattlefieldView != null && cardView.Card != null)
            {
                Debug.Log("[UnitDeployDragHandler] Highlighting empty slots via PlayerBattlefieldView");
                playerBattlefieldView.HighlightEmptySlots(cardView.Card.Owner.Battlefield);
            }
            else
            {
                Debug.LogError("UnitDeployDragHandler: PlayerBattlefieldView not available for highlighting");
            }
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
        /// The actual deployment logic happens in PlayerCardSlot.OnDrop.
        /// </summary>
        /// <param name="eventData">Data about the pointer event</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("[UnitDeployDragHandler] End Drag");

            if (!isDragging)
                return;

            isDragging = false;
            
            // Re-enable raycast blocking
            canvasGroup.blocksRaycasts = true;

            // The deployment logic is handled by the drop target (PlayerCardSlot.OnDrop)
            // This drag handler only needs to return the card if it wasn't dropped on a valid target

            // Check if it was dropped on a valid target
            bool droppedOnTarget = (eventData.pointerEnter != null &&
                                   eventData.pointerEnter.GetComponent<PlayerCardSlot>() != null);

            if (!droppedOnTarget)
            {
                // Return the card to its original position if not dropped on a valid target
                ReturnToOriginalPosition();

                // Clear any highlights
                if (playerBattlefieldView != null)
                {
                    playerBattlefieldView.ClearHighlights();
                }
            }

            // Note: We don't destroy the card here.
            // If the drop was valid, the drop handler (PlayerCardSlot.OnDrop) will handle the deployment logic
            // and either destroy this object or reposition it.
        }

        private bool CanDrag()
        {
            if (cardView == null || cardView.Card == null)
                return false;

            // Only unit cards can be dragged
            return cardView.Card.IsUnitCard;
        }

        private void ReturnToOriginalPosition()
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
        }

        private List<PlayerCardSlot> GetCardSlotsUnderPointer(Vector2 position)
        {
            List<PlayerCardSlot> results = new List<PlayerCardSlot>();

            // Cast rays to find all objects under the pointer
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = position
            };

            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            // Filter results to only include PlayerCardSlot components
            foreach (var result in raycastResults)
            {
                PlayerCardSlot slot = result.gameObject.GetComponent<PlayerCardSlot>();
                if (slot != null)
                {
                    results.Add(slot);
                }
            }

            return results;
        }
    }
}
