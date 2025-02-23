using Kardx.UI.Components.Card;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Scenes.Battle
{
    public class BattlefieldDropZone : MonoBehaviour, IDropHandler
    {
        [Header("Drop Zone Settings")]
        [SerializeField]
        private Vector2Int gridPosition;

        [SerializeField]
        private bool isOccupied;

        [Header("Visual Feedback")]
        [SerializeField]
        private GameObject highlightEffect;

        public Vector2Int GridPosition => gridPosition;
        public bool IsOccupied => isOccupied;

        // Event for notifying BattleView
        public event System.Action<CardView, Vector2Int> OnCardDropped;

        public void OnDrop(PointerEventData eventData)
        {
            var cardView = eventData.pointerDrag.GetComponent<CardView>();
            if (cardView != null && IsValidDropTarget())
            {
                HandleCardDrop(cardView);
            }
        }

        public bool IsValidDropTarget()
        {
            return !isOccupied;
        }

        private void HandleCardDrop(CardView cardView)
        {
            // Position the card in the grid
            cardView.transform.SetParent(transform);
            cardView.transform.localPosition = Vector3.zero;

            // Mark the zone as occupied
            SetOccupied(true);

            // Notify BattleView
            OnCardDropped?.Invoke(cardView, gridPosition);
        }

        public void SetHighlight(bool active)
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(active);
            }
        }

        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
            SetHighlight(!occupied); // Highlight available drop zones
        }

        public void ClearZone()
        {
            SetOccupied(false);
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
