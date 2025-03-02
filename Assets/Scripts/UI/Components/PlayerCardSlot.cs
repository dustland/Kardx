using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;

    /// <summary>
    /// Specialized card slot for the player's battlefield.
    /// Handles card deployment and ability targeting.
    /// </summary>
    public class PlayerCardSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;

        [SerializeField]
        private int position; // 0-4 for the five positions

        private Kardx.UI.Scenes.MatchView matchView;

        private void Awake()
        {
            matchView = GetComponentInParent<Kardx.UI.Scenes.MatchView>();
            if (!highlightImage)
                CreateHighlight();
            else
                ConfigureHighlightImage(); // Configure the prefab-assigned highlight image

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

            // Make sure highlight fills the entire slot
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Set default properties
            highlightImage.color = new Color(1f, 1f, 0f, 0.3f);
            highlightImage.raycastTarget = false;
            highlightImage.enabled = false; // Start with highlight disabled
        }

        private void CreateHighlight()
        {
            var go = new GameObject("Highlight");
            highlightImage = go.AddComponent<Image>();
            highlightImage.transform.SetParent(transform, false); // Set worldPositionStays to false
            ConfigureHighlightImage();
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"[PlayerCardSlot] OnDrop called at position {position}");

            // Get the card view from the dragged object
            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("[PlayerCardSlot] No CardView found on dropped object");
                return;
            }

            // Ensure the card's CanvasGroup.blocksRaycasts is set to true
            var canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                Debug.Log(
                    "[PlayerCardSlot] Ensuring CanvasGroup.blocksRaycasts is true for dropped card"
                );
            }

            // Check if this is a valid drop target for the card
            if (!IsValidDropTarget(cardView.Card))
            {
                Debug.Log($"[PlayerCardSlot] Invalid drop target for card {cardView.Card.Title}");
                return;
            }

            // Deploy the card to this slot
            if (matchView == null || !matchView.DeployUnitCard(cardView.Card, position))
            {
                Debug.Log(
                    $"[PlayerCardSlot] Failed to deploy card {cardView.Card.Title} at position {position}"
                );
            }
            else
            {
                Debug.Log($"[PlayerCardSlot] Card deployed successfully at position {position}");
            }
        }

        // Helper method to check if a card is in the player's hand
        private bool IsCardInHand(Card card)
        {
            if (card == null || card.Owner == null)
                return false;

            var handCards = card.Owner.Hand;
            foreach (var handCard in handCards)
            {
                if (handCard == card)
                    return true;
            }
            return false;
        }

        // Helper method to check if a card is on the battlefield
        private bool IsCardOnBattlefield(Card card)
        {
            if (card == null || card.Owner == null)
                return false;

            // Check if the card is in the battlefield
            var battlefield = card.Owner.Battlefield;
            foreach (var battlefieldCard in battlefield)
            {
                if (battlefieldCard == card)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the card can be deployed to this slot.
        /// </summary>
        public bool IsValidDropTarget(Card card)
        {
            if (matchView == null || card == null)
            {
                Debug.Log("[PlayerCardSlot] Invalid drop target: matchView or card is null");
                return false;
            }

            // Only Unit cards can be deployed to battlefield slots
            if (card.CardType.Category != Kardx.Core.CardCategory.Unit)
            {
                Debug.Log($"[PlayerCardSlot] Invalid drop target: {card.Title} is not a Unit card");
                return false;
            }

            // Check if the card is in the player's hand
            if (!IsCardInHand(card))
            {
                Debug.Log(
                    $"[PlayerCardSlot] Invalid drop target: {card.Title} is not in player's hand"
                );
                return false;
            }

            // Check if the card can be deployed according to game rules
            if (!matchView.CanDeployUnitCard(card))
            {
                Debug.Log(
                    $"[PlayerCardSlot] Invalid drop target: {card.Title} cannot be deployed (game rules)"
                );
                return false;
            }

            // Check if the slot is empty
            var currentPlayer = matchView.GetCurrentPlayer();
            if (currentPlayer == null)
            {
                Debug.Log("[PlayerCardSlot] Invalid drop target: current player is null");
                return false;
            }

            // Ensure position is within bounds
            if (position < 0 || position >= Kardx.Core.Player.BATTLEFIELD_SLOT_COUNT)
            {
                Debug.Log(
                    $"[PlayerCardSlot] Invalid drop target: position {position} is out of bounds"
                );
                return false;
            }

            // Check if the slot is already occupied
            bool isEmpty = currentPlayer.Battlefield[position] == null;
            if (!isEmpty)
            {
                Debug.Log(
                    $"[PlayerCardSlot] Invalid drop target: position {position} is already occupied"
                );
            }
            return isEmpty;
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

        /// <summary>
        /// Gets the position of this slot.
        /// </summary>
        /// <returns>The position index of this slot.</returns>
        public int GetPosition()
        {
            return position;
        }
    }
}
