using System.Collections;
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
    /// Manages the display of cards in a player's hand
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField]
        private float cardSpacing = 10f;

        [SerializeField]
        private float maxHandWidth = 500f;

        [SerializeField]
        private Vector3 rotationAngle = new Vector3(0, 0, 5f);

        // References
        private MatchManager matchManager;
        private Transform handTransform;

        [SerializeField]
        private GameObject cardPrefab;

        /// <summary>
        /// Initializes the hand view with a reference to the match manager
        /// </summary>
        public void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;
            this.handTransform = transform;

            // No event subscriptions needed - ViewManager handles synchronization
        }

        /// <summary>
        /// Get the transform that holds the hand cards
        /// </summary>
        public Transform GetHandTransform()
        {
            return handTransform;
        }

        /// <summary>
        /// Add a card to the hand
        /// </summary>
        public void AddCardToHand(Card card)
        {
            if (card == null)
            {
                Debug.LogError("[HandView] Cannot add null card");
                return;
            }

            // Check if this card already exists in the hand
            if (IsCardInHand(card))
            {
                Debug.Log($"[HandView] Card {card.Title} is already in hand");
                return;
            }

            // Create UI for the card
            bool faceDown = card.Owner != matchManager.Player;
            CardView cardView = CreateCardUI(card, faceDown);

            if (cardView != null)
            {
                // Set up the card view for its new location
                if (card.Owner == matchManager.Player)
                {
                    // Add proper drag handlers based on card type
                    if (card.IsUnitCard)
                    {
                        if (cardView.GetComponent<UnitDeployDragHandler>() == null)
                        {
                            cardView.gameObject.AddComponent<UnitDeployDragHandler>();
                        }
                    }
                    else if (card.IsOrderCard)
                    {
                        if (cardView.GetComponent<OrderDeployDragHandler>() == null)
                        {
                            cardView.gameObject.AddComponent<OrderDeployDragHandler>();
                        }
                    }
                }
                else
                {
                    // Opponent cards aren't draggable
                    cardView.SetDraggable(false);
                }

                Debug.Log($"[HandView] Added card {card.Title} to hand");

                // Re-arrange the cards in the hand
                ArrangeCards();
            }
        }

        /// <summary>
        /// Check if a card is already visually in the hand
        /// </summary>
        private bool IsCardInHand(Card card)
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
        private CardView CreateCardUI(Card card, bool faceDown = false)
        {
            // Check if a card view already exists in the registry
            CardView existingView = matchManager.ViewRegistry.GetCardView(card);

            if (existingView != null)
            {
                Debug.Log($"[HandView] Using existing view for card {card.Title}");

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
                Debug.LogError("[HandView] Card prefab is null");
                return null;
            }

            // Let the ViewManager create and register the card view
            if (matchManager != null && matchManager.ViewManager != null)
            {
                // Get the card view prefab component
                CardView cardViewPrefab = cardPrefab.GetComponent<CardView>();
                if (cardViewPrefab == null)
                {
                    Debug.LogError("[HandView] Card prefab does not have a CardView component");
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

            Debug.LogError("[HandView] MatchManager or ViewManager is null");
            return null;
        }

        /// <summary>
        /// Arranges the cards in the hand in a visually appealing way
        /// </summary>
        public void ArrangeCards()
        {
            if (handTransform == null) return;

            // Get all card views in the hand
            List<CardView> cardViews = new List<CardView>();
            foreach (Transform child in handTransform)
            {
                CardView cardView = child.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardViews.Add(cardView);
                }
            }

            int cardCount = cardViews.Count;
            if (cardCount == 0) return;

            // Calculate the total width needed
            float cardWidth = cardViews[0].GetComponent<RectTransform>().rect.width;
            float totalWidth = cardCount * cardWidth + (cardCount - 1) * cardSpacing;

            // Adjust spacing if the hand is too wide
            float actualSpacing = cardSpacing;
            if (totalWidth > maxHandWidth)
            {
                float availableSpace = maxHandWidth - cardCount * cardWidth;
                actualSpacing = availableSpace / (cardCount - 1);
                if (actualSpacing < 0) actualSpacing = 0;
                totalWidth = maxHandWidth;
            }

            // Position each card
            float startX = -totalWidth / 2 + cardWidth / 2;
            for (int i = 0; i < cardCount; i++)
            {
                CardView cardView = cardViews[i];

                // Calculate position
                float xPos = startX + i * (cardWidth + actualSpacing);
                cardView.transform.localPosition = new Vector3(xPos, 0, 0);

                // Apply a slight rotation for visual appeal
                float rotationFactor = (xPos / (totalWidth / 2)) * rotationAngle.z;
                cardView.transform.localRotation = Quaternion.Euler(0, 0, rotationFactor);

                // Ensure proper scale
                cardView.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Gets all cards currently in the hand
        /// </summary>
        public List<Card> GetCardsInHand()
        {
            List<Card> cards = new List<Card>();

            foreach (Transform child in handTransform)
            {
                CardView cardView = child.GetComponent<CardView>();
                if (cardView != null && cardView.Card != null)
                {
                    cards.Add(cardView.Card);
                }
            }

            return cards;
        }

        /// <summary>
        /// Gets all card views currently in this hand
        /// </summary>
        /// <returns>Array of CardView components in this hand</returns>
        public CardView[] GetCardViews()
        {
            return handTransform.GetComponentsInChildren<CardView>(includeInactive: false);
        }

        /// <summary>
        /// Updates the visual representation of the hand from model
        /// Keeping for backward compatibility
        /// </summary>
        public void UpdateHand()
        {
            ArrangeCards();
        }
    }
}
