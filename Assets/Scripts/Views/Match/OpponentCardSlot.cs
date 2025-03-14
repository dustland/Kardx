using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;
using Kardx.Controllers.DragHandlers;

namespace Kardx.Views.Match
{
    /// <summary>
    /// UI component representing a slot in the opponent's battlefield.
    /// Handles targeting for player abilities and attacks.
    /// </summary>
    public class OpponentCardSlot : MonoBehaviour, IDropHandler
    {
        private Image highlightImage;

        [SerializeField]
        private Color targetableHighlightColor = new Color(1.0f, 0.5f, 0.0f, 0.5f);

        [SerializeField]
        private Color selectedHighlightColor = new Color(1.0f, 1.0f, 0.0f, 0.5f);

        private int slotIndex;
        private OpponentBattlefieldView battlefieldView;
        private bool isHighlighted = false;

        public int SlotIndex => slotIndex;
        
        // Remove the CardContainer property and use transform directly
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
            
            // Get the battlefield view component
            battlefieldView = GetComponentInParent<OpponentBattlefieldView>();
            if (battlefieldView == null)
            {
                Debug.LogError("[OpponentCardSlot] Could not find OpponentBattlefieldView parent");
            }

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
                highlightImage.color = targetableHighlightColor;
                highlightImage.enabled = false;
                highlightImage.raycastTarget = false; // Don't block raycasts on the highlight
            }
            else
            {
                // Make sure the highlight doesn't block raycasts
                highlightImage.raycastTarget = false;
            }

            // Remove the CardContainer creation
            // We'll place cards directly as children of this slot

            // Make sure this GameObject has a RectTransform
            if (GetComponent<RectTransform>() == null)
            {
                Debug.LogError("[OpponentCardSlot] Missing RectTransform component!");
            }

            // Log that we're ready to receive drops
            Debug.Log($"[OpponentCardSlot] Initialized slot {slotIndex} with raycastTarget={image.raycastTarget}");
        }

        public void Initialize(int index, OpponentBattlefieldView view)
        {
            slotIndex = index;
            battlefieldView = view;

            // Create highlight image if it doesn't exist
            if (highlightImage == null)
            {
                CreateHighlightImage();
            }

            // Initialize highlight image
            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }

            // Ensure raycastTarget is enabled for drop events
            Image slotImage = GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.raycastTarget = true;
                Debug.Log($"[OpponentCardSlot] Initialized slot {slotIndex} with raycastTarget = {slotImage.raycastTarget}");
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

            // Debug.Log($"[OpponentCardSlot] Created highlight image for slot {slotIndex}");
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
            Debug.Log($"[OpponentCardSlot] OnDrop called on slot {slotIndex}");

            // Check if this slot is a valid target
            if (!IsTargetable())
            {
                Debug.LogWarning($"[OpponentCardSlot] Slot {slotIndex} is not targetable");
                return;
            }

            // Try to get the CardView from the dragged object
            GameObject draggedObject = eventData.pointerDrag;
            if (draggedObject == null)
            {
                Debug.LogWarning("[OpponentCardSlot] OnDrop - No dragged object");
                return;
            }

            CardView cardView = draggedObject.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.LogWarning("[OpponentCardSlot] OnDrop - No valid CardView found in the dragged object");
                return;
            }

            // Try to get the AbilityDragHandler from the dragged object
            AbilityDragHandler abilityDragHandler = draggedObject.GetComponent<AbilityDragHandler>();
            if (abilityDragHandler == null)
            {
                Debug.LogWarning("[OpponentCardSlot] OnDrop - No AbilityDragHandler found in the dragged object");
                return;
            }

            Debug.Log($"[OpponentCardSlot] Processing attack from {cardView.Card.Title} to slot {slotIndex}");

            // Get the target card
            CardView targetCardView = GetCardView();
            if (targetCardView == null)
            {
                Debug.LogWarning("[OpponentCardSlot] OnDrop - No target card found in this slot");
                return;
            }

            // Attempt to perform the attack
            bool attackSuccessful = false;
            try
            {
                // Get the match manager
                MatchManager matchManager = UnityEngine.Object.FindAnyObjectByType<MatchManager>();
                if (matchManager == null)
                {
                    Debug.LogError("[OpponentCardSlot] OnDrop - MatchManager not found");
                    return;
                }

                // Perform the attack
                attackSuccessful = matchManager.InitiateAttack(cardView.Card, targetCardView.Card);
                Debug.Log($"[OpponentCardSlot] Attack result: {(attackSuccessful ? "Success" : "Failed")}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OpponentCardSlot] Error during attack: {ex.Message}");
                return;
            }

            // Notify the ability drag handler that the drop was successful
            if (attackSuccessful)
            {
                Debug.Log("[OpponentCardSlot] Notifying AbilityDragHandler of successful drop");
                abilityDragHandler.NotifyDropSuccess();
            }
            else
            {
                Debug.LogWarning("[OpponentCardSlot] Attack was not successful");
            }

            // Clear highlights
            if (battlefieldView != null)
            {
                battlefieldView.ClearCardHighlights();
            }
        }

        // Adds a public getter to check if this slot has a card
        public bool HasCard()
        {
            return GetCardView() != null;
        }

        // Adds a public getter to get the current card view
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

        public void AddCard(CardView cardView)
        {
            if (cardView == null)
                return;

            // Set the card's parent directly to this slot (not to a container)
            cardView.transform.SetParent(transform);
            cardView.transform.localPosition = Vector3.zero;
            cardView.transform.localScale = Vector3.one;
        }

        public void RemoveCard()
        {
            CardView cardView = GetCardView();
            if (cardView != null)
            {
                Destroy(cardView.gameObject);
            }
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
                SetHighlight(targetableHighlightColor, true);
            }
            else
            {
                ClearHighlight();
            }
        }

        private bool IsTargetable()
        {
            // TO DO: implement logic to determine if this slot is targetable
            return true;
        }
    }
}
