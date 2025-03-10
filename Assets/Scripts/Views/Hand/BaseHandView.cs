using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kardx.Models.Cards;
using Kardx.Views.Cards;
using Kardx.Models.Match;
using System.Linq;
using Kardx.Controllers.DragHandlers;

namespace Kardx.Views.Hand
{
    /// <summary>
    /// Base class for hand views that manages the display of cards in a hand
    /// </summary>
    public abstract class BaseHandView : MonoBehaviour
    {
        [SerializeField]
        protected float cardSpacing = 10f;

        [SerializeField]
        protected float maxHandWidth = 500f;

        [SerializeField]
        protected Vector3 rotationAngle = new Vector3(0, 0, 5f);

        // References
        protected MatchManager matchManager;
        protected Transform handTransform;

        [SerializeField]
        protected GameObject cardPrefab;

        /// <summary>
        /// Initialize with the match manager
        /// </summary>
        public virtual void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;
            handTransform = transform;

            if (matchManager == null)
            {
                Debug.LogError("[BaseHandView] MatchManager is null");
                return;
            }

            // Subscribe to card events
            matchManager.OnCardDrawn += HandleCardDrawn;
            matchManager.OnCardDeployed += HandleCardDeployed;
            matchManager.OnCardDiscarded += HandleCardDiscarded;
        }

