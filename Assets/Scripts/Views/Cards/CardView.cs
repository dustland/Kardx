using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;
using Kardx.Models.Cards;
using Kardx.Utils;
using Kardx.Models;
using Kardx.Controllers.DragHandlers;
using Kardx.Managers;
using Kardx.Models.Match;

namespace Kardx.Views.Cards
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
        private TextMeshProUGUI ownerText;

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

        private CardDragController dragController;
        private CanvasGroup canvasGroup;
        private bool isHighlighted = false;

        // Event fired when this card view is destroyed
        public event Action OnDestroyed;

        // Reference to the ViewManager
        private ViewManager viewManager;

        public Card Card => card;
        public CardType CardType => cardType;

        /// <summary>
        /// Sets the ViewManager reference for this card view
        /// </summary>
        public void SetViewManager(ViewManager viewManager)
        {
            this.viewManager = viewManager;
        }

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

            dragController = GetComponent<CardDragController>();
            if (dragController == null)
                dragController = gameObject.AddComponent<CardDragController>();

            dragController.OnDragStarted += () => isBeingDragged = true;
            dragController.OnDragEnded += (success) =>
            {
                isBeingDragged = false;
                if (success)
                    CancelInvoke(nameof(ShowDetail));
            };

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
            bool isInBattlefield = IsOnBattlefield();
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
                var owner = card != null ? card.Owner.Id : string.Empty;

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

                if (ownerText != null)
                    ownerText.text = owner;

                UpdateCardFrame();
                if (card != null)
                {
                    UpdateModifierEffects();

                    // Update draggable state based on card location
                    UpdateInteractivity();
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

        /// <summary>
        /// Updates the card's interaction behavior based on its current state and location
        /// </summary>
        public void UpdateInteractivity()
        {
            if (dragController == null)
                dragController = GetComponent<CardDragController>();

            if (dragController != null)
                dragController.RefreshCapability();
            else if (backgroundImage != null)
                backgroundImage.raycastTarget = false;
        }

        /// <summary>
        /// Checks if this card is on the battlefield
        /// </summary>
        private bool IsOnBattlefield()
        {
            if (card == null)
                return false;

            // Check if the card is on the battlefield by checking the model
            if (card.Owner != null)
            {
                // Check if this card is in the owner's battlefield
                return card.Owner.Battlefield.Contains(card);
            }

            return false;
        }

        /// <summary>
        /// Checks if this card is in a player's hand
        /// </summary>
        private bool IsInHand()
        {
            if (card == null)
                return false;

            // Check if the card is in the hand by checking the model
            if (card.Owner != null)
            {
                // Check if this card is in the owner's hand
                return card.Owner.Hand.Contains(card);
            }

            return false;
        }

        public void ResetDraggingState()
        {
            isBeingDragged = false;
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
            UpdateInteractivity(); // CardType view should not be draggable
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

            ShowDetailView();
        }

        /// <summary>
        /// Shows the card detail view for this card
        /// </summary>
        public void ShowDetailView()
        {
            // Get the CardDetailView from the ViewManager
            if (viewManager != null)
            {
                CardDetailView detailView = viewManager.GetCardDetailView();
                if (detailView != null)
                {
                    if (card != null)
                    {
                        detailView.Show(card);
                    }
                    else if (cardType != null)
                    {
                        detailView.Show(cardType);
                    }
                }
                else
                {
                    Debug.LogWarning("[CardView] Cannot show detail view: CardDetailView not found in ViewManager");
                }
            }
            else
            {
                Debug.LogWarning("[CardView] Cannot show detail view: ViewManager reference not set");
            }
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

        /// <summary>
        /// Plays a death animation and destroys the card GameObject when complete
        /// </summary>
        public void DieWithAnimation()
        {
            // Ensure the card is visible above other elements during animation
            transform.SetAsLastSibling();

            // Get or add a CanvasGroup component for fade animation
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // First slightly scale up the card for dramatic effect
            transform.DOScale(transform.localScale * 1.2f, 0.2f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    // Then use the utility class for the main death animation
                    Sequence deathSequence = DOTweenAnimationUtility.AnimateCardDeath(
                        transform,
                        canvasGroup,
                        floatDistance: 50f,
                        duration: 0.8f,
                        onComplete: () =>
                        {
                            Destroy(gameObject);
                        }
                    );

                    // Play the sequence
                    deathSequence.Play();
                });
        }

        /// <summary>
        /// Plays an attack animation toward a target position or in a forward direction
        /// </summary>
        /// <param name="targetPosition">Position to attack toward (optional)</param>
        /// <param name="lungeDistance">How far to lunge if no target is specified</param>
        /// <param name="duration">Duration of the attack animation</param>
        /// <param name="flashColor">Color to flash during impact (null for default orange flash, Color.clear for no flash)</param>
        /// <param name="onImpactCallback">Action to execute at the moment of impact</param>
        /// <param name="playSound">Whether to play sound effect</param>
        /// <returns>The DOTween sequence for further chaining if needed</returns>
        public Sequence AttackWithAnimation(
            Vector3? targetPosition = null,
            float lungeDistance = 30f,
            float duration = 0.5f,
            Color? flashColor = null,
            Action onImpactCallback = null,
            bool playSound = true)
        {
            // Log the attack animation
            Debug.Log($"[CardView] Playing attack animation for card: {(card != null ? card.Title : "unknown")}");

            // Ensure the card is visible above other elements during animation
            transform.SetAsLastSibling();

            // Play sound effect if requested
            if (playSound)
            {
                // Try to play sound using a generic approach without direct AudioService reference
                // This maintains loose coupling with the audio system
                var audioManager = FindAnyObjectByType<MonoBehaviour>();
                if (audioManager != null)
                {
                    // Try to invoke PlaySFX method via reflection if it exists
                    var method = audioManager.GetType().GetMethod("PlaySFX");
                    if (method != null)
                    {
                        try
                        {
                            method.Invoke(audioManager, new object[] { "CardAttack" });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[CardView] Error playing attack sound: {ex.Message}");
                        }
                    }
                }
            }

            // If flashColor is null, use default orange flash
            if (flashColor == null)
            {
                flashColor = new Color(1f, 0.5f, 0f, 0.8f); // Orange flash
            }

            // Use the utility class for attack animation
            Sequence attackSequence = DOTweenAnimationUtility.AnimateCardAttack(
                transform,
                targetPosition,
                lungeDistance,
                duration,
                flashColor,
                onImpactCallback
            );

            // Play the sequence
            attackSequence.Play();

            return attackSequence;
        }

        protected virtual void OnDestroy()
        {
            // Inform listeners this view is being destroyed
            OnDestroyed?.Invoke();

            if (dragController != null)
            {
                dragController.OnDragStarted -= () => isBeingDragged = true;
                dragController.OnDragEnded -= (success) => isBeingDragged = false;
            }
        }

        /// <summary>
        /// Creates a new card UI GameObject for a card
        /// </summary>
        /// <param name="card">The card model to create UI for</param>
        /// <param name="cardPrefab">The card prefab to instantiate</param>
        /// <param name="parent">The parent transform</param>
        /// <returns>The created CardView component</returns>
        public static CardView CreateCard(Card card, GameObject cardPrefab, Transform parent)
        {
            if (card == null)
            {
                Debug.LogError("[CardView] Cannot create card UI from null card model");
                return null;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[CardView] Cannot create card UI from null prefab");
                return null;
            }

            // Instantiate the card prefab
            GameObject cardGO = Instantiate(cardPrefab, parent);
            cardGO.name = $"Card_{card.Title}";

            // Get the CardView component
            CardView cardView = cardGO.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.LogError("[CardView] Card prefab does not have a CardView component");
                Destroy(cardGO);
                return null;
            }

            // Set the card model
            cardView.Initialize(card);

            if (card.Owner != null && !card.Owner.IsOpponent && cardGO.GetComponent<CardDragController>() == null)
                cardGO.AddComponent<CardDragController>();

            // Update the card UI
            cardView.UpdateUI();
            cardView.UpdateInteractivity();

            return cardView;
        }

        /// <summary>
        /// Helper method to check if a card is in hand
        /// </summary>
        private static bool IsInHand(Card card)
        {
            return card != null && card.Owner != null && card.Owner.Hand.Contains(card);
        }

        /// <summary>
        /// Helper method to check if a card is on battlefield
        /// </summary>
        private static bool IsOnBattlefield(Card card)
        {
            return card != null && card.Owner != null && card.Owner.Battlefield.Contains(card);
        }

    }
}
