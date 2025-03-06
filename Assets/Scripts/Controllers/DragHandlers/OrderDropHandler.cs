using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Kardx.Utils;
using Kardx.Views.Match;
using Kardx.Models;
using Kardx.Views.Cards;
using Kardx.Models.Cards;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Handles the dropping of Order cards onto the battlefield.
    /// This component should be attached to a GameObject that covers the entire battlefield area.
    /// </summary>
    public class OrderDropHandler : MonoBehaviour, IDropHandler
    {
        private MatchView matchView;

        [SerializeField]
        private float heartbeatDuration = 0.5f;

        [SerializeField]
        private float heartbeatScale = 1.15f;

        [SerializeField]
        private int heartbeatPulses = 2;

        [SerializeField]
        private Color heartbeatColor = new Color(0.8f, 0.2f, 0.2f, 0.5f); // Reddish glow

        private void Awake()
        {
            // Just store the matchView reference
            matchView = GetComponentInParent<MatchView>();

            if (matchView == null)
            {
                Debug.LogWarning("[OrderDropHandler] Could not find MatchView.");
            }

            // Ensure this GameObject can receive drops
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("[OrderDropHandler] OnDrop called");

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.Log("[OrderDropHandler] No CardView found on dropped object");
                return;
            }

            // Ensure the card's CanvasGroup.blocksRaycasts is set to true
            var canvasGroup = cardView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                Debug.Log(
                    "[OrderDropHandler] Ensuring CanvasGroup.blocksRaycasts is true for dropped card"
                );
            }

            // Make sure Card and CardType are not null
            if (cardView.Card == null)
            {
                Debug.LogError("[OrderDropHandler] CardView has null Card reference");
                return;
            }

            if (cardView.Card.CardType == null)
            {
                Debug.LogError($"[OrderDropHandler] Card {cardView.Card.Title} has null CardType");
                return;
            }

            // Check if this is an Order card
            if (cardView.Card.CardType.Category != CardCategory.Order)
            {
                Debug.Log($"[OrderDropHandler] Card {cardView.Card.Title} is not an Order card");
                return;
            }

            // Deploy the Order card using MatchManager directly
            if (matchView == null)
            {
                Debug.LogError("[OrderDropHandler] Failed to deploy Order card - MatchView not available");
                return;
            }

            var matchManager = matchView.MatchManager;
            if (matchManager == null)
            {
                Debug.LogError("[OrderDropHandler] Failed to deploy Order card - MatchManager not available");
                return;
            }

            // Mark this card as being deployed as an order card to prevent attack behaviors
            // Store the original isBeingDragged state to restore it later
            bool originalDragState = cardView.IsBeingDragged;
            cardView.IsBeingDragged = false; // Prevent other handlers from processing this card

            if (!matchManager.DeployCard(cardView.Card, -1))
            {
                Debug.Log($"[OrderDropHandler] Failed to deploy Order card {cardView.Card.Title}");
                cardView.IsBeingDragged = originalDragState; // Restore the original state
            }
            else
            {
                Debug.Log(
                    $"[OrderDropHandler] Order card {cardView.Card.Title} deployed successfully"
                );

                // Find the deployed card in the order area and animate it
                PlayHeartbeatAnimation(cardView.Card);
            }
        }

        /// <summary>
        /// Checks if the card can be deployed as an Order card.
        /// </summary>
        public bool IsValidDropTarget(Card card)
        {
            if (card == null)
            {
                Debug.Log("[OrderDropHandler] Invalid drop target: Card is null");
                return false;
            }

            // Get MatchManager from matchView
            if (matchView == null)
            {
                Debug.Log("[OrderDropHandler] Invalid drop target: MatchView not available");
                return false;
            }

            var matchManager = matchView.MatchManager;
            if (matchManager == null)
            {
                Debug.Log("[OrderDropHandler] Invalid drop target: MatchManager not available");
                return false;
            }

            // Make sure CardType is not null
            if (card.CardType == null)
            {
                Debug.LogError($"[OrderDropHandler] Card {card.Title} has null CardType");
                return false;
            }

            // Check if the card is an Order card
            if (card.CardType.Category != CardCategory.Order)
            {
                Debug.Log($"[OrderDropHandler] Invalid drop target: {card.Title} is not an Order card");
                return false;
            }

            // Check if the card can be deployed according to game rules
            if (!matchManager.CanDeployOrderCard(card))
            {
                Debug.Log(
                    $"[OrderDropHandler] Invalid drop target: {card.Title} cannot be deployed due to game rules"
                );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Plays a heartbeat animation on the card in the order area to provide visual feedback
        /// when a new order card is deployed.
        /// </summary>
        /// <param name="deployedCard">The card that was just deployed</param>
        private void PlayHeartbeatAnimation(Card deployedCard)
        {
            if (deployedCard == null)
                return;

            // Find the CardView for this card in the order area
            // We need to search for it because the original cardView GameObject will be destroyed
            // and a new one created in the order area
            CardView[] cardViews = transform.GetComponentsInChildren<CardView>();
            CardView targetCardView = null;

            // Find the card view matching our deployed card
            foreach (var cv in cardViews)
            {
                if (cv.Card != null && cv.Card.InstanceId.Equals(deployedCard.InstanceId))
                {
                    targetCardView = cv;
                    break;
                }
            }

            // If we found the CardView, animate it
            if (targetCardView != null)
            {
                // Get the Image component to animate (card background)
                Image cardImage = targetCardView.GetComponent<Image>();

                Debug.Log($"[OrderDropHandler] Animating deployed card: {deployedCard.Title}");

                // Use the DOTweenAnimationUtility to play the heartbeat animation on the card
                DOTweenAnimationUtility.AnimateHeartbeat(
                    targetTransform: targetCardView.transform,
                    targetImage: cardImage,
                    heartbeatColor: heartbeatColor,
                    pulseScale: heartbeatScale,
                    pulseDuration: heartbeatDuration,
                    pulseCount: heartbeatPulses
                );
            }
            else
            {
                Debug.LogWarning($"[OrderDropHandler] Could not find deployed card view for animation: {deployedCard.Title}");
            }
        }
    }
}
