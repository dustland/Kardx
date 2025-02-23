using Kardx.UI.Components.Card;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Scenes.Battle
{
    using Card = Kardx.Core.Data.Cards.Card;

    public class BattlefieldDropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;
        private BattleView battleView;

        private void Awake()
        {
            battleView = GetComponentInParent<BattleView>();
            if (!highlightImage)
                CreateHighlight();

            // Ensure this GameObject can receive drops
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = Color.clear; // Make it invisible
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
            highlightImage.raycastTarget = false; // Don't block drops
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("OnDrop called");
            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("No CardView found on dropped object");
                return;
            }

            if (battleView == null || !battleView.DeployCard(cardView.Card))
            {
                Debug.Log("Failed to deploy card");
                return;
            }

            Debug.Log("Card deployed successfully");
            cardView.transform.SetParent(transform);
            cardView.transform.localPosition = Vector3.zero;
            cardView.transform.localScale = Vector3.one;
        }

        public bool IsValidDropTarget(Card card)
        {
            return battleView?.CanDeployCard(card) ?? false;
        }

        public void SetHighlight(bool show)
        {
            if (highlightImage)
                highlightImage.enabled = show;
        }
    }
}
