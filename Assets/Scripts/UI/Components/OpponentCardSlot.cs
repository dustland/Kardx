using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;

    /// <summary>
    /// Specialized card slot for the opponent's battlefield.
    /// Handles targeting for player abilities and attacks.
    /// </summary>
    public class OpponentCardSlot : MonoBehaviour, IDropHandler
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

            // Set default properties for opponent highlight (different color)
            highlightImage.color = new Color(1f, 0f, 0f, 0.3f); // Red highlight for opponent slots
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
            Debug.Log($"[OpponentCardSlot] OnDrop called at position {position}");

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("[OpponentCardSlot] No CardView found on dropped object");
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
                    $"[OpponentCardSlot] Ensuring CanvasGroup.blocksRaycasts is true for dropped card"
                );
            }

            // For opponent slots, we only handle ability targeting or attacks
            // Get the target card at this position
            var targetCard = GetCardAtPosition();
            if (targetCard == null)
            {
                Debug.Log($"[OpponentCardSlot] No target card found at position {position}");
                return;
            }

            // Check if this is an ability use (from player's battlefield card)
            if (IsPlayerBattlefieldCard(cardView.Card))
            {
                Debug.Log(
                    $"[OpponentCardSlot] Ability targeting detected from {cardView.Card.Title} to {targetCard.Title}"
                );

                // Handle ability targeting
                // TODO: Implement ability targeting logic
                // matchView.UseAbility(cardView.Card, targetCard);

                // For now, we'll treat it as an attack
                if (matchView != null)
                {
                    matchView.AttackCard(cardView.Card, targetCard);
                    Debug.Log(
                        $"[OpponentCardSlot] Attack initiated from {cardView.Card.Title} to {targetCard.Title}"
                    );
                }
            }
            else
            {
                Debug.Log($"[OpponentCardSlot] Invalid drop: Card is not on player's battlefield");
            }
        }

        // Helper method to check if a card is on the player's battlefield
        private bool IsPlayerBattlefieldCard(Card card)
        {
            if (card == null || card.Owner == null || matchView == null)
                return false;

            // Check if this is the current player's card and it's on the battlefield
            var player = matchView.GetCurrentPlayer();
            if (card.Owner != player)
                return false;

            var battlefield = player.Battlefield;
            foreach (var battlefieldCard in battlefield)
            {
                if (battlefieldCard == card)
                    return true;
            }
            return false;
        }

        // Helper method to get the card at this position
        private Card GetCardAtPosition()
        {
            if (matchView == null)
                return null;

            var opponent = matchView.GetOpponent();
            if (opponent == null || position < 0 || position >= opponent.Battlefield.Count)
                return null;

            return opponent.Battlefield[position];
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

        // Get the position of this slot
        public int GetPosition()
        {
            return position;
        }
    }
}
