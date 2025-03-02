using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;
    using CardCategory = Kardx.Core.CardCategory;

    /// <summary>
    /// Handles the dropping of Order cards onto the battlefield.
    /// This component should be attached to a GameObject that covers the entire battlefield area.
    /// </summary>
    public class OrderDropHandler : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;

        private Kardx.UI.Scenes.MatchView matchView;

        private void Awake()
        {
            matchView = GetComponentInParent<Kardx.UI.Scenes.MatchView>();
            if (!highlightImage)
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

            // Make sure highlight fills the entire area
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Set default properties
            highlightImage.color = new Color(0f, 0.5f, 1f, 0.2f); // Blue tint for Order cards
            highlightImage.raycastTarget = false;
            highlightImage.enabled = false; // Start with highlight disabled
        }

        private void CreateHighlight()
        {
            var go = new GameObject("OrderHighlight");
            highlightImage = go.AddComponent<Image>();
            highlightImage.transform.SetParent(transform, false);
            ConfigureHighlightImage();
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("[OrderDropHandler] OnDrop called");

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("[OrderDropHandler] No CardView found on dropped object");
                return;
            }

            // Clear highlight before handling the drop
            SetHighlight(false);

            // Ensure the card's CanvasGroup.blocksRaycasts is set to true
            var canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                Debug.Log(
                    "[OrderDropHandler] Ensuring CanvasGroup.blocksRaycasts is true for dropped card"
                );
            }

            // Check if this is an Order card
            if (cardView.Card.CardType.Category != CardCategory.Order)
            {
                Debug.Log($"[OrderDropHandler] Card {cardView.Card.Title} is not an Order card");
                return;
            }

            // Check if the card is in the player's hand
            if (!IsCardInHand(cardView.Card))
            {
                Debug.Log($"[OrderDropHandler] Card {cardView.Card.Title} is not in player's hand");
                return;
            }

            // Deploy the Order card
            if (matchView == null || !matchView.DeployOrderCard(cardView.Card))
            {
                Debug.Log($"[OrderDropHandler] Failed to deploy Order card {cardView.Card.Title}");
            }
            else
            {
                Debug.Log(
                    $"[OrderDropHandler] Order card {cardView.Card.Title} deployed successfully"
                );
            }
        }

        // Helper method to check if a card is in the player's hand
        private bool IsCardInHand(Card card)
        {
            if (matchView == null || card == null)
                return false;

            var currentPlayer = matchView.GetCurrentPlayer();
            if (currentPlayer == null)
                return false;

            return currentPlayer.Hand.Cards.Any(c => c == card);
        }

        /// <summary>
        /// Checks if the card can be deployed as an Order card.
        /// </summary>
        public bool IsValidDropTarget(Card card)
        {
            if (matchView == null || card == null)
            {
                Debug.Log("[OrderDropHandler] Invalid drop target: matchView or card is null");
                return false;
            }

            // Only Order cards can be deployed here
            if (card.CardType.Category != CardCategory.Order)
            {
                Debug.Log(
                    $"[OrderDropHandler] Invalid drop target: {card.Title} is not an Order card"
                );
                return false;
            }

            // Check if the card is in the player's hand
            if (!IsCardInHand(card))
            {
                Debug.Log(
                    $"[OrderDropHandler] Invalid drop target: {card.Title} is not in player's hand"
                );
                return false;
            }

            // Check if the card can be deployed according to game rules
            if (!matchView.CanDeployOrderCard(card))
            {
                Debug.Log(
                    $"[OrderDropHandler] Invalid drop target: {card.Title} cannot be deployed (game rules)"
                );
                return false;
            }

            return true;
        }

        public void SetHighlight(bool show)
        {
            if (highlightImage != null)
                highlightImage.enabled = show;
        }
    }
}
