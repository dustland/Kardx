using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components
{
    using Kardx.Core;

    /// <summary>
    /// Specialized card slot for the opponent's battlefield.
    /// Handles targeting for player abilities and attacks.
    /// </summary>
    public class OpponentCardSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;

        [SerializeField]
        private Transform cardContainer;

        private int slotIndex;
        private OpponentBattlefieldView battlefieldView;
        private CardView currentCardView;

        private void Awake()
        {
            if (highlightImage == null)
                CreateHighlight();
            else
                ConfigureHighlightImage();

            // Ensure this GameObject can receive drops
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
            }

            if (cardContainer == null)
            {
                cardContainer = transform;
            }
        }

        public void Initialize(int slotIndex, OpponentBattlefieldView battlefieldView)
        {
            this.slotIndex = slotIndex;
            this.battlefieldView = battlefieldView;
        }

        private void ConfigureHighlightImage()
        {
            // Ensure the highlight image has proper RectTransform settings
            var rt = highlightImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.localPosition = Vector3.zero;
            rt.localScale = Vector3.one;

            // Make sure highlight fills the entire slot
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Set default properties for opponent highlight
            highlightImage.color = new Color(1f, 0f, 0f, 0.3f); // Red highlight for opponent slots
            highlightImage.raycastTarget = false;
            highlightImage.enabled = false; // Start with highlight disabled
        }

        private void CreateHighlight()
        {
            var go = new GameObject("Highlight");
            highlightImage = go.AddComponent<Image>();
            highlightImage.transform.SetParent(transform, false);
            ConfigureHighlightImage();
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"[OpponentCardSlot] OnDrop called at position {slotIndex}");

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("[OpponentCardSlot] No CardView found on dropped object");
                return;
            }

            // Clear highlight
            ClearHighlight();

            // Ensure the card's CanvasGroup.blocksRaycasts is set to true
            var canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            // Handle the ability targeting logic via MatchView
            var matchView = battlefieldView.GetMatchView();
            if (matchView != null)
            {
                matchView.OnOpponentCardSlotTargeted(cardView.Card, slotIndex);
            }
            
            // Clear all highlights
            matchView.ClearAllHighlights();
        }

        public void UpdateCardDisplay(Card card)
        {
            // Clear existing card view
            if (currentCardView != null)
            {
                Destroy(currentCardView.gameObject);
                currentCardView = null;
            }

            if (card != null)
            {
                // Create new card view
                var matchView = battlefieldView.GetMatchView();
                var cardViewGO = matchView.CreateCardUI(card, cardContainer);
                currentCardView = cardViewGO.GetComponent<CardView>();
                
                // Configure card view for opponent battlefield
                currentCardView.SetInteractable(false);
            }
        }

        public void SetHighlight(Color color, bool show)
        {
            if (highlightImage != null)
            {
                highlightImage.color = color;
                highlightImage.enabled = show;
            }
        }

        public void ClearHighlight()
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }
        }

        public int GetSlotIndex()
        {
            return slotIndex;
        }
    }
}
