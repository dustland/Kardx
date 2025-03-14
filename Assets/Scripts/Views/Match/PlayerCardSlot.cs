using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Kardx.Views.Cards;
using Kardx.Models.Cards;

namespace Kardx.Views.Match
{
    /// <summary>
    /// UI component representing a slot in the player's battlefield.
    /// </summary>
    public class PlayerCardSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// Types of highlights that can be applied to a card slot
        /// </summary>
        public enum HighlightType
        {
            None,           // No highlight
            Available,      // Slot is available for card placement
            DropTarget,     // Slot is being hovered for dropping
            Selected,       // Slot is selected
            Invalid         // Slot cannot be used (invalid target)
        }

        [SerializeField]
        private Color validDropHighlightColor = new Color(0.0f, 1.0f, 0.0f, 0.5f);

        [SerializeField]
        private Color invalidDropHighlightColor = new Color(1.0f, 0.0f, 0.0f, 0.5f);

        [SerializeField]
        private Color selectedHighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.5f);

        private Image highlightImage;
        private int slotIndex;
        private PlayerBattlefieldView battlefieldView;
        private bool isHighlighted = false;

        public int SlotIndex => slotIndex;
        
        public Transform CardContainer => transform;

        private void Awake()
        {
            // Ensure we have an Image component for raycasting
            Image image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
                // Make it transparent but raycast target
                image.color = new Color(0, 0, 0, 0.01f); // Very slight alpha to ensure raycasting works
            }
            else
            {
                // Make sure the image has some alpha for raycasting
                Color color = image.color;
                if (color.a <= 0)
                {
                    color.a = 0.01f; // Very slight alpha to ensure raycasting works
                    image.color = color;
                }
            }
            
            // Ensure raycast target is enabled
            image.raycastTarget = true;
            
            // Get the highlight image if it exists
            highlightImage = transform.Find("Highlight")?.GetComponent<Image>();
            if (highlightImage == null)
            {
                // Create a highlight image if it doesn't exist
                GameObject highlightObj = new GameObject("Highlight");
                highlightObj.transform.SetParent(transform);
                highlightObj.transform.localPosition = Vector3.zero;
                highlightObj.transform.localScale = Vector3.one;
                
                // Make the highlight fill the slot
                RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
                highlightRect.anchorMin = Vector2.zero;
                highlightRect.anchorMax = Vector2.one;
                highlightRect.offsetMin = Vector2.zero;
                highlightRect.offsetMax = Vector2.zero;
                
                // Add the image component
                highlightImage = highlightObj.AddComponent<Image>();
                highlightImage.color = validDropHighlightColor;
                highlightImage.enabled = false;
                highlightImage.raycastTarget = false; // Don't block raycasts on the highlight
            }
            else
            {
                // Make sure the highlight doesn't block raycasts
                highlightImage.raycastTarget = false;
            }

            // Make sure this GameObject has a RectTransform
            if (GetComponent<RectTransform>() == null)
            {
                Debug.LogError("[PlayerCardSlot] Missing RectTransform component!");
            }
        }

        public void Initialize(int slotIndex, PlayerBattlefieldView battlefieldView)
        {
            this.slotIndex = slotIndex;
            this.battlefieldView = battlefieldView;
            
            // If the battlefield view wasn't set in Awake, set it now
            if (this.battlefieldView == null)
            {
                this.battlefieldView = battlefieldView;
            }

            // Log that we're ready to receive drops
            Debug.Log($"[PlayerCardSlot] Initialized slot {slotIndex} with raycastTarget={GetComponent<Image>()?.raycastTarget ?? false}");
        }

        /// <summary>
        /// Set the highlight based on a highlight type. This handles the proper color and visibility.
        /// </summary>
        public void SetHighlightState(HighlightType highlightType)
        {
            if (highlightImage == null)
                return;

            switch (highlightType)
            {
                case HighlightType.None:
                    highlightImage.enabled = false;
                    break;

                case HighlightType.Available:
                    highlightImage.enabled = true;
                    highlightImage.color = validDropHighlightColor;
                    break;

                case HighlightType.DropTarget:
                    highlightImage.enabled = true;
                    highlightImage.color = validDropHighlightColor;
                    break;

                case HighlightType.Selected:
                    highlightImage.enabled = true;
                    highlightImage.color = selectedHighlightColor;
                    break;

                case HighlightType.Invalid:
                    highlightImage.enabled = true;
                    highlightImage.color = invalidDropHighlightColor;
                    break;
            }
        }

        /// <summary>
        /// Get the current highlight type
        /// </summary>
        public HighlightType GetHighlightState()
        {
            if (highlightImage.enabled)
            {
                if (highlightImage.color == validDropHighlightColor)
                    return HighlightType.Available;
                else if (highlightImage.color == selectedHighlightColor)
                    return HighlightType.Selected;
                else if (highlightImage.color == invalidDropHighlightColor)
                    return HighlightType.Invalid;
                else
                    return HighlightType.DropTarget;
            }
            else
                return HighlightType.None;
        }

        /// <summary>
        /// Set a highlight for this slot with the specified color and active state.
        /// </summary>
        public void SetHighlight(Color color, bool active = true)
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = active;
                highlightImage.color = color;
            }
        }

        /// <summary>
        /// Clear any highlight on this slot.
        /// </summary>
        public void ClearHighlight()
        {
            SetHighlightState(HighlightType.None);
        }

        /// <summary>
        /// Set the intensity of the current highlight by adjusting its alpha value.
        /// </summary>
        /// <param name="intensity">Intensity multiplier (1.0f is normal)</param>
        public void SetHighlightIntensity(float intensity)
        {
            if (highlightImage != null && highlightImage.enabled)
            {
                // Get current color and adjust alpha based on intensity
                Color currentColor = highlightImage.color;
                // Preserve the base alpha value but scale it by intensity
                float baseAlpha = currentColor.a / intensity; // Get what the base alpha would be
                currentColor.a = baseAlpha * intensity;       // Apply the new intensity

                highlightImage.color = currentColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Check if there's a card being dragged
            var draggingCardView = eventData.pointerDrag?.GetComponent<CardView>();
            var draggingCard = draggingCardView?.Card;

            // Ignore order cards completely - don't highlight or accept them
            if (draggingCard != null && draggingCard.IsOrderCard)
            {
                // Explicitly ignore order cards by not highlighting
                return;
            }

            // Get the match manager
            var matchManager = battlefieldView.GetMatchManager();
            if (matchManager == null)
                return;

            // For unit cards, highlight the slot if it's available AND only if the card is from the hand (not battlefield)
            if (draggingCard != null && draggingCard.IsUnitCard)
            {
                // Check if the card is in the player's hand (new unit deployment)
                // and NOT on the battlefield (attacking)
                bool isCardInHand = matchManager.Player.Hand.Contains(draggingCard);
                bool isCardOnBattlefield = matchManager.Player.Battlefield.Contains(draggingCard);

                // Only highlight if:
                // 1. Card is in hand (not on battlefield)
                // 2. This slot is empty
                if (isCardInHand && !isCardOnBattlefield &&
                    matchManager.Player.Battlefield.GetCardAt(slotIndex) == null)
                {
                    SetHighlightState(HighlightType.DropTarget);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Check the current highlight state to determine what to do
            if (GetHighlightState() == HighlightType.DropTarget)
            {
                // If this was highlighted as DropTarget, restore the previous state
                // (which might be Available or None)
                SetHighlightState(HighlightType.None);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!IsHighlighted())
            {
                Debug.LogWarning($"[PlayerCardSlot] Card slot {slotIndex} is not a valid drop target. Current highlight: {GetHighlightState()}");
                return;
            }

            // Get the card view from the dragged object
            var cardView = eventData.pointerDrag.GetComponent<CardView>();
            if (cardView == null || cardView.Card == null)
            {
                Debug.LogError("[PlayerCardSlot] Dropped item is not a valid card");
                return;
            }

            // Early exit if this is an Order card - we want these to be handled by the OrderDropHandler on the canvas
            if (cardView.Card.IsOrderCard)
            {
                Debug.Log($"[PlayerCardSlot] Ignoring Order card {cardView.Card.Title} - this should be handled by OrderDropHandler");
                return;
            }

            // Re-enable raycast blocking on the dragged card
            var canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            Debug.Log($"[PlayerCardSlot] Unit Card {cardView.Card.Title} dropped on slot {slotIndex}");

            // Get access to the match manager directly from battlefield view
            var matchManager = battlefieldView.GetMatchManager();
            if (matchManager != null)
            {
                Card card = cardView.Card;

                // Only deploy unit cards - order cards should be handled by OrderDropHandler
                if (card.IsUnitCard)
                {
                    // For unit cards, deploy to the specific slot
                    Debug.Log($"[PlayerCardSlot] Deploying unit card to slot {slotIndex}");
                    bool success = matchManager.DeployCard(card, slotIndex);
                    Debug.Log($"[PlayerCardSlot] Deployment {(success ? "succeeded" : "failed")}");
                }
            }
            else
            {
                Debug.LogError("[PlayerCardSlot] Cannot deploy card - MatchManager is null");
            }

            // Clear all highlights - we access this through the battlefield view now 
            battlefieldView.ClearCardHighlights();
        }

        public void AddCard(CardView cardView)
        {
            if (cardView == null)
                return;

            // Set the card's parent directly to this slot (not to a container)
            cardView.transform.SetParent(transform);
            cardView.transform.localPosition = Vector3.zero;
            cardView.transform.localScale = Vector3.one;
        }

        public CardView GetCardView()
        {
            // Look for a CardView directly in the children of this slot
            // Skip the Highlight object
            foreach (Transform child in transform)
            {
                if (child.name != "Highlight")
                {
                    CardView cardView = child.GetComponent<CardView>();
                    if (cardView != null)
                        return cardView;
                }
            }
            return null;
        }

        public void RemoveCard()
        {
            CardView cardView = GetCardView();
            if (cardView != null)
            {
                Destroy(cardView.gameObject);
            }
        }

        public bool HasCard()
        {
            return GetCardView() != null;
        }

        private bool IsHighlighted()
        {
            // Accept both DropTarget and Available states as valid drop targets
            return GetHighlightState() == HighlightType.DropTarget ||
                   GetHighlightState() == HighlightType.Available;
        }
    }
}
