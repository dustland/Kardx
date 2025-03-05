using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Kardx.Core;
using Kardx.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI
{
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Card Data")]
        public bool autoUpdateUI = true;
        private Card card;
        private CardType cardType;
        private bool isBeingDragged = false; // Track if card is being dragged

        // Public property to track drag state
        public bool IsBeingDragged 
        { 
            get { return isBeingDragged; } 
            set { isBeingDragged = value; }
        }

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
        private TextMeshProUGUI categoryText;

        [SerializeField]
        private TextMeshProUGUI categoryTagText;

        [SerializeField]
        private TextMeshProUGUI deploymentCostText;

        [SerializeField]
        private TextMeshProUGUI operationCostText;

        [SerializeField]
        private TextMeshProUGUI attackText;

        [SerializeField]
        private TextMeshProUGUI defenseText;

        [SerializeField]
        private TextMeshProUGUI abilityText;

        [SerializeField]
        private TextMeshProUGUI abilityDescriptionText;

        [SerializeField]
        private Toggle hasAttackedToggle;

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

        // Reference to the drag handlers on the canvas
        private AbilityDragHandler abilityDragHandler;
        private UnitDeployDragHandler unitDeployDragHandler;
        private OrderDeployDragHandler orderDeployDragHandler;
        private CanvasGroup canvasGroup;
        private bool isDraggable = true;
        private bool isHighlighted = false;

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
            // Initialize isBeingDragged to false
            isBeingDragged = false;

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

            // Get ability drag handler
            abilityDragHandler = GetComponent<AbilityDragHandler>();

            // Set up event handlers for ability dragging
            if (abilityDragHandler != null)
            {
                abilityDragHandler.OnDragStarted += () => isBeingDragged = true;
                abilityDragHandler.OnDragEnded += (success) =>
                {
                    isBeingDragged = false;
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
        }

        public void OnPointerClick(PointerEventData eventData)
        {
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
                    $"[CardView] Card is in player battlefield. isBeingDragged: {isBeingDragged}, Card: {(card != null ? card.Title : "null")}"
                );
            }

            if (!isBeingDragged)
            {
                ShowDetail();
            }
            else
            {
                Debug.Log("[CardView] Not showing detail because card is being dragged");
            }
        }

        public void UpdateUI()
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
                var defense = card != null ? card.CurrentDefense : cardType?.BaseDefense ?? 0;
                var imageUrl = card != null ? card.ImageUrl : cardType?.ImageUrl;
                var abilities = card != null ? card.CardType?.Abilities : cardType?.Abilities;
                var categoryValue = card != null ? card.CardType?.Category : cardType?.Category;
                var category = categoryValue.HasValue ? categoryValue.ToString() : string.Empty;
                var categoryTag = !string.IsNullOrEmpty(category) ? category[0].ToString() : string.Empty;

                // Update UI elements safely
                if (nameText != null)
                    nameText.text = name;
                if (descriptionText != null)
                    descriptionText.text = description;
                if (categoryText != null)
                    categoryText.text = category;
                if (categoryTagText != null)
                    categoryTagText.text = categoryTag;
                if (deploymentCostText != null)
                    deploymentCostText.text = deploymentCost.ToString();
                if (operationCostText != null)
                    operationCostText.text = operationCost.ToString();
                if (attackText != null)
                    attackText.text = attack.ToString();
                if (defenseText != null)
                {
                    defenseText.text = defense.ToString();

                    // Optionally highlight if defense is different from max
                    if (card != null && card.CurrentDefense < card.Defense)
                    {
                        defenseText.color = Color.red;
                    }
                    else
                    {
                        defenseText.color = Color.white;
                    }
                }
                if (hasAttackedToggle != null)
                    hasAttackedToggle.isOn = card != null ? card.HasAttackedThisTurn : false;

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

                    // Update draggable state based on card location
                    bool isOnBattlefield = IsCardOnBattlefield();
                    bool canDrag = isOnBattlefield
                        ? !card.HasAttackedThisTurn
                        : (card.Owner != null);
                    SetDraggable(canDrag);
                }

                // Always load the card image, even for face-down cards
                // This ensures the image is ready when the card is flipped
                LoadCardImage();

                // Use the card's FaceDown property to control the visual state
                if (card != null)
                {
                    ShowCardBack(card.FaceDown);
                }

                if (card != null)
                {
                    Debug.Log(
                        $"[CardView] Updated UI for card: {card.Title}, Defense: {card.CurrentDefense}/{card.Defense}"
                    );
                }
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
                case CardCategory.Headquarters:
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
            this.isHighlighted = isHighlighted;
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(isHighlighted);
            }
        }

        public void SetDraggable(bool canDrag)
        {
            isDraggable = canDrag;

            // Cache references to drag handlers if not already done
            if (abilityDragHandler == null)
                abilityDragHandler = GetComponent<AbilityDragHandler>();

            if (unitDeployDragHandler == null)
                unitDeployDragHandler = GetComponent<UnitDeployDragHandler>();

            if (orderDeployDragHandler == null)
                orderDeployDragHandler = GetComponent<OrderDeployDragHandler>();

            // Check if the card is on the battlefield
            bool isOnBattlefield = IsCardOnBattlefield();
            bool isPlayerCard = card != null && card.Owner != null && !card.Owner.IsOpponent;

            // Disable all drag handlers first
            if (abilityDragHandler != null) abilityDragHandler.enabled = false;
            if (unitDeployDragHandler != null) unitDeployDragHandler.enabled = false;
            if (orderDeployDragHandler != null) orderDeployDragHandler.enabled = false;

            // Only enable drag handlers if dragging is allowed and it's a player card
            if (canDrag && isPlayerCard)
            {
                if (isOnBattlefield)
                {
                    // Cards on battlefield use AbilityDragHandler
                    if (abilityDragHandler != null)
                    {
                        abilityDragHandler.enabled = true;
                        Debug.Log($"[CardView] Card {card?.Title} is on battlefield, enabling AbilityDragHandler");
                    }
                    else
                    {
                        Debug.LogWarning($"[CardView] Card {card?.Title} is on battlefield but has no AbilityDragHandler");
                    }
                }
                else
                {
                    // Cards in hand use UnitDeployDragHandler or OrderDeployDragHandler based on card type
                    if (card != null && card.IsUnitCard && unitDeployDragHandler != null)
                    {
                        unitDeployDragHandler.enabled = true;
                        Debug.Log($"[CardView] Card {card.Title} is in hand, enabling UnitDeployDragHandler");
                    }
                    else if (card != null && !card.IsUnitCard && orderDeployDragHandler != null)
                    {
                        orderDeployDragHandler.enabled = true;
                        Debug.Log($"[CardView] Card {card.Title} is in hand, enabling OrderDeployDragHandler");
                    }
                }
            }
            else
            {
                Debug.Log($"[CardView] Card {card?.Title} is not draggable or is an opponent card");
            }

            // When a card is no longer draggable, reset the isBeingDragged flag
            if (!canDrag)
            {
                isBeingDragged = false;
                Debug.Log($"[CardView] Card {card?.Title} is no longer draggable, resetting isBeingDragged to false");
            }
        }

        /// <summary>
        /// Explicitly switches the card from deployment drag handlers to ability drag handlers
        /// Called when a card is deployed to the battlefield
        /// </summary>
        public void SwitchToAbilityDragHandler()
        {
            // Cache references to drag handlers if not already done
            if (abilityDragHandler == null)
                abilityDragHandler = GetComponent<AbilityDragHandler>();

            if (unitDeployDragHandler == null)
                unitDeployDragHandler = GetComponent<UnitDeployDragHandler>();

            if (orderDeployDragHandler == null)
                orderDeployDragHandler = GetComponent<OrderDeployDragHandler>();

            // Disable deployment drag handlers
            if (unitDeployDragHandler != null) unitDeployDragHandler.enabled = false;
            if (orderDeployDragHandler != null) orderDeployDragHandler.enabled = false;

            // Only enable ability drag handler if it's a player card
            bool isPlayerCard = card != null && card.Owner != null && !card.Owner.IsOpponent;
            if (isPlayerCard && abilityDragHandler != null)
            {
                abilityDragHandler.enabled = isDraggable;
                Debug.Log($"[CardView] Switched {card?.Title} to AbilityDragHandler (enabled: {isDraggable})");
            }
        }

        // Method to set card interactable state
        public void SetInteractable(bool interactable)
        {
            // Enable/disable canvas group interactivity
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable;

                // Always keep the alpha at 1.0 - we don't need visual differentiation for interactable state
                canvasGroup.alpha = 1.0f;
            }

            // Update draggable state
            SetDraggable(interactable && isDraggable);
        }

        // Helper method to check if a card is on the battlefield
        private bool IsCardOnBattlefield()
        {
            if (card == null || card.Owner == null)
                return false;

            // Check if the card is in the battlefield
            var battlefield = card.Owner.Battlefield;
            return battlefield.Contains(card);
        }

        public void ResetDraggingState()
        {
            isBeingDragged = false;
            Debug.Log(
                $"[CardView] Explicitly reset isBeingDragged flag for {(card != null ? card.Title : "unknown")}"
            );
        }

        public void SetFaceDown(bool faceDown)
        {
            // Update the card model if it exists
            if (card != null)
            {
                Debug.Log($"[CardView] Setting card {card.Title} FaceDown={faceDown}. Called from: {new System.Diagnostics.StackTrace().ToString()}");
                card.SetFaceDown(faceDown);
            }

            // Always update the UI state
            ShowCardBack(faceDown);
        }

        // Initialize the card with a Card object
        public void Initialize(Card card, bool faceDown = false)
        {
            this.card = card;
            this.cardType = card?.CardType;

            // Set face down state on the card model
            if (card != null)
            {
                // Make a single call to SetFaceDown which will update both model and UI
                SetFaceDown(faceDown);
            }

            // Update the UI
            UpdateUI();
        }

        // Method to set a card on this view
        public void SetCard(Card card, bool faceDown = false)
        {
            Initialize(card, faceDown);
        }

        // Alternative initialization with CardType only
        public void Initialize(CardType cardType)
        {
            this.card = null;
            this.cardType = cardType;
            SetDraggable(false); // CardType view should not be draggable
            UpdateUI();
        }

        public void ShowDetail()
        {
            Debug.Log("[CardView] Starting ShowDetail method");
            
            if (card == null)
            {
                Debug.LogWarning("[CardView] Cannot show detail - card is null");
                return;
            }

            // If the card is being dragged, don't show the detail
            if (isBeingDragged)
            {
                Debug.Log($"[CardView] Card {card.Title} is being dragged, not showing detail");
                return;
            }

            // If the card is face down, don't show the detail
            if (card.FaceDown)
            {
                Debug.Log($"[CardView] The card {card.Title} is face down, not showing detail. Card location: {transform.parent?.name ?? "unknown"}, Card visual state matches FaceDown: {cardBackOverlay?.gameObject.activeInHierarchy.ToString() ?? "unknown"}");
                return;
            }

            // Shared detail view is used across all cards
            if (sharedDetailView != null)
            {
                Debug.Log("[CardView] Using shared card detail view");
                if (card != null)
                {
                    Debug.Log($"[CardView] Showing card: {card.Title}");
                    sharedDetailView.Show(card);
                }
                else if (cardType != null)
                {
                    Debug.Log($"[CardView] Showing cardType: {cardType}");
                    sharedDetailView.Show(cardType);
                }
            }
            else
            {
                Debug.LogWarning("[CardView] No shared detail view found");
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

                Sprite cardBackSprite = Resources.Load<Sprite>(cardBackPath);

                if (cardBackSprite != null)
                {
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
            if (abilityDragHandler != null)
            {
                abilityDragHandler.OnDragStarted -= () => isBeingDragged = true;
                abilityDragHandler.OnDragEnded -= (success) => isBeingDragged = false;
            }
        }

        // Static factory method to create a card UI
        public static CardView CreateCard(
            Card card,
            Transform parent,
            bool faceDown,
            GameObject cardPrefab
        )
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[CardView] Card prefab is null");
                return null;
            }

            // Create the card GameObject
            var cardGO = Instantiate(cardPrefab, parent);
            Debug.Log($"[CardView] Created card UI: {cardGO.name}, Parent: {parent.name}");

            // Get the CardView component
            var cardView = cardGO.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.LogError($"[CardView] CardView component missing on prefab: {cardGO.name}");
                return null;
            }

            // Initialize card data
            cardView.Initialize(card);

            // Update the UI to reflect the current state of the card
            cardView.UpdateUI();

            // Override face-down state if needed (for opponent cards)
            if (faceDown != card.FaceDown)
            {
                cardView.SetFaceDown(faceDown);
            }

            // Ensure the card has required UI components
            var rectTransform = cardGO.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"[CardView] RectTransform missing on card: {cardGO.name}");
                rectTransform = cardGO.AddComponent<RectTransform>();
            }

            var image = cardGO.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError($"[CardView] Image component missing on card: {cardGO.name}");
                image = cardGO.AddComponent<Image>();
            }
            image.raycastTarget = true;

            // Set up the appropriate drag handlers
            bool isInHand = !cardView.IsCardOnBattlefield();

            // For cards in hand, add appropriate deploy drag handler based on card type
            if (isInHand)
            {
                if (card != null)
                {
                    // Disable AbilityDragHandler for hand cards
                    var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                    if (abilityDragHandler != null)
                    {
                        abilityDragHandler.enabled = false;
                    }

                    // Add appropriate drag handler based on card type
                    if (card.IsUnitCard)
                    {
                        var unitDragHandler = cardGO.GetComponent<UnitDeployDragHandler>();
                        if (unitDragHandler == null)
                        {
                            unitDragHandler = cardGO.AddComponent<UnitDeployDragHandler>();
                            Debug.Log($"[CardView] Added UnitDeployDragHandler to {card.Title}");
                        }
                    }
                    else if (card.IsOrderCard)
                    {
                        var orderDragHandler = cardGO.GetComponent<OrderDeployDragHandler>();
                        if (orderDragHandler == null)
                        {
                            orderDragHandler = cardGO.AddComponent<OrderDeployDragHandler>();
                            Debug.Log($"[CardView] Added OrderDeployDragHandler to {card.Title}");
                        }
                    }
                }
            }
            // For cards on battlefield, add AbilityDragHandler if it doesn't exist
            else
            {
                var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                if (abilityDragHandler == null)
                {
                    abilityDragHandler = cardGO.AddComponent<AbilityDragHandler>();
                    Debug.Log($"[CardView] Added AbilityDragHandler to {card.Title}");
                }

                // Make sure any deploy handlers are disabled
                var unitDragHandler = cardGO.GetComponent<UnitDeployDragHandler>();
                if (unitDragHandler != null)
                {
                    unitDragHandler.enabled = false;
                }

                var orderDragHandler = cardGO.GetComponent<OrderDeployDragHandler>();
                if (orderDragHandler != null)
                {
                    orderDragHandler.enabled = false;
                }
            }

            return cardView;
        }

        /// <summary>
        /// Plays a death animation and destroys the card GameObject when complete
        /// </summary>
        public void DieWithAnimation()
        {
            // Disable any drag handlers during animation
            var dragHandlers = GetComponents<MonoBehaviour>().OfType<IDragHandler>();
            foreach (var handler in dragHandlers)
            {
                var behavior = handler as MonoBehaviour;
                if (behavior != null)
                    behavior.enabled = false;
            }

            // Get references to components
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Animation parameters
            float animationDuration = 0.8f; // Slightly longer duration for more impact

            // Ensure the card is visible above other elements during animation
            transform.SetAsLastSibling();

            // Original position for movement animation
            Vector3 originalPosition = transform.position;
            Vector3 floatUpPosition = originalPosition + new Vector3(0, 50f, 0); // Float upward

            // Create a sequence of animations
            Sequence deathSequence = DOTween.Sequence();

            // First slightly scale up the card for dramatic effect
            deathSequence.Append(transform.DOScale(transform.localScale * 1.2f, 0.2f).SetEase(Ease.OutBack));

            // Then start the main death animation

            // Add movement animation (float upward)
            deathSequence.Append(transform.DOMove(floatUpPosition, animationDuration * 0.8f).SetEase(Ease.OutQuad));

            // Add scale animation (shrink to nothing)
            deathSequence.Join(transform.DOScale(Vector3.zero, animationDuration * 0.7f).SetEase(Ease.InBack).SetDelay(0.1f));

            // Add rotation animation (spin as it shrinks)
            deathSequence.Join(transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-90, 90)), animationDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad));

            // Add fade out animation
            deathSequence.Join(canvasGroup.DOFade(0, animationDuration * 0.6f).SetEase(Ease.OutQuad).SetDelay(0.2f));

            // Destroy the card when the animation completes
            deathSequence.OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}
