using System;
using Kardx.UI.Scenes.Battle;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Components.Card
{
    using Card = Kardx.Core.Data.Cards.Card;

    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Drag Settings")]
        [SerializeField]
        private float dragSpeed = 1f;

        [SerializeField]
        private float returnSpeed = 10f;

        [SerializeField]
        private float minDragDistance = 10f; // Minimum distance to consider it a drag

        private Vector3 originalPosition;
        private bool isDragging;
        private Vector2 dragStartPosition;
        private RectTransform rectTransform;
        private Canvas canvas;
        private CardView cardView;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private int originalSiblingIndex; // Store the original sibling index

        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Find the drag parent (should be a child of Canvas for proper sorting)
            // Removed dragParent as it's no longer needed
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDragCard())
            {
                Debug.Log("Cannot drag card");
                return;
            }

            isDragging = true;
            originalPosition = transform.position;
            originalParent = transform.parent;

            // Store the original sibling index so we can restore it if drag fails
            originalSiblingIndex = transform.GetSiblingIndex();

            // Set as last sibling in the canvas to ensure it renders on top
            transform.SetParent(canvas.transform);
            transform.SetAsLastSibling();

            canvasGroup.blocksRaycasts = false;
            OnDragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out position
            );

            transform.position = Vector3.Lerp(
                transform.position,
                canvas.transform.TransformPoint(position),
                Time.deltaTime * dragSpeed
            );
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            canvasGroup.blocksRaycasts = true;
            bool wasSuccessful = false;

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var hit in raycastResults)
            {
                var dropZone = hit.gameObject.GetComponent<BattlefieldDropZone>();
                if (dropZone != null && dropZone.IsValidDropTarget(cardView.Card))
                {
                    wasSuccessful = true;
                    // Let the dropzone handle the parenting
                    break;
                }
            }

            if (!wasSuccessful)
            {
                // Return to original position and parent
                transform.SetParent(originalParent);
                transform.SetSiblingIndex(originalSiblingIndex);
                transform.position = originalPosition;
            }

            isDragging = false;
            OnDragEnded?.Invoke(wasSuccessful);
        }

        private bool CanDragCard()
        {
            if (cardView == null)
            {
                Debug.Log("CanDragCard: Card view is null");
                return false;
            }

            // Get BattleView reference
            var battleView = canvas.GetComponentInParent<BattleView>();
            if (battleView == null)
            {
                Debug.Log("CanDragCard: Battle view is null");
                return false;
            }

            // Only allow dragging cards from player's hand that can be deployed
            return battleView.CanDeployCard(cardView.Card);
        }

        private bool IsValidDropTarget(BattlefieldDropZone dropZone)
        {
            if (cardView == null || dropZone == null)
                return false;

            return dropZone.IsValidDropTarget(cardView.Card);
        }

        // Removed ReturnToOriginalPosition as it's no longer needed
    }
}
