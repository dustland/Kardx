using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI
{
    using Kardx.Core;

    [RequireComponent(typeof(CardView))]
    public class AbilityDragHandler
        : MonoBehaviour,
            IPointerDownHandler,
            IPointerUpHandler,
            IDragHandler,
            IBeginDragHandler
    {
        [SerializeField]
        private float minDragDistance = 10f; // Minimum drag distance to start targeting

        private CardView cardView;
        private AttackArrow attackArrow;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private UnitDeployDragHandler deployDragHandler;
        private MatchView matchView;
        private MatchManager matchManager;
        private Vector2 pointerStartPosition;
        private bool isDragging = false;
        private Transform originalParent;
        private Vector3 originalPosition;

        // Event to notify when an attack is initiated
        public event Action<Card, Card> OnAttackInitiated;

        // Events for drag state
        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
            deployDragHandler = GetComponent<UnitDeployDragHandler>();

            // Find Canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[AbilityDragHandler] No Canvas found in parents.");
                return;
            }

            // Find CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Find MatchView and MatchManager
            matchView = GetComponentInParent<MatchView>();
            if (matchView == null)
            {
                Debug.LogError("[AbilityDragHandler] No MatchView found in the scene.");
                return;
            }

            // Get MatchManager through a battlefield view
            PlayerBattlefieldView playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
            if (playerBattlefieldView != null)
            {
                matchManager = playerBattlefieldView.GetMatchManager();
            }

            if (matchManager == null)
            {
                Debug.LogError("[AbilityDragHandler] Cannot find MatchManager reference");
                return;
            }

            // Find or create the attack arrow
            GameObject arrowObj = GameObject.Find("AttackArrow");
            if (arrowObj == null)
            {
                arrowObj = new GameObject("AttackArrow");
                arrowObj.transform.SetParent(canvas.transform, false);
                arrowObj.AddComponent<LineRenderer>();
            }

            attackArrow = arrowObj.GetComponent<AttackArrow>();
            if (attackArrow == null)
            {
                attackArrow = arrowObj.AddComponent<AttackArrow>();
            }
        }

        // Additional initialization method
        public void Initialize(MatchView matchView, MatchManager matchManager)
        {
            this.matchView = matchView;
            this.matchManager = matchManager;

            // Find Canvas if not already set
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            // Verify we have arrow reference
            if (attackArrow == null)
            {
                GameObject arrowObj = GameObject.Find("AttackArrow");
                if (arrowObj != null)
                {
                    attackArrow = arrowObj.GetComponent<AttackArrow>();
                }
            }

            // Set component state appropriately
            UpdateComponentState();
        }

        private void OnEnable()
        {
            // Subscribe to events
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (isDragging)
            {
                EndDragging();
            }
        }

        // Helper method to cleanly end the dragging state
        private void EndDragging()
        {
            isDragging = false;

            if (attackArrow != null)
            {
                attackArrow.StopDrawing();
            }

            // Clear any highlights
            if (matchView != null)
            {
                matchView.ClearAllHighlights();
            }

            OnDragEnded?.Invoke(false);
        }

        // Updates component state based on card abilities
        private void UpdateComponentState()
        {
            if (cardView == null || cardView.Card == null)
            {
                enabled = false;
                return;
            }

            // Only enable this drag handler if the card has abilities and can attack
            bool shouldBeEnabled = CanCardAttack();
            enabled = shouldBeEnabled;

            // If we have a deploy drag handler, make sure it's disabled when this component is enabled
            if (deployDragHandler != null)
            {
                deployDragHandler.enabled = !shouldBeEnabled;
            }
        }

        // Determines if the card can attack (is on battlefield, player's turn, etc.)
        private bool CanCardAttack()
        {
            if (cardView == null || cardView.Card == null || matchManager == null)
                return false;

            var card = cardView.Card;

            // Check if it's the player's turn
            if (!matchManager.IsPlayerTurn())
                return false;

            // Check if the card is on the player's battlefield
            if (!matchManager.Player.Battlefield.Contains(card))
                return false;

            // Check if the card has already attacked this turn
            if (card.HasAttackedThisTurn)
                return false;

            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // If this component is disabled or the card can't attack, don't handle the event
            if (!enabled || !CanCardAttack())
                return;

            pointerStartPosition = eventData.position;
            isDragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // If this component is disabled, don't handle the event
            if (!enabled)
                return;

            if (cardView.Card == null || !CanCardAttack())
                return;

            // Check if we've dragged far enough to start targeting
            if (!isDragging)
            {
                float dragDistance = Vector2.Distance(pointerStartPosition, eventData.position);
                if (dragDistance < minDragDistance)
                    return;

                // Start drawing the attack arrow
                isDragging = true;
                OnDragStarted?.Invoke();
                attackArrow.SetSource(transform);
                attackArrow.StartDrawing();
            }

            // Update the arrow position
            if (isDragging)
            {
                attackArrow.UpdatePosition(eventData.position);

                // Highlight valid targets
                HighlightValidTargets(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // If this component is disabled or we're not dragging, don't handle the event
            if (!enabled || !isDragging)
                return;

            isDragging = false;

            // Check if we're over a valid target
            Card targetCard = FindTargetCardUnderPointer(eventData);
            bool isValidTarget = targetCard != null && IsValidAttackTarget(targetCard);
            OnDragEnded?.Invoke(isValidTarget);

            if (isValidTarget)
            {
                // Get the CardView of the target
                CardView targetCardView = FindCardViewForCard(targetCard);
                if (targetCardView != null)
                {
                    // Complete the arrow drawing to the target
                    attackArrow.FinishDrawing(targetCardView.transform);

                    // Initiate the attack
                    InitiateAttack(cardView.Card, targetCard);
                }
            }
            else
            {
                // Cancel the attack
                attackArrow.CancelDrawing();
            }

            // Clear all highlights
            ClearAllHighlights();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // This is required for the interface but we handle drag start in OnDrag
        }

        private void HighlightValidTargets(PointerEventData eventData)
        {
            if (matchManager == null || cardView.Card == null)
                return;

            // Get opponent battlefield
            OpponentBattlefieldView opponentView = FindAnyObjectByType<OpponentBattlefieldView>();
            if (opponentView != null)
            {
                // Highlight valid targets in opponent battlefield
                opponentView.HighlightValidTargets(cardView.Card);
            }
            else if (matchView != null)
            {
                // Fallback to match view if available
                matchView.ClearAllHighlights();
            }
        }

        private void ClearAllHighlights()
        {
            if (matchView != null)
            {
                matchView.ClearAllHighlights();
            }
        }

        private Card FindTargetCardUnderPointer(PointerEventData eventData)
        {
            // Raycast to find objects under the pointer
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                // Skip this card itself
                if (result.gameObject == gameObject)
                    continue;

                // Look for a CardView component
                CardView targetCardView = result.gameObject.GetComponent<CardView>();
                if (targetCardView != null && targetCardView.Card != null)
                {
                    return targetCardView.Card;
                }
            }

            return null;
        }

        private CardView FindCardViewForCard(Card card)
        {
            if (card == null)
                return null;

            // Find all CardViews in the scene
            CardView[] cardViews = FindObjectsByType<CardView>(FindObjectsSortMode.None);
            foreach (var cv in cardViews)
            {
                if (cv.Card == card)
                    return cv;
            }

            return null;
        }

        private bool IsValidAttackTarget(Card targetCard)
        {
            if (cardView.Card == null || targetCard == null || matchManager == null)
                return false;

            // Find the OpponentBattlefieldView to use its CanTargetCard method
            OpponentBattlefieldView opponentView = FindAnyObjectByType<OpponentBattlefieldView>();
            if (opponentView != null)
            {
                return opponentView.CanTargetCard(cardView.Card, targetCard);
            }

            // Fallback implementation if OpponentBattlefieldView isn't available
            // Check if the target card is on the opponent's battlefield
            return matchManager.Opponent.Battlefield.Contains(targetCard) &&
                   !targetCard.HasAttackedThisTurn && // Using HasAttackedThisTurn as a simple check
                   matchManager.IsPlayerTurn();
        }

        private void InitiateAttack(Card attackerCard, Card targetCard)
        {
            if (attackerCard == null || targetCard == null || matchManager == null)
                return;

            // Notify listeners about the attack
            OnAttackInitiated?.Invoke(attackerCard, targetCard);

            // Try to use the OpponentBattlefieldView to handle the attack
            OpponentBattlefieldView opponentView = FindAnyObjectByType<OpponentBattlefieldView>();
            if (opponentView != null)
            {
                opponentView.AttackCard(attackerCard, targetCard);
            }
            else
            {
                // Fallback to using the match manager directly
                matchManager.InitiateAttack(attackerCard, targetCard);
            }

            // Mark the card as having attacked this turn
            attackerCard.HasAttackedThisTurn = true;

            // Update the component state
            UpdateComponentState();
        }
    }
}