        /// <summary>
        /// Clean up event subscriptions
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (matchManager != null)
            {
                matchManager.OnCardDrawn -= HandleCardDrawn;
                matchManager.OnCardDeployed -= HandleCardDeployed;
                matchManager.OnCardDiscarded -= HandleCardDiscarded;
            }
        }

        /// <summary>
        /// Handle card drawn event
        /// </summary>
        protected abstract void HandleCardDrawn(Card card);

        /// <summary>
        /// Handle card deployed event
        /// </summary>
        protected virtual void HandleCardDeployed(Card card, int slotIndex)
        {
            // Remove the card from the hand if it's in this hand
            RemoveCardFromHand(card);
        }

        /// <summary>
        /// Handle card discarded event
        /// </summary>
        protected virtual void HandleCardDiscarded(Card card)
        {
            // Remove the card from the hand if it's in this hand
            RemoveCardFromHand(card);
        }

        /// <summary>
        /// Adds a card to the hand
        /// </summary>
        public abstract void AddCardToHand(Card card);

        /// <summary>
        /// Removes a card from the hand
        /// </summary>
        public virtual void RemoveCardFromHand(Card card)
        {
            if (card == null)
            {
                Debug.LogWarning("[BaseHandView] Cannot remove null card from hand");
                return;
            }

            // Find the card view in the hand
            CardView cardView = GetCardViewInHand(card);
            if (cardView != null)
            {
                // Remove the card view from the hand
                if (matchManager != null && matchManager.ViewManager != null)
                {
                    matchManager.ViewManager.DestroyCardView(card);
                }
                else
                {
                    Destroy(cardView.gameObject);
                }

                // Re-arrange the cards in the hand
                ArrangeCards();
            }
        }

        /// <summary>
        /// Check if a card is already visually in the hand
        /// </summary>
        protected virtual bool IsCardInHand(Card card)
        {
            CardView existingView = matchManager.ViewRegistry.GetCardView(card);
            return existingView != null && existingView.transform.parent == handTransform;
        }

        /// <summary>
        /// Creates a card UI for a card in the hand
        /// </summary>
        /// <param name="card">The card to create UI for</param>
        /// <param name="faceDown">Whether the card should be face down</param>
        /// <returns>The created card view</returns>
        protected virtual CardView CreateCardUI(Card card, bool faceDown = false)
        {
            // Check if a card view already exists in the registry
            CardView existingView = matchManager.ViewRegistry.GetCardView(card);

            if (existingView != null)
            {
                Debug.Log($"[BaseHandView] Using existing view for card {card.Title}");

                // Move the existing view to this hand
                existingView.transform.SetParent(handTransform);
                existingView.transform.localPosition = Vector3.zero;
                existingView.transform.localRotation = Quaternion.identity;
                existingView.transform.localScale = Vector3.one;

                // Make sure it's visible
                existingView.SetFaceDown(faceDown);

                return existingView;
            }

            // No existing view, create a new one
            if (cardPrefab == null)
            {
                Debug.LogError("[BaseHandView] Card prefab is null");
                return null;
            }

            // Let the ViewManager create and register the card view
            if (matchManager != null && matchManager.ViewManager != null)
            {
                // Get the card view prefab component
                CardView cardViewPrefab = cardPrefab.GetComponent<CardView>();
                if (cardViewPrefab == null)
                {
                    Debug.LogError("[BaseHandView] Card prefab does not have a CardView component");
                    return null;
                }

                // Create the card view
                CardView newView = matchManager.CreateCardView(card, handTransform, cardViewPrefab);
                if (newView != null)
                {
                    newView.SetFaceDown(faceDown);
                }
                return newView;
            }

            Debug.LogError("[BaseHandView] MatchManager or ViewManager is null");
            return null;
        }

        /// <summary>
        /// Arranges the cards in the hand in a visually appealing way
        /// </summary>
        public virtual void ArrangeCards()
        {
            // Get all card views in the hand
            List<CardView> cardViews = GetCardViewsInHand();
            int cardCount = cardViews.Count;

            if (cardCount == 0)
                return;

            // Calculate the total width needed for all cards with spacing
            float cardWidth = cardPrefab != null ? cardPrefab.GetComponent<RectTransform>().rect.width : 100f;
            float totalWidth = (cardWidth * cardCount) + (cardSpacing * (cardCount - 1));

            // Adjust spacing if the total width exceeds the max hand width
            float actualSpacing = cardSpacing;
            if (totalWidth > maxHandWidth)
            {
                float availableSpace = maxHandWidth - (cardWidth * cardCount);
                actualSpacing = availableSpace / (cardCount - 1);
            }

            // Calculate the starting position (left-most card)
            float startX = -(totalWidth / 2) + (cardWidth / 2);

            // Position each card
            for (int i = 0; i < cardCount; i++)
            {
                CardView cardView = cardViews[i];
                if (cardView == null)
                    continue;

                // Calculate position
                float xPos = startX + (i * (cardWidth + actualSpacing));
                Vector3 position = new Vector3(xPos, 0, 0);

                // Calculate rotation (fan effect)
                float rotationFactor = 0f;
                if (cardCount > 1)
                {
                    rotationFactor = (i - (cardCount - 1) / 2f) / (cardCount - 1);
                }
                Vector3 rotation = rotationAngle * rotationFactor;

                // Apply position and rotation
                cardView.transform.localPosition = position;
                cardView.transform.localRotation = Quaternion.Euler(rotation);
            }
        }

        /// <summary>
        /// Gets all card views currently in the hand
        /// </summary>
        protected virtual List<CardView> GetCardViewsInHand()
        {
            List<CardView> cardViews = new List<CardView>();
            
            // Get all CardView components that are children of the hand transform
            CardView[] childCardViews = handTransform.GetComponentsInChildren<CardView>();
            cardViews.AddRange(childCardViews);

            return cardViews;
        }

        /// <summary>
        /// Gets a specific card view in the hand
        /// </summary>
        protected virtual CardView GetCardViewInHand(Card card)
        {
            if (card == null)
                return null;

            // Get the card view from the registry
            CardView cardView = matchManager.ViewRegistry.GetCardView(card);
            
            // Check if it's in this hand
            if (cardView != null && cardView.transform.parent == handTransform)
            {
                return cardView;
            }

            return null;
        }

        /// <summary>
        /// Gets all cards currently in the hand
        /// </summary>
        public virtual List<Card> GetCardsInHand()
        {
            List<Card> cards = new List<Card>();
            
            // Get all card views in the hand
            List<CardView> cardViews = GetCardViewsInHand();
            
            // Get the card model from each card view
            foreach (CardView cardView in cardViews)
            {
                if (cardView.Card != null)
                {
                    cards.Add(cardView.Card);
                }
            }

            return cards;
        }

        /// <summary>
        /// Gets the transform that contains all the cards in the hand
        /// </summary>
        public Transform GetHandTransform()
        {
            return handTransform;
        }

        /// <summary>
        /// Gets all CardView components in the hand
        /// </summary>
        public CardView[] GetCardViews()
        {
            return handTransform.GetComponentsInChildren<CardView>();
        }

        /// <summary>
        /// Updates the hand to reflect the current state of the model
        /// </summary>
        public virtual void UpdateHand()
        {
            if (matchManager == null)
            {
                Debug.LogWarning("[BaseHandView] Cannot update hand: MatchManager is null");
                return;
            }

            // Re-arrange the cards in the hand
            ArrangeCards();
        }
    }
}
