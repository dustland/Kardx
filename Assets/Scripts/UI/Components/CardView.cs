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
            { Faction.BritishEmpire, "CardBacks/british_back" },
            { Faction.ThirdReich, "CardBacks/german_back" },
            { Faction.Empire, "CardBacks/japan_back" },
            { Faction.Neutral, "CardBacks/neutral_back" },
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

            // Only handle the click if this GameObject is the actual target
            if (eventData.pointerPress != gameObject)
            {
                Debug.Log("[CardView] Ignoring click as it's not directly on this card");
                return;
            }

            if (!isDragging)
            {
                ShowDetail();
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

                // Check if card is face down first
                if (card != null && card.FaceDown)
                {
                    ShowCardBack();
                    return;
                }

                // Hide card back overlay when face up
                if (cardBackOverlay != null)
                {
                    cardBackOverlay.gameObject.SetActive(false);
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
                var abilities = card != null ? card.CardType.Abilities : cardType?.Abilities;

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

                LoadCardImage();
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
            string imageUrl = card != null ? card.ImageUrl : cardType.ImageUrl;
            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.LogWarning($"[CardView] No image URL specified for card {cardType.Title}");
                return;
            }

            try
            {
                // Remove file extension as Unity adds its own
                string fileName = Path.GetFileNameWithoutExtension(imageUrl);
                Debug.Log($"[CardView] Loading image for {imageUrl}");

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
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardView] Error loading image for {imageUrl}: {e.Message}");
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
        }

        public void Initialize(Card card)
        {
            Debug.Log("[CardView] Initializing card: " + card.Title);
            this.card = card;
            this.cardType = null;
            SetDraggable(true);
            UpdateCardView();
        }

        public void Initialize(CardType cardType)
        {
            Debug.Log("[CardView] Initializing cardType: " + cardType.Title);
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

        private void ShowCardBack()
        {
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
                    Debug.LogWarning(
                        $"[CardView] Failed to load card back sprite for faction {card.OwnerFaction} at path: {cardBackPath}"
                    );
                    // Load neutral back as fallback
                    cardBackOverlay.sprite = Resources.Load<Sprite>(
                        FactionCardBacks[Faction.Neutral]
                    );
                    cardBackOverlay.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning(
                    "[CardView] Missing card back overlay image component or card is null"
                );
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

        // Add this method to test if the GameObject is properly set up for UI interaction
        private void OnEnable()
        {
            Debug.Log(
                $"[CardView] Card enabled: {gameObject.name}. "
                    + $"Active in hierarchy: {gameObject.activeInHierarchy}, "
                    + $"Layer: {gameObject.layer}, "
                    + $"Canvas: {GetComponentInParent<Canvas>()?.name ?? "None"}"
            );
        }
    }
}
