using Kardx.UI.Components;
using Kardx.UI.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;

    public class CardSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField]
        private Image highlightImage;

        [SerializeField]
        private int position; // 0-4 for the five positions

        [SerializeField]
        [Tooltip(
            "Whether this slot can accept cards dropped by the player. Set to false for opponent slots."
        )]
        private bool isDroppable = true;

        private MatchView matchView;

        private void Awake()
        {
            matchView = GetComponentInParent<MatchView>();
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
            Debug.Log($"OnDrop called at position {position}");

            // Immediately return if this is not a droppable slot (e.g., opponent slot)
            if (!isDroppable)
            {
                Debug.LogWarning(
                    $"[CardSlot] Prevented drop on non-droppable slot at position {position}"
                );
                return;
            }

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("No CardView found on dropped object");
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
                    $"[CardSlot] Ensuring CanvasGroup.blocksRaycasts is true for dropped card"
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
                    Debug.Log($"Failed to deploy card at position {position}");
                    return;
                }
                Debug.Log($"Card deployed successfully at position {position}");
            }
            else if (isAbilityUse)
            {
                // Handle ability use
                Debug.Log($"Ability use detected from card {cardView.Card.Title} to position {position}");
                // TODO: Implement ability targeting logic here
                // This would call something like: matchView.UseAbility(cardView.Card, targetCard);
            }
            else
            {
                Debug.Log($"Card is neither in hand nor on battlefield, cannot determine action");
                return;
            }
        }

        // Helper method to check if a card is in the player's hand
        private bool IsCardInHand(Card card)
        {
            if (card == null || card.Owner == null)
                return false;

            var hand = card.Owner.Hand;
            return hand.Contains(card);
        }

        // Helper method to check if a card is on the battlefield
        private bool IsCardOnBattlefield(Card card)
        {
            if (card == null || card.Owner == null)
                return false;
                
            // Check if the card is in the battlefield
            var battlefield = card.Owner.Battlefield;
            return battlefield.Contains(card);
        }

        public bool IsValidDropTarget(Card card)
        {
            // First check if this slot is configured as droppable in the editor
            if (!isDroppable)
            {
                return false;
            }

            // If the slot is droppable, check with the match view if the card can be deployed
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

        // Set whether this slot can accept dropped cards
        public void SetDroppable(bool droppable)
        {
            this.isDroppable = droppable;
            Debug.Log($"[CardSlot] Slot {position} droppable set to: {droppable}");
        }

        // Check if this is a player slot (i.e., if it's droppable)
        public bool IsPlayerSlot()
        {
            return isDroppable;
        }
    }
}
