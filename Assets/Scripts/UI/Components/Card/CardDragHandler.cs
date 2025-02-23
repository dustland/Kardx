using System;
using Kardx.UI.Scenes.Battle;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Components.Card
{
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

        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragStartPosition = eventData.position;
            originalPosition = transform.position;
            // Don't set isDragging true yet - wait until we've moved enough
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Check if we've moved far enough to consider it a drag
            if (
                !isDragging
                && Vector2.Distance(dragStartPosition, eventData.position) > minDragDistance
            )
            {
                isDragging = true;
                OnDragStarted?.Invoke();
            }

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
            // If we never actually started dragging, treat it as a click
            if (!isDragging)
            {
                OnDragEnded?.Invoke(false);
                return;
            }

            bool wasSuccessful = false;

            // Check if dropped on valid target
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var hit in raycastResults)
            {
                var dropZone = hit.gameObject.GetComponent<BattlefieldDropZone>();
                if (dropZone != null && dropZone.IsValidDropTarget())
                {
                    wasSuccessful = true;
                    break;
                }
            }

            if (!wasSuccessful)
            {
                ReturnToOriginalPosition();
            }

            isDragging = false;
            OnDragEnded?.Invoke(wasSuccessful);
        }

        private void ReturnToOriginalPosition()
        {
            StartCoroutine(ReturnToPositionCoroutine());
        }

        private System.Collections.IEnumerator ReturnToPositionCoroutine()
        {
            float elapsedTime = 0;
            Vector3 startPosition = transform.position;

            while (elapsedTime < 1f)
            {
                elapsedTime += Time.deltaTime * returnSpeed;
                transform.position = Vector3.Lerp(startPosition, originalPosition, elapsedTime);
                yield return null;
            }

            transform.position = originalPosition;
        }
    }
}
