using System;
using System.IO;
using System.Linq;
using Kardx.Core;
using Kardx.Core.Data.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components.Card
{
    using Card = Kardx.Core.Data.Cards.Card; // Alias for Card

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
        private Image frameImage;

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

        [Header("References")]
        [SerializeField]
        private CardDetailView cardDetailView;

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
                var cardData = card?.CardType ?? cardType;
                if (cardData == null)
                {
                    Debug.LogError("[CardView] Both card.CardType and cardType are null");
                    return;
                }

                // Update UI elements safely with null checks on both the UI element and the data
                if (nameText != null)
                    nameText.text = card?.Name ?? cardData.Name ?? "";
                if (descriptionText != null)
                    descriptionText.text = card?.Description ?? cardData.Description ?? "";
                if (deploymentCostText != null)
                    deploymentCostText.text = (card?.DeploymentCost ?? cardData.DeploymentCost).ToString();
                if (operationCostText != null)
                    operationCostText.text = (card?.OperationCost ?? cardData.OperationCost).ToString();
                if (attackText != null)
                    attackText.text = (card?.Attack ?? cardData.BaseAttack).ToString();
                if (defenceText != null)
                    defenceText.text = (card?.CurrentDefence ?? cardData.BaseDefence).ToString();

                // Handle abilities safely
                var abilities = card?.CardType?.Abilities ?? cardData.Abilities;
                if (abilities?.Count > 0 && abilities[0] != null)
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

                // These methods have their own null checks and error handling
                UpdateCardFrame();
                if (card != null)
                {
                    UpdateModifierEffects();
                }

                LoadCardImage();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardView] Error updating card view: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Don't rethrow - we want to keep the card partially functional even if some parts fail
            }
        }

        private void UpdateCardFrame()
        {
            if (frameImage == null) return;

            var category =
                card != null ? card.CardType?.Category : cardType?.Category ?? CardCategory.Unit;

            try 
            {
                switch (category)
                {
                    case CardCategory.Unit:
                        // Set frame for Unit
                        break;
                    case CardCategory.Order:
                        // Set frame for Order
                        break;
                    case CardCategory.Countermeasure:
                        // Set frame for Countermeasure
                        break;
                    case CardCategory.Headquarter:
                        // Set frame for Headquarter
                        break;
                    default:
                        Debug.LogWarning($"[CardView] Unknown card category: {category}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardView] Error updating card frame: {ex.Message}");
                // Don't rethrow - this is not critical
            }
        }

        private void UpdateModifierEffects()
        {
            if (card?.Modifiers == null) return;
            
            foreach (var modifier in card.Modifiers)
            {
                if (modifier == null) continue;
                // Handle modifier effects here
            }
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
                Debug.LogWarning($"[CardView] No image URL specified for card {cardType.Name}");
                return;
            }

            try
            {
                // Keep the full filename including extension for WebP files
                string fileName = Path.GetFileName(imageUrl);
                Debug.Log($"[CardView] Loading image for {cardType.Name} from: {fileName}");

                // Load sprite from Cards folder
                Sprite sprite = Resources.Load<Sprite>($"Cards/{fileName}");

                if (sprite != null)
                {
                    cardImage.sprite = sprite;
                    cardImage.preserveAspect = true;
                    Debug.Log($"[CardView] Successfully loaded image for {cardType.Name}");
                }
                else
                {
                    Debug.LogError(
                        $"[CardView] Failed to load sprite for {cardType.Name}. "
                            + $"Tried path: Resources/Cards/{fileName}"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardView] Error loading card image: {ex.Message}");
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
            cardDetailView = FindObjectOfType<CardDetailView>(true);

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

        private void HandleDragStarted()
        {
            if (!isDraggable)
                return;
            transform.SetAsLastSibling();
        }

        private void HandleDragEnded(bool wasSuccessful)
        {
            if (!wasSuccessful) { }
        }
    }
}
