using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI
{
    using Card = Kardx.Core.Card;
    using CardType = Kardx.Core.CardType;

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

            // Add a Canvas Group to manage interactivity
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;


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
                // First make the panel active so we can initialize properly
                gameObject.SetActive(true);

                // Initialize the card data
                cardView.Initialize(cardType);

                // Force the card to load its image immediately
                if (cardView.GetComponent<Image>() != null)
                {
                    cardView.GetComponent<Image>().enabled = true;
                }

                // Make sure all image components are enabled
                EnsureCardViewIsConfigured();

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
                // First make the panel active so we can initialize properly
                gameObject.SetActive(true);

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
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("[CardDetailView] Background clicked, hiding panel");
            Hide();
        }
    }
}
