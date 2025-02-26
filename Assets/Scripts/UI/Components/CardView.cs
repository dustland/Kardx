using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kardx.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components
{
    using Card = Kardx.Core.Card;

    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Card Data")]
        [SerializeField]
        private Card card;

        [SerializeField]
        private CardType cardType;

        [Header("Core Components")]
        [SerializeField]
        private Image backgroundImage; // The main card background/frame

        [Header("Content Elements")]
        [SerializeField]
        private Image artworkImage; // The card's artwork

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI descriptionText;

        [SerializeField]
        private TextMeshProUGUI deploymentCostText;

        [SerializeField]
        private TextMeshProUGUI operationCostText;

        [SerializeField]
        private TextMeshProUGUI attackText;

        [SerializeField]
        private TextMeshProUGUI defenceText;

        [SerializeField]
        private TextMeshProUGUI abilityText;

        [SerializeField]
        private TextMeshProUGUI abilityDescriptionText;

        [Header("Card Back")]
        [SerializeField]
        private Image cardBackOverlay; // Overlay image for face down state

        private static readonly Dictionary<Faction, string> FactionCardBacks = new()
        {
            { Faction.UnitedStates, "CardBacks/BasicGermanB1" },
            { Faction.SovietUnion, "CardBacks/SovietBasicB1" },
            { Faction.BritishEmpire, "CardBacks/BasicGermanB1" },
            { Faction.ThirdReich, "CardBacks/BasicGermanB1" },
            { Faction.Empire, "CardBacks/SovietBasicB1" },
            { Faction.Neutral, "CardBacks/BasicGermanB1" },
        };

        [Header("Effects")]
        [SerializeField]
        private GameObject highlightEffect;
        private CardDragHandler dragHandler;
        private CanvasGroup canvasGroup;
        private bool isDraggable = true;
        private bool isDragging = false;

        private static CardDetailView sharedDetailView;

        public static void InitializeSharedDetailView(CardDetailView detailView)
        {
            sharedDetailView = detailView;
            Debug.Log("[CardView] Initialized shared CardDetailView reference");
        }

        public Card Card => card;
        public CardType CardType => cardType;

        private void Awake()
        {
            // Initialize isDragging to false
            isDragging = false;
            Debug.Log($"[CardView] Initializing {gameObject.name} with isDragging = {isDragging}");

            // Ensure we have the basic required components
            if (GetComponent<RectTransform>() == null)
            {
                gameObject.AddComponent<RectTransform>();
            }

            // The main Image component should be on this GameObject
            backgroundImage = GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }
            backgroundImage.raycastTarget = true;

            // Get or add CanvasGroup for drag opacity
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Get drag handler
            dragHandler = GetComponent<CardDragHandler>();
            if (dragHandler != null)
            {
                dragHandler.OnDragStarted += () => isDragging = true;
                dragHandler.OnDragEnded += (success) =>
                {
                    isDragging = false;
                    if (success)
                    {
                        CancelInvoke(nameof(ShowDetail));
                    }
                };
            }

            // Ensure card back overlay is initially inactive
            if (cardBackOverlay != null)
            {
                cardBackOverlay.gameObject.SetActive(false);
            }

            Debug.Log(
                $"[CardView] Initialized {gameObject.name} - Image: {backgroundImage != null}, "
                    + $"RaycastTarget: {backgroundImage.raycastTarget}, "
                    + $"CanvasGroup: {canvasGroup != null}, "
                    + $"DragHandler: {dragHandler != null}"
            );
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log(
                $"[CardView] OnPointerClick on {gameObject.name} (isDragging: {isDragging}, EventCamera: {eventData.enterEventCamera?.name}, PointerPress: {eventData.pointerPress?.name})"
            );

            // Check if the CanvasGroup is blocking raycasts
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"[CardView] CanvasGroup.blocksRaycasts: {canvasGroup.blocksRaycasts}");
            }

            // Only handle the click if this GameObject is the actual target
            if (eventData.pointerPress != gameObject)
            {
                Debug.Log("[CardView] Ignoring click as it's not directly on this card");
                return;
            }

            // Check if this is a player card in the battlefield
            bool isInBattlefield =
                transform.parent != null
                && transform.parent.name.Contains("CardSlot")
                && !transform.parent.name.Contains("Opponent");
            if (isInBattlefield)
            {
                Debug.Log(
                    $"[CardView] Card is in player battlefield. isDragging: {isDragging}, Card: {(card != null ? card.Title : "null")}"
                );
            }

            if (!isDragging)
            {
                ShowDetail();
            }
            else
            {
                Debug.Log("[CardView] Not showing detail because card is being dragged");
            }
        }

        public void UpdateCardView()
        {
            try
            {
                if (card == null && cardType == null)
                {
                    Debug.LogWarning("[CardView] No card data available for update");
                    return;
                }

                // Get the active card data source (either Card or CardType)
                var name = card != null ? card.Title : cardType?.Title ?? "";
                var description = card != null ? card.Description : cardType?.Description ?? "";
                var deploymentCost =
                    card != null ? card.DeploymentCost : cardType?.DeploymentCost ?? 0;
                var operationCost =
                    card != null ? card.OperationCost : cardType?.OperationCost ?? 0;
                var attack = card != null ? card.Attack : cardType?.BaseAttack ?? 0;
                var defence = card != null ? card.CurrentDefence : cardType?.BaseDefence ?? 0;
                var imageUrl = card != null ? card.ImageUrl : cardType?.ImageUrl;
                var abilities = card != null ? card.CardType?.Abilities : cardType?.Abilities;

                // Update UI elements safely
                if (nameText != null)
                    nameText.text = name;
                if (descriptionText != null)
                    descriptionText.text = description;
                if (deploymentCostText != null)
                    deploymentCostText.text = deploymentCost.ToString();
                if (operationCostText != null)
                    operationCostText.text = operationCost.ToString();
                if (attackText != null)
                    attackText.text = attack.ToString();
                if (defenceText != null)
                    defenceText.text = defence.ToString();

                // Handle abilities if available
                if (abilities != null && abilities.Count > 0 && abilities[0] != null)
                {
                    if (abilityText != null)
                        abilityText.text = abilities[0].Name ?? "";
                    if (abilityDescriptionText != null)
                        abilityDescriptionText.text = abilities[0].Description ?? "";
                }
                else
                {
                    if (abilityText != null)
                        abilityText.text = "";
                    if (abilityDescriptionText != null)
                        abilityDescriptionText.text = "";
                }

                UpdateCardFrame();
                if (card != null)
                {
                    UpdateModifierEffects();
                }

                // Always load the card image, even for face-down cards
                // This ensures the image is ready when the card is flipped
                LoadCardImage();

                // After all UI elements are initialized, check if the card should be face down
                // Show or hide the card back overlay based on the card's face down state
                ShowCardBack(card?.FaceDown ?? false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardView] Error updating card view: {ex.Message}");
                throw;
            }
        }

        private void UpdateCardFrame()
        {
            var category =
                card != null ? card.CardType.Category : cardType?.Category ?? CardCategory.Unit;

            switch (category)
            {
                case CardCategory.Unit:
                    break;
                case CardCategory.Order:
                    break;
                case CardCategory.Countermeasure:
                    break;
                case CardCategory.Headquarter:
                    break;
            }
        }

        private void UpdateModifierEffects()
        {
            foreach (var modifier in card.Modifiers) { }
        }

        private void LoadCardImage()
        {
            if (card == null && cardType == null)
            {
                Debug.LogWarning("[CardView] Card image component or card type is missing");
                return;
            }

            // Use the card's image URL if available, otherwise use the card type's image URL
            string imageUrl = null;

            if (card != null)
            {
                imageUrl = card.ImageUrl;
                // If card has no image URL but has a CardType, try to use the CardType's image URL
                if (string.IsNullOrEmpty(imageUrl) && card.CardType != null)
                {
                    imageUrl = card.CardType.ImageUrl;
                }
            }
            else if (cardType != null)
            {
                imageUrl = cardType.ImageUrl;
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.LogWarning(
                    $"[CardView] No image URL specified for card {(card != null ? card.Title : cardType?.Title ?? "Unknown")}"
                );
                // Just log the warning, no placeholder needed
                return;
            }

            try
            {
                // Remove file extension as Unity adds its own
                string fileName = Path.GetFileNameWithoutExtension(imageUrl);

                // Load sprite from Cards folder
                Sprite sprite = Resources.Load<Sprite>($"Cards/{fileName}");

                if (sprite != null)
                {
                    artworkImage.sprite = sprite;
                    artworkImage.preserveAspect = true;
                    Debug.Log($"[CardView] Successfully loaded image for {imageUrl}");
                }
                else
                {
                    Debug.LogError($"[CardView] Failed to load image from Cards/{fileName}");
                    // Just log the error, no placeholder needed
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardView] Error loading image for {imageUrl}: {e.Message}");
                // Just log the error, no placeholder needed
            }
        }

        public void SetHighlight(bool isHighlighted)
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(isHighlighted);
            }
        }

        public void SetDraggable(bool canDrag)
        {
            isDraggable = canDrag;
            if (dragHandler != null)
            {
                dragHandler.enabled = canDrag;
            }

            // When a card is deployed to the battlefield, we disable dragging
            // Make sure to reset the isDragging flag to ensure the card can be clicked
            if (!canDrag)
            {
                isDragging = false;
                Debug.Log(
                    $"[CardView] Card {(card != null ? card.Title : "unknown")} is no longer draggable, resetting isDragging to false"
                );
            }
        }

        /// <summary>
        /// Explicitly resets the isDragging flag to ensure the card can be clicked.
        /// This is useful when the card is moved from hand to battlefield.
        /// </summary>
        public void ResetDraggingState()
        {
            isDragging = false;
            Debug.Log(
                $"[CardView] Explicitly reset isDragging flag for {(card != null ? card.Title : "unknown")}"
            );
        }

        public void SetFaceDown(bool faceDown)
        {
            // Only update the UI state, don't modify the card's state
            ShowCardBack(faceDown);
        }

        public void Initialize(Card card)
        {
            this.card = card;
            this.cardType = card?.CardType;

            // Update the UI to reflect the card's current state
            UpdateCardView();
        }

        public void Initialize(CardType cardType)
        {
            this.card = null;
            this.cardType = cardType;
            SetDraggable(false); // CardType view should not be draggable
            UpdateCardView();
        }

        public void ShowDetail()
        {
            Debug.Log("[CardView] Starting ShowDetail method");
            if (isDragging)
            {
                Debug.Log("[CardView] is dragging, not showing detail");
                return;
            }

            Debug.Log(
                $"[CardView] SharedDetailView is {(sharedDetailView != null ? "not null" : "null")}"
            );
            if (sharedDetailView != null)
            {
                Debug.Log("[CardView] Using shared card detail view");
                if (card != null)
                {
                    if (card.FaceDown)
                    {
                        Debug.Log(
                            $"[CardView] The card {card.Title} is face down, not showing detail"
                        );
                        return;
                    }
                    else
                    {
                        Debug.Log($"[CardView] Showing card: {card.Title}");
                        sharedDetailView.Show(card);
                    }
                }
                else if (cardType != null)
                {
                    Debug.Log($"[CardView] Showing cardType: {cardType.Title}");
                    sharedDetailView.Show(cardType);
                }
                else
                {
                    Debug.LogWarning("[CardView] Both card and cardType are null");
                }
            }
            else
            {
                Debug.LogWarning(
                    "[CardView] No shared CardDetailView has been initialized. Call CardView.InitializeSharedDetailView first."
                );
            }
            Debug.Log("[CardView] ShowDetail method completed");
        }

        private void ShowCardBack(bool show = true)
        {
            Debug.Log(
                $"[CardView] ShowCardBack called with show={show} for card: {(card != null ? card.Title : "null")} with faction: {(card != null ? card.OwnerFaction.ToString() : "null")}"
            );

            if (!show)
            {
                // If we're not showing the card back, just hide the overlay and return
                if (cardBackOverlay != null)
                {
                    cardBackOverlay.gameObject.SetActive(false);
                }
                return;
            }

            if (cardBackOverlay != null && card != null)
            {
                // Load the appropriate card back sprite based on card's owner faction
                string cardBackPath = FactionCardBacks.GetValueOrDefault(
                    card.OwnerFaction,
                    FactionCardBacks[Faction.Neutral]
                );
                Debug.Log($"[CardView] Attempting to load card back from path: {cardBackPath}");

                Sprite cardBackSprite = Resources.Load<Sprite>(cardBackPath);

                if (cardBackSprite != null)
                {
                    Debug.Log(
                        $"[CardView] Successfully loaded card back sprite for faction {card.OwnerFaction}"
                    );
                    cardBackOverlay.sprite = cardBackSprite;
                    cardBackOverlay.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning(
                        $"[CardView] Failed to load card back sprite for faction {card.OwnerFaction} at path: {cardBackPath}"
                    );

                    // Try loading with a direct path to the Resources/Cards folder as fallback
                    string fallbackPath = $"Cards/{Path.GetFileName(cardBackPath)}";
                    Debug.Log($"[CardView] Trying fallback path: {fallbackPath}");
                    cardBackSprite = Resources.Load<Sprite>(fallbackPath);

                    if (cardBackSprite != null)
                    {
                        Debug.Log($"[CardView] Successfully loaded card back from fallback path");
                        cardBackOverlay.sprite = cardBackSprite;
                        cardBackOverlay.gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError(
                            $"[CardView] Failed to load card back from fallback path. Using neutral back as last resort"
                        );
                        // Load neutral back as fallback
                        cardBackSprite = Resources.Load<Sprite>(FactionCardBacks[Faction.Neutral]);

                        if (cardBackSprite == null)
                        {
                            Debug.LogError(
                                "[CardView] Even neutral card back failed to load! Creating placeholder image."
                            );
                            // Skip placeholder creation
                            cardBackOverlay.gameObject.SetActive(false);
                        }
                        else
                        {
                            cardBackOverlay.sprite = cardBackSprite;
                            cardBackOverlay.gameObject.SetActive(true);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    $"[CardView] Missing card back overlay image component ({cardBackOverlay == null}) or card is null ({card == null})"
                );

                // If we have a card back overlay but no card, still show a placeholder
                if (cardBackOverlay != null && card == null)
                {
                    // Skip placeholder creation
                    cardBackOverlay.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (dragHandler != null)
            {
                dragHandler.OnDragStarted -= () => isDragging = true;
                dragHandler.OnDragEnded -= (success) => isDragging = false;
            }
        }
    }
}
