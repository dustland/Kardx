using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Kardx.Core;

namespace Kardx.UI.Components
{
    /// <summary>
    /// UI component representing a slot in the player's battlefield.
    /// </summary>
    public class PlayerCardSlot : MonoBehaviour, IDropHandler
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
        private Image highlightImage;
        
        [SerializeField]
        private Transform cardContainer;
        
        [SerializeField]
        private Color availableHighlightColor = new Color(0.0f, 1.0f, 0.0f, 0.3f);
        
        [SerializeField]
        private Color dropTargetHighlightColor = new Color(0.0f, 1.0f, 0.0f, 0.6f);
        
        [SerializeField]
        private Color selectedHighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.5f);
        
        [SerializeField]
        private Color invalidHighlightColor = new Color(1.0f, 0.0f, 0.0f, 0.3f);
        
        private int slotIndex;
        private PlayerBattlefieldView battlefieldView;
        private CardView currentCardView;
        private HighlightType currentHighlightType = HighlightType.None;
        
        public int SlotIndex => slotIndex;
        public Transform CardContainer => cardContainer ?? transform;
        
        public void Initialize(int slotIndex, PlayerBattlefieldView battlefieldView)
        {
            this.slotIndex = slotIndex;
            this.battlefieldView = battlefieldView;
            
            // Initialize highlight
            if (highlightImage == null)
            {
                // Create highlight image if it doesn't exist
                GameObject highlightObj = new GameObject("Highlight");
                highlightObj.transform.SetParent(transform, false);
                highlightObj.transform.SetAsFirstSibling(); // Put it at the back
                
                // Make it fill the slot
                RectTransform rectTransform = highlightObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                // Add image component
                highlightImage = highlightObj.AddComponent<Image>();
                highlightImage.color = availableHighlightColor;
                highlightImage.raycastTarget = false; // Don't block raycasts
            }
            
            // Ensure highlight is initially disabled
            if (highlightImage != null)
            {
                highlightImage.gameObject.SetActive(false);
            }
            
            // Initialize card container if not set
            if (cardContainer == null)
            {
                cardContainer = transform;
            }
        }
        
        /// <summary>
        /// Set the highlight based on a highlight type. This handles the proper color and visibility.
        /// </summary>
        public void SetHighlightState(HighlightType highlightType)
        {
            currentHighlightType = highlightType;
            
            if (highlightImage == null)
                return;
                
            switch (highlightType)
            {
                case HighlightType.None:
                    highlightImage.gameObject.SetActive(false);
                    break;
                    
                case HighlightType.Available:
                    highlightImage.gameObject.SetActive(true);
                    highlightImage.color = availableHighlightColor;
                    break;
                    
                case HighlightType.DropTarget:
                    highlightImage.gameObject.SetActive(true);
                    highlightImage.color = dropTargetHighlightColor;
                    break;
                    
                case HighlightType.Selected:
                    highlightImage.gameObject.SetActive(true);
                    highlightImage.color = selectedHighlightColor;
                    break;
                    
                case HighlightType.Invalid:
                    highlightImage.gameObject.SetActive(true);
                    highlightImage.color = invalidHighlightColor;
                    break;
            }
        }
        
        /// <summary>
        /// Get the current highlight type
        /// </summary>
        public HighlightType GetHighlightState()
        {
            return currentHighlightType;
        }
        
        /// <summary>
        /// Set a highlight for this slot with the specified color and active state.
        /// </summary>
        public void SetHighlight(Color color, bool active = true)
        {
            if (highlightImage != null)
            {
                highlightImage.gameObject.SetActive(active);
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
            if (highlightImage != null && highlightImage.gameObject.activeSelf)
            {
                // Get current color and adjust alpha based on intensity
                Color currentColor = highlightImage.color;
                // Preserve the base alpha value but scale it by intensity
                float baseAlpha = currentColor.a / intensity; // Get what the base alpha would be
                currentColor.a = baseAlpha * intensity;       // Apply the new intensity
                
                highlightImage.color = currentColor;
            }
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            if (!IsHighlighted())
            {
                Debug.LogWarning($"[PlayerCardSlot] Card slot {slotIndex} is not a valid drop target. Current highlight: {currentHighlightType}");
                return;
            }
            
            // Get the card view from the dragged object
            var cardView = eventData.pointerDrag.GetComponent<CardView>();
            if (cardView == null || cardView.Card == null)
            {
                Debug.LogError("[PlayerCardSlot] Dropped item is not a valid card");
                return;
            }

            Debug.Log($"[PlayerCardSlot] Card {cardView.Card.Title} dropped on slot {slotIndex}");

            // Get access to the match manager directly from battlefield view
            var matchManager = battlefieldView.GetMatchManager();
            if (matchManager != null)
            {
                Card card = cardView.Card;
                
                // Call the appropriate game logic method directly on MatchManager
                bool success = false;
                
                if (card.IsUnitCard)
                {
                    // For unit cards, deploy to the specific slot
                    Debug.Log($"[PlayerCardSlot] Deploying unit card to slot {slotIndex}");
                    success = matchManager.DeployUnitCard(card, slotIndex);
                }
                else if (card.IsOrderCard)
                {
                    // For order cards, just deploy 
                    Debug.Log("[PlayerCardSlot] Deploying order card");
                    success = matchManager.DeployOrderCard(card);
                }
                
                Debug.Log($"[PlayerCardSlot] Deployment {(success ? "succeeded" : "failed")}");
                
                // We don't destroy the card anymore - HandleCardDeployed will handle it
                // The drag handlers now leave the cards for HandleCardDeployed to reposition
                // If deployment was successful, the UI will be updated through the OnCardDeployed event from MatchManager
            }
            else
            {
                Debug.LogError("[PlayerCardSlot] Cannot deploy card - MatchManager is null");
            }
            
            // Clear all highlights - we access this through the battlefield view now 
            battlefieldView.ClearHighlights();
        }
        
        // Method to clear the current card view
        public void ClearCardView()
        {
            // CardSlot should not be responsible for destroying GameObjects
            // It should only update its reference, while actual destruction
            // should be handled by MatchView
            
            // Simply clear the reference to the card
            currentCardView = null;
        }
        
        // Method to set a new card view
        public void SetCardView(CardView cardView)
        {
            currentCardView = cardView;
        }
        
        private bool IsHighlighted()
        {
            // Accept both DropTarget and Available states as valid drop targets
            return currentHighlightType == HighlightType.DropTarget || 
                   currentHighlightType == HighlightType.Available;
        }
    }
}
