using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Kardx.Utils;
using Kardx.Models.Cards;

namespace Kardx.Views.Cards
{
    public class CardDetailView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private CardView cardView;

        [SerializeField]
        private Image backgroundPanel; // Semi-transparent background panel

        [SerializeField]
        private TextMeshProUGUI cardNameText;

        [SerializeField]
        private TextMeshProUGUI descriptionText;

        [SerializeField]
        private TextMeshProUGUI categoryText;

        [Header("Animation Settings")]
        [SerializeField]
        private float scaleInDuration = 0.4f;

        [SerializeField]
        private float scaleOutDuration = 0.25f;

        private CanvasGroup canvasGroup;
        private RectTransform cardRectTransform;
        private Vector3 originalCardScale;
        private Vector3 originalCardPosition;
        private Sequence currentAnimation;

        private void Awake()
        {
            // Ensure we have the required components
            if (cardView == null)
            {
                cardView = GetComponent<CardView>();
                Debug.Log(
                    $"[CardDetailView] Found CardView: {(cardView != null ? cardView.name : "null")}"
                );
            }

            // Use the Image component on this GameObject
            if (backgroundPanel == null)
            {
                backgroundPanel = GetComponent<Image>();
                if (backgroundPanel == null)
                {
                    Debug.LogError(
                        "[CardDetailView] No Image component found on CardPanel. Please add one."
                    );
                    return;
                }

                // Set up the background panel properties
                backgroundPanel.color = new Color(0, 0, 0, 0.95f); // Semi-transparent black
            }

            // Ensure the panel blocks raycasts
            backgroundPanel.raycastTarget = true;

            // Add a Canvas Group to manage interactivity and animations
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // Get card rect transform for animations
            if (cardView != null)
            {
                cardRectTransform = cardView.GetComponent<RectTransform>();
                if (cardRectTransform != null)
                {
                    originalCardScale = cardRectTransform.localScale;
                    originalCardPosition = cardRectTransform.anchoredPosition;
                }
            }

            // Make sure the card view is properly configured
            EnsureCardViewIsConfigured();

            // Disable raycast targets on all child graphic elements except the background panel
            DisableRaycastsOnChildren();
        }

        private void Initialize(CardType cardType)
        {
            Debug.Log("[CardDetailView] Initialized with cardType: " + cardType?.Title);

            if (cardNameText != null)
                cardNameText.text = cardType?.Title;
            if (descriptionText != null)
                descriptionText.text = cardType?.Description;
            if (categoryText != null)
                categoryText.text = cardType?.Category.ToString();
        }

        private void DisableRaycastsOnChildren()
        {
            // Get all Graphic components (Image, Text, etc.) in children
            Graphic[] graphics = GetComponentsInChildren<Graphic>();

            foreach (Graphic graphic in graphics)
            {
                // Skip the background panel itself
                if (graphic.gameObject == backgroundPanel.gameObject)
                    continue;

                // Disable raycast targeting on all other UI elements
                graphic.raycastTarget = false;
                Debug.Log($"[CardDetailView] Disabled raycast targeting on {graphic.gameObject.name}");
            }
        }

        private void EnsureCardViewIsConfigured()
        {
            if (cardView == null)
            {
                Debug.LogError("[CardDetailView] CardView is null, cannot configure");
                return;
            }

            // Make sure all image components are enabled
            var images = cardView.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                img.enabled = true;
                // Debug.Log($"[CardDetailView] Enabled image component: {img.name}");
            }

            // Make sure the card view itself is active
            cardView.gameObject.SetActive(true);

