using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI
{
    using Card = Kardx.Core.Card;
    using CardCategory = Kardx.Core.CardCategory;
    using Kardx.Core;

    /// <summary>
    /// Handles the dropping of Order cards onto the battlefield.
    /// This component should be attached to a GameObject that covers the entire battlefield area.
    /// </summary>
    public class OrderDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Image highlightImage;

        private MatchView matchView;

        private void Awake()
        {
            // Just store the matchView reference
            matchView = GetComponentInParent<MatchView>();

            if (matchView == null)
            {
                Debug.LogWarning("[OrderDropHandler] Could not find MatchView.");
            }

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
            highlightImage.raycastTarget = false;
            SetHighlight(false);
        }

        private void CreateHighlight()
        {
            // Create a child object for highlighting
            var highlightObj = new GameObject("HighlightImage");
            highlightObj.transform.SetParent(transform, false);
            highlightObj.transform.localPosition = Vector3.zero;
            highlightObj.transform.localScale = Vector3.one;

            // Make it fill the parent
            var rectTransform = highlightObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Add the image component
            highlightImage = highlightObj.AddComponent<Image>();
            highlightImage.raycastTarget = false;
            highlightImage.color = new Color(1f, 1f, 0f, 0.3f); // Semi-transparent yellow
            SetHighlight(false);
        }

        public void SetHighlight(bool show)
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = show;
            }
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

            // Make sure Card and CardType are not null
            if (cardView.Card == null)
            {
                Debug.LogError("[OrderDropHandler] CardView has null Card reference");
                return;
            }

            if (cardView.Card.CardType == null)
            {
                Debug.LogError($"[OrderDropHandler] Card {cardView.Card.Title} has null CardType");
                return;
            }

            // Check if this is an Order card
            if (cardView.Card.CardType.Category != CardCategory.Order)
            {
                Debug.Log($"[OrderDropHandler] Card {cardView.Card.Title} is not an Order card");
                return;
            }

            // Deploy the Order card using MatchManager directly
            if (matchView == null)
            {
                Debug.LogError("[OrderDropHandler] Failed to deploy Order card - MatchView not available");
                return;
            }

            var matchManager = matchView.MatchManager;
            if (matchManager == null)
            {
                Debug.LogError("[OrderDropHandler] Failed to deploy Order card - MatchManager not available");
                return;
            }

            if (!matchManager.DeployCard(cardView.Card, -1))
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            var draggingCard = eventData.pointerDrag?.GetComponent<CardView>()?.Card;
            if (draggingCard != null && IsValidDropTarget(draggingCard))
            {
                SetHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        /// <summary>
        /// Checks if the card can be deployed as an Order card.
        /// </summary>
        public bool IsValidDropTarget(Card card)
        {
            if (card == null)
            {
                Debug.Log("[OrderDropHandler] Invalid drop target: Card is null");
                return false;
            }

            // Get MatchManager from matchView
            if (matchView == null)
            {
                Debug.Log("[OrderDropHandler] Invalid drop target: MatchView not available");
                return false;
            }

            var matchManager = matchView.MatchManager;
            if (matchManager == null)
            {
                Debug.Log("[OrderDropHandler] Invalid drop target: MatchManager not available");
                return false;
            }

            // Make sure CardType is not null
            if (card.CardType == null)
            {
                Debug.LogError($"[OrderDropHandler] Card {card.Title} has null CardType");
                return false;
            }

            // Check if the card is an Order card
            if (card.CardType.Category != CardCategory.Order)
            {
                Debug.Log($"[OrderDropHandler] Invalid drop target: {card.Title} is not an Order card");
                return false;
            }

            // Check if the card can be deployed according to game rules
            if (!matchManager.CanDeployOrderCard(card))
            {
                Debug.Log(
                    $"[OrderDropHandler] Invalid drop target: {card.Title} cannot be deployed due to game rules"
                );
                return false;
            }
            return true;
        }
    }
}
