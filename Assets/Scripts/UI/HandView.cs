using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Core;

namespace Kardx.UI
{
    /// <summary>
    /// Manages a player or opponent's hand visualization including hand updates and card creation.
    /// Can be used for both player and opponent hands by setting the isOpponentHand flag.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField]
        private GameObject cardPrefab;

        [SerializeField]
        [Tooltip("Set to true if this is the opponent's hand")]
        private bool isOpponentHand = false;

        [Tooltip("Reference to the MatchManager - assigned at runtime during initialization")]
        private MatchManager matchManager;

        private Player player;
        private Transform handTransform;

        /// <summary>
        /// Initializes the hand view with the match manager reference
        /// </summary>
        public void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;
            this.handTransform = transform;

            // Determine which player this hand belongs to
            if (isOpponentHand)
            {
                player = matchManager.Opponent;
            }
            else
            {
                player = matchManager.Player;
            }

            if (cardPrefab == null)
            {
                Debug.LogError($"[HandView] Card prefab is not assigned. Please assign it in the Unity Editor.");
            }
        }

        /// <summary>
        /// Updates the visual representation of the hand
        /// </summary>
        public void UpdateHand()
        {
            if (player == null || handTransform == null)
                return;

            bool faceDown = isOpponentHand;

            // Clear existing cards
            foreach (Transform child in handTransform)
            {
                Destroy(child.gameObject);
            }

            // Add cards from player's hand
            foreach (var card in player.Hand.GetCards())
            {
                var cardGO = CreateCardUI(card, faceDown);

                // Add appropriate drag handlers based on card type 
                // Only add to player's hand, not opponent
                if (!faceDown && !isOpponentHand)
                {
                    var cardView = cardGO.GetComponent<CardView>();
                    if (cardView != null)
                    {
                        if (card.IsUnitCard)
                        {
                            if (cardGO.GetComponent<UnitDeployDragHandler>() == null)
                            {
                                cardGO.AddComponent<UnitDeployDragHandler>();
                            }
                        }
                        else if (card.IsOrderCard)
                        {
                            if (cardGO.GetComponent<OrderDeployDragHandler>() == null)
                            {
                                cardGO.AddComponent<OrderDeployDragHandler>();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the hand with a specific hand object
        /// </summary>
        public void UpdateHand(Hand hand)
        {
            if (hand == null || handTransform == null)
                return;

            bool faceDown = isOpponentHand;

            // Clear existing cards
            foreach (Transform child in handTransform)
            {
                Destroy(child.gameObject);
            }

            // Add cards from hand
            foreach (var card in hand.GetCards())
            {
                var cardGO = CreateCardUI(card, faceDown);

                // Add appropriate drag handlers based on card type
                // Only add to player's hand, not opponent
                if (!faceDown && !isOpponentHand)
                {
                    var cardView = cardGO.GetComponent<CardView>();
                    if (cardView != null)
                    {
                        if (card.IsUnitCard)
                        {
                            if (cardGO.GetComponent<UnitDeployDragHandler>() == null)
                            {
                                cardGO.AddComponent<UnitDeployDragHandler>();
                            }
                        }
                        else if (card.IsOrderCard)
                        {
                            if (cardGO.GetComponent<OrderDeployDragHandler>() == null)
                            {
                                cardGO.AddComponent<OrderDeployDragHandler>();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a single card to the hand visualization without recreating the entire hand.
        /// More efficient than UpdateHand when just adding one card.
        /// </summary>
        /// <param name="card">The card to add to the hand visualization</param>
        public void AddCardToHand(Card card)
        {
            if (card == null || handTransform == null)
                return;

            bool faceDown = isOpponentHand;

            // Create and add just the new card
            var cardGO = CreateCardUI(card, faceDown);

            // Add appropriate drag handlers based on card type 
            // Only add to player's hand, not opponent
            if (!faceDown && !isOpponentHand)
            {
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    if (card.IsUnitCard)
                    {
                        if (cardGO.GetComponent<UnitDeployDragHandler>() == null)
                        {
                            cardGO.AddComponent<UnitDeployDragHandler>();
                        }
                    }
                    else if (card.IsOrderCard)
                    {
                        if (cardGO.GetComponent<OrderDeployDragHandler>() == null)
                        {
                            cardGO.AddComponent<OrderDeployDragHandler>();
                        }
                    }
                }
            }

            Debug.Log($"[HandView] Added card {card.Title} to hand visualization");
        }

        /// <summary>
        /// Creates a card UI element
        /// </summary>
        public GameObject CreateCardUI(Card card, bool faceDown = false)
        {
            if (card == null || cardPrefab == null)
                return null;

            var cardGO = Instantiate(cardPrefab, handTransform);
            cardGO.name = $"Card_{card.Title}";

            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.Initialize(card, faceDown);
            }
            else
            {
                Debug.LogError($"[HandView] CardView component not found on card prefab. Please add this component in the Unity Editor.");
                Destroy(cardGO);
                return null;
            }

            return cardGO;
        }

        /// <summary>
        /// Handles the UI visualization when an order card is deployed from this hand
        /// This is a UI-only method and should be called AFTER game state has been updated
        /// </summary>
        /// <param name="card">The card that was deployed</param>
        /// <param name="success">Whether the deployment was successful (affects UI behavior)</param>
        /// <param name="cardGameObject">The GameObject representing the card being deployed</param>
        public void HandleOrderCardDeployed(Card card, bool success, GameObject cardGameObject)
        {
            if (card == null || isOpponentHand)
                return;

            if (success)
            {
                // The card will be removed from the hand by the game model
                // Just destroy this GameObject or handle visual effects
                if (cardGameObject != null)
                {
                    Destroy(cardGameObject);
                    Debug.Log($"[HandView] Removed card UI for deployed order card: {card.Title}");
                }
            }
            else
            {
                Debug.Log($"[HandView] Failed to deploy order card: {card.Title}");
                // Card deployment failed, so we need to update the hand to ensure card is shown
                UpdateHand();
            }
        }

        /// <summary>
        /// Clears any highlights on the hand cards or other UI elements
        /// </summary>
        public void ClearHighlights()
        {
            // Clear any highlights on the hand that might be active
            Debug.Log("[HandView] Clearing highlights");

            // If more specific highlight clearing is needed, add it here
        }
    }
}
