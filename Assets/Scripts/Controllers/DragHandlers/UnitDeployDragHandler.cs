using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;
using Kardx.Views.Match;
using System.Linq;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Specialized drag handler for deploying unit cards.
    /// </summary>
    public class UnitDeployDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private float dragOffset = 15f;

        private Vector3 originalPosition;
        private Transform originalParent;
        private CardView cardView;
        private CanvasGroup canvasGroup;
        private PlayerBattlefieldView playerBattlefieldView;

        private void Awake()
        {
            // Cache component references
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Find the player battlefield view in the scene
            playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
        }

        private void OnEnable()
        {
            if (cardView == null)
            {
                cardView = GetComponent<CardView>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
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
            playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Ensure the card can be dragged
            if (!CanDrag())
                return;

            // Set the CardView.IsBeingDragged flag
            cardView.IsBeingDragged = true;

            // Store the original position and parent
            originalPosition = transform.position;
            originalParent = transform.parent;

            // Disable raycasting on this card while dragging
            canvasGroup.blocksRaycasts = false;

            // Place the card on the root to ensure proper layering
            transform.SetParent(transform.root);

            // Highlight valid drop targets
            if (playerBattlefieldView != null && cardView.Card != null)
            {
                Debug.Log("[UnitDeployDragHandler] Highlighting empty slots via PlayerBattlefieldView");
                playerBattlefieldView.HighlightEmptySlotsForCard(cardView.Card);
            }
            else
            {
                Debug.LogError("UnitDeployDragHandler: PlayerBattlefieldView not available for highlighting");
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
        /// The actual deployment logic happens in PlayerCardSlot.OnDrop.
        /// </summary>
        /// <param name="eventData">Data about the pointer event</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("[UnitDeployDragHandler] End Drag");

            if (!cardView.IsBeingDragged)
                return;

            // Reset the CardView.IsBeingDragged flag
            cardView.IsBeingDragged = false;

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
                    playerBattlefieldView.ClearCardHighlights();
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
