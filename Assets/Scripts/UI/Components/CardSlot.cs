using Kardx.UI.Components;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kardx.UI.Scenes;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;

    public class CardSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;
        [SerializeField]
        private int position; // 0-4 for the five positions

        private MatchView matchView;

        private void Awake()
        {
            matchView = GetComponentInParent<MatchView>();
            if (!highlightImage)
                CreateHighlight();

            // Ensure this GameObject can receive drops
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
            }
        }

        private void CreateHighlight()
        {
            var go = new GameObject("Highlight");
            highlightImage = go.AddComponent<Image>();
            highlightImage.transform.SetParent(transform);

            var rt = highlightImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;

            highlightImage.color = new Color(1f, 1f, 0f, 0.3f);
            highlightImage.enabled = false;
            highlightImage.raycastTarget = false;
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"OnDrop called at position {position}");
            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("No CardView found on dropped object");
                return;
            }

            if (matchView == null || !matchView.DeployCard(cardView.Card, position))
            {
                Debug.Log($"Failed to deploy card at position {position}");
                return;
            }

            Debug.Log($"Card deployed successfully at position {position}");
            cardView.transform.SetParent(transform);
            cardView.transform.localPosition = Vector3.zero;
            cardView.transform.localScale = Vector3.one;
        }

        public bool IsValidDropTarget(Card card)
        {
            return matchView?.CanDeployCard(card) ?? false;
        }

        public void SetHighlight(bool show)
        {
            if (highlightImage)
                highlightImage.enabled = show;
        }

        // For editor setup
        public void SetPosition(int position)
        {
            this.position = Mathf.Clamp(position, 0, 4);
        }
    }
}
