using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Kardx.Views.Cards;

namespace Kardx.Views.Match
{
    /// <summary>
    /// UI component representing a slot in the opponent's battlefield.
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
        private bool isHighlighted = false;

        public int SlotIndex => slotIndex;
        public Transform CardContainer => cardContainer ?? transform;

        public void Initialize(int slotIndex, OpponentBattlefieldView battlefieldView)
        {
            this.slotIndex = slotIndex;
            this.battlefieldView = battlefieldView;

            // Initialize highlight
            if (highlightImage == null)
            {
                CreateHighlightImage();
            }

            if (highlightImage != null)
            {
                highlightImage.enabled = false;
                Debug.Log($"[OpponentCardSlot] Initialized slot {slotIndex} with highlight image: {highlightImage.name}");
            }
            else
            {
                Debug.LogWarning($"[OpponentCardSlot] No highlight image assigned to slot {slotIndex}");
            }

            // Initialize card container if not set
            if (cardContainer == null)
            {
                cardContainer = transform;
            }
        }

        private void CreateHighlightImage()
        {
            // Create a new GameObject for the highlight
            GameObject highlightObj = new GameObject("HighlightImage");
            highlightObj.transform.SetParent(transform, false);
            highlightObj.transform.SetAsFirstSibling(); // Ensure it's the first child so it appears behind other elements

            // Make it fill the parent
            RectTransform rectTransform = highlightObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Add an Image component
            Image image = highlightObj.AddComponent<Image>();
            image.color = new Color(1f, 0.2f, 0f, 0.9f); // Bright orange-red with high alpha

            // Create a white texture for the highlight
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            // Create a sprite from the texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            image.sprite = sprite;

            // Set it to be behind other elements
            image.raycastTarget = false;

            // Initially disabled
            image.enabled = false;

            // Assign it to the highlightImage field
            highlightImage = image;

            Debug.Log($"[OpponentCardSlot] Created highlight image for slot {slotIndex}");
        }

        public void SetHighlight(Color color, bool active = true)
        {
            if (highlightImage != null)
            {
                highlightImage.color = color;
                highlightImage.enabled = active;
                isHighlighted = active;
            }
        }

        public void ClearHighlight()
        {
            isHighlighted = false;

            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Get the card view from the dragged object
            CardView sourceCardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (sourceCardView == null || sourceCardView.Card == null)
            {
                return;
            }

            // Get the attacking card
            var attackingCard = sourceCardView.Card;

            // Get access to the match manager directly from battlefield view
            var matchManager = battlefieldView.GetMatchManager();

            // Check if there's a card in this slot to target
            var targetPlayer = matchManager?.Opponent;
            var targetCard = targetPlayer?.Battlefield.GetCardAt(slotIndex);

            if (targetCard != null && matchManager != null)
            {
                // Try to attack from the attacking card to the target card
                bool attackSuccess = matchManager.InitiateAttack(attackingCard, targetCard);

                // The UI will be updated through events from MatchManager
            }

            // Return the attacking card to its original position (handled by UnitAttackDragHandler)

            // Clear any highlights
            battlefieldView.ClearCardHighlights();
        }


        // Adds a public getter to check if this slot has a card
        public bool HasCard()
        {
            return CardContainer.GetComponentInChildren<CardView>() != null;
        }

        // Adds a public getter to get the current card view
        public CardView GetCardView()
        {
            return CardContainer.GetComponentInChildren<CardView>();
        }

        /// <summary>
        /// Updates the highlight state of this slot based on the provided condition
        /// </summary>
        /// <param name="shouldHighlight">Whether this slot should be highlighted</param>
        public void UpdateHighlight(bool shouldHighlight)
        {
            if (shouldHighlight)
            {
                // Use a standard highlight color for targetable cards
                SetHighlight(new Color(1f, 0.5f, 0f, 0.5f), true); // Orange highlight
            }
            else
            {
                ClearHighlight();
            }
        }
    }
}
