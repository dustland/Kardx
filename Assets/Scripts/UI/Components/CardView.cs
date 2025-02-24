using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kardx.Core;

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

        [Header("UI Elements")]
        [SerializeField]
        private Image cardImage;

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

        [SerializeField]
        private GameObject highlightEffect;

        [Header("Components")]
        [SerializeField]
        private CardDragHandler dragHandler;

        [Header("Animation")]
        [SerializeField]
        private Animator animator;

        private bool isDraggable = true;
        private bool isDragging = false; // Track if we're currently dragging

        public Card Card => card;
        public CardType CardType => cardType;

        private void Awake()
        {
            dragHandler = GetComponent<CardDragHandler>();
            if (dragHandler != null)
            {
                dragHandler.OnDragStarted += () => isDragging = true;
                dragHandler.OnDragEnded += (success) =>
                {
                    isDragging = false;
                    // Small delay to ensure click doesn't fire immediately after drag
                    if (success)
                    {
                        CancelInvoke(nameof(ShowDetail));
                    }
                };
            }
        }

        private void Start()
        {
            if (dragHandler == null)
            {
                dragHandler = GetComponent<CardDragHandler>();
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
            if (cardImage == null || cardType == null)
            {
                Debug.LogWarning("[CardView] Card image component or card type is missing");
                return;
            }

            string imageUrl = cardType.ImageUrl;
            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.LogWarning($"[CardView] No image URL specified for card {cardType.Title}");
                return;
            }

            try
            {
                // Remove file extension as Unity adds its own
                string fileName = Path.GetFileNameWithoutExtension(imageUrl);
                Debug.Log($"[CardView] Loading image for {cardType.Title} from: Cards/{fileName}");

                // Load sprite from Cards folder
                Sprite sprite = Resources.Load<Sprite>($"Cards/{fileName}");

                if (sprite != null)
                {
                    cardImage.sprite = sprite;
                    cardImage.preserveAspect = true;
                    Debug.Log($"[CardView] Successfully loaded image for {cardType.Title}");
                }
                else
                {
                    Debug.LogError($"[CardView] Failed to load image for {cardType.Title} from Cards/{fileName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CardView] Error loading image for {cardType.Title}: {e.Message}");
            }
        }

        public void PlayDeployAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("Deploy");
            }
        }

        public void PlayAttackAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }

        public void PlayDamageAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("TakeDamage");
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
            this.card = card;
            this.cardType = null;
            SetDraggable(true);
            UpdateCardView();
        }

        public void Initialize(CardType cardType)
        {
            this.card = null;
            this.cardType = cardType;
            SetDraggable(false); // CardType view should not be draggable
            UpdateCardView();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("OnPointerClick (isDragging: " + isDragging + ")");
            if (!isDragging)
            {
                // Add a small delay to prevent accidental clicks during drag
                Invoke(nameof(ShowDetail), 0.1f);
            }
        }

        public void ShowDetail()
        {
            Debug.Log("[CardView] Showing card detail view" + (isDragging ? " (dragging)" : ""));
            if (isDragging)
            {
                Debug.Log("[CardView] is dragging, not showing detail");
                return; // Don't show detail if we're dragging
            }
            // Find CardDetailView even if inactive
            var cardDetailView = FindObjectOfType<CardDetailView>(true);

            if (cardDetailView != null)
            {
                Debug.Log("[CardView] Found card detail view");
                if (card != null)
                {
                    cardDetailView.Show(card);
                }
                else if (cardType != null)
                {
                    cardDetailView.Show(cardType);
                }
            }
            else
            {
                Debug.LogWarning("[CardView] No CardDetailView found in scene");
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
