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

        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Ensure we have a CanvasGroup
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            // Make sure it blocks raycasts by default
            canvasGroup.blocksRaycasts = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("OnBeginDrag");
            if (cardView == null || cardView.Card == null)
            {
                Debug.Log("Cannot drag: card or cardView is null");
                return;
            }

            var matchView = canvas.GetComponentInParent<MatchView>();
            if (matchView == null || !matchView.CanDeployCard(cardView.Card))
            {
                Debug.Log("Cannot drag: invalid match view or cannot deploy card");
                return;
            }

            originalPosition = transform.position;
            originalParent = transform.parent;
            transform.SetParent(canvas.transform);
            canvasGroup.blocksRaycasts = false; // Disable raycast blocking during drag
            OnDragStarted?.Invoke();
            Debug.Log("OnBeginDrag: cardView.Card.Title = " + cardView.Card.Title);
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

            // Highlight potential drop zones
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            Debug.Log("OnDrag: raycastResults.Count = " + raycastResults.Count);
            foreach (var hit in raycastResults)
            {
                var slot = hit.gameObject.GetComponent<CardSlot>();
                if (slot != null)
                {
                    slot.SetHighlight(slot.IsValidDropTarget(cardView.Card));
                    break;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (originalParent == null)
                return;

            canvasGroup.blocksRaycasts = true;

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            bool wasSuccessful = false;

            foreach (var hit in raycastResults)
            {
                var dropZone = hit.gameObject.GetComponent<CardSlot>();
                if (dropZone != null && dropZone.IsValidDropTarget(cardView.Card))
                {
                    wasSuccessful = true;
                    break;
                }
            }

            if (!wasSuccessful)
            {
                transform.SetParent(originalParent);
                transform.position = originalPosition;
            }

            OnDragEnded?.Invoke(wasSuccessful);
        }
    }
}
