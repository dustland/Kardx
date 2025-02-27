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

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("[PlayerCardSlot] No CardView found on dropped object");
                return;
            }

            // Clear highlight before handling the drop
            SetHighlight(false);

            // Ensure the card's CanvasGroup.blocksRaycasts is set to true
            // This is critical because OnEndDrag might not be called if the card is successfully deployed
            var canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                Debug.Log(
                    $"[PlayerCardSlot] Ensuring CanvasGroup.blocksRaycasts is true for dropped card"
                );
            }

            // Determine if this is a deployment or an ability use
            bool isDeployment = IsCardInHand(cardView.Card);
            bool isAbilityUse = IsCardOnBattlefield(cardView.Card);

            if (isDeployment)
            {
                // Handle card deployment
                if (matchView == null || !matchView.DeployCard(cardView.Card, position))
                {
                    Debug.Log($"[PlayerCardSlot] Failed to deploy card at position {position}");
                    return;
                }
                Debug.Log($"[PlayerCardSlot] Card deployed successfully at position {position}");
            }
            else if (isAbilityUse)
            {
                // Handle ability use
                Debug.Log(
                    $"[PlayerCardSlot] Ability use detected from card {cardView.Card.Title} to position {position}"
                );
                // TODO: Implement ability targeting logic here
                // This would call something like: matchView.UseAbility(cardView.Card, targetCard);
            }
            else
            {
                Debug.Log(
                    $"[PlayerCardSlot] Card is neither in hand nor on battlefield, cannot determine action"
                );
                return;
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

        public bool IsValidDropTarget(Card card)
        {
            // Check with the match view if the card can be deployed
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

        // Get the position of this slot
        public int GetPosition()
        {
            return position;
        }
    }
}