            Debug.Log("[CardDetailView] CardView configuration completed");
        }

        public void Show(CardType cardType)
        {
            Debug.Log($"[CardDetailView] Show called with cardType: {cardType?.Title}");
            if (cardView != null)
            {
                // Kill any running animations
                KillCurrentAnimation();

                // First make the panel active so we can initialize properly
                gameObject.SetActive(true);

                // Set background and canvas group immediately instead of animating
                canvasGroup.alpha = 1f;

                // Set initial card animation state for zoom/flip effect
                if (cardRectTransform != null)
                {
                    // We'll set initial scale in the animation utility
                    // Just store original scale for reference
                }

                // Initialize the card data
                cardView.Initialize(cardType);

                // Force the card to load its image immediately
                if (cardView.GetComponent<Image>() != null)
                {
                    cardView.GetComponent<Image>().enabled = true;
                }

                // Make sure all image components are enabled
                EnsureCardViewIsConfigured();

                // Reset text components alpha for animation
                SetupTextComponentsForAnimation(0f);

                // Start show animation (using zoom/flip effect)
                PlayShowAnimation();

                Debug.Log($"[CardDetailView] Panel activated for cardType: {cardType?.Title}");
            }
            else
            {
                Debug.LogError("[CardDetailView] cardView is null!");
                gameObject.SetActive(true);
            }
        }

        public void Show(Card card)
        {
            Debug.Log($"[CardDetailView] Show called with card: {card?.Title}");
            if (cardView != null)
            {
                // Kill any running animations
                KillCurrentAnimation();

                // First make the panel active so we can initialize properly
                gameObject.SetActive(true);

                // Set background and canvas group immediately instead of animating
                canvasGroup.alpha = 1f;

                // Set initial card animation state for zoom/flip effect
                if (cardRectTransform != null)
                {
                    // We'll set initial scale in the animation utility
                    // Just store original scale for reference
                }

                // Initialize the card data
                cardView.Initialize(card);
                Initialize(card?.CardType);

                // Force the card to load its image immediately
                if (cardView.GetComponent<Image>() != null)
                {
                    cardView.GetComponent<Image>().enabled = true;
                }

                // Make sure all image components are enabled
                EnsureCardViewIsConfigured();

                // Reset text components alpha for animation
                SetupTextComponentsForAnimation(0f);

                // Start show animation (using zoom/flip effect)
                PlayShowAnimation();

                Debug.Log($"[CardDetailView] Panel activated for card: {card?.Title}");
            }
            else
            {
                Debug.LogError("[CardDetailView] cardView is null!");
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            Debug.Log($"[CardDetailView] Hide");

            // Kill any running animations
            KillCurrentAnimation();

            // Start hide animation
            PlayHideAnimation();
        }

        private void SetupTextComponentsForAnimation(float alpha)
        {
            // Set initial alpha for text components
            if (cardNameText != null)
            {
                var nameGroup = cardNameText.GetComponent<CanvasGroup>();
                if (nameGroup == null)
                {
                    nameGroup = cardNameText.gameObject.AddComponent<CanvasGroup>();
                }
                nameGroup.alpha = alpha;
            }

            if (descriptionText != null)
            {
                var descGroup = descriptionText.GetComponent<CanvasGroup>();
                if (descGroup == null)
                {
                    descGroup = descriptionText.gameObject.AddComponent<CanvasGroup>();
                }
                descGroup.alpha = alpha;
            }

            if (categoryText != null)
            {
                var catGroup = categoryText.GetComponent<CanvasGroup>();
                if (catGroup == null)
                {
                    catGroup = categoryText.gameObject.AddComponent<CanvasGroup>();
                }
                catGroup.alpha = alpha;
            }
        }

        private void PlayShowAnimation()
        {
            // Kill any existing animation
            KillCurrentAnimation();

            // Initialize a new sequence
            currentAnimation = DOTween.Sequence();

            // Animate the card appearance (zoom in and optional flip effect)
            Sequence cardAnimation = DOTweenAnimationUtility.AnimateCardZoomIn(
                cardRectTransform,
                originalCardScale * 0.7f,  // Start at 70% scale
                originalCardScale,         // End at original scale
                scaleInDuration,
                0f  // Removed flip effect
            );

            if (cardAnimation != null)
            {
                currentAnimation.Append(cardAnimation);
            }

            // Collect all text elements in an array
            TextMeshProUGUI[] textElements = new TextMeshProUGUI[]
            {
                cardNameText,
                descriptionText,
                categoryText
            };

            // Animate text elements sequentially
            Sequence textAnimation = DOTweenAnimationUtility.AnimateTextElementsSequentialFadeIn(
                textElements,
                0.3f
            );

            if (textAnimation != null)
            {
                currentAnimation.Append(textAnimation);
            }

            currentAnimation.Play();
        }

        private void PlayHideAnimation()
        {
            // Kill any existing animation
            KillCurrentAnimation();

            // Initialize a new sequence
            currentAnimation = DOTween.Sequence();

            // Collect all text elements in an array (in reverse order)
            TextMeshProUGUI[] textElements = new TextMeshProUGUI[]
            {
                categoryText,
                descriptionText,
                cardNameText
            };

            // Animate text elements fading out sequentially
            Sequence textAnimation = DOTweenAnimationUtility.AnimateTextElementsSequentialFadeOut(
                textElements,
                0.15f
            );

            if (textAnimation != null)
            {
                currentAnimation.Append(textAnimation);
            }

            // Animate the card disappearance (zoom out and optional flip effect)
            Sequence cardAnimation = DOTweenAnimationUtility.AnimateCardZoomOut(
                cardRectTransform,
                originalCardScale,        // Start at original scale
                originalCardScale * 0.7f, // End at 70% scale
                scaleOutDuration,
                0f  // Removed flip effect
            );

            if (cardAnimation != null)
            {
                currentAnimation.Append(cardAnimation);
            }

            // After animation completes, deactivate the game object
            currentAnimation.OnComplete(() =>
            {
                gameObject.SetActive(false);

                // Reset rotation for next time
                if (cardRectTransform != null)
                {
                    cardRectTransform.localRotation = Quaternion.identity;
                }
            });

            currentAnimation.Play();
        }

        private void KillCurrentAnimation()
        {
            DOTweenAnimationUtility.SafeKill(currentAnimation);
            currentAnimation = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("[CardDetailView] Background clicked, hiding panel");
            Hide();
        }

        private void OnDestroy()
        {
            // Make sure to clean up any running tweens
            KillCurrentAnimation();
        }
    }
}
