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

        private void Awake()
        {
            // Ensure we have the required components
            if (cardView == null)
            {
                cardView = GetComponentInChildren<CardView>();
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

            Debug.Log("[CardDetailView] Initialized with background panel and raycast blocking");

            // Make sure the card view is properly configured
            EnsureCardViewIsConfigured();
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
