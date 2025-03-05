using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Kardx.Core;

namespace Kardx.UI
{
    using Kardx.Core;

    [RequireComponent(typeof(CardView))]
    public class AbilityDragHandler
        : MonoBehaviour,
            IPointerDownHandler,
            IPointerUpHandler,
            IDragHandler,
            IBeginDragHandler,
            IEndDragHandler,
            IPointerExitHandler
    {
        [SerializeField]
        private float minDragDistance = 10f; // Minimum drag distance to start targeting

        [SerializeField]
        private GameObject attackArrowPrefab;

        private CardView cardView;
        private AttackArrow attackArrow;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private UnitDeployDragHandler deployDragHandler;
        private MatchView matchView;
        private MatchManager matchManager;
        private Vector2 pointerStartPosition;
        private OpponentCardSlot currentTarget = null;

        // Events for drag state
        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;
        public event Action OnPointerDownEvent;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
            deployDragHandler = GetComponent<UnitDeployDragHandler>();

            // Find Canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
                Debug.LogWarning("[AbilityDragHandler] No canvas found in parents, using any canvas in scene");
            }

            // Find CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Find MatchView and MatchManager
            matchView = FindAnyObjectByType<MatchView>();
            if (matchView == null)
            {
                Debug.LogError("[AbilityDragHandler] No MatchView found in the scene.");
                return;
            }
            matchManager = matchView.MatchManager;

            if (matchManager == null)
            {
                Debug.LogError("[AbilityDragHandler] Cannot find MatchManager reference");
                return;
            }

            InitializeAttackArrow();
        }

        private void InitializeAttackArrow()
        {
            // Find or create the attack arrow
            if (attackArrow == null)
            {
                attackArrow = FindAnyObjectByType<AttackArrow>(FindObjectsInactive.Include);
                if (attackArrow == null && attackArrowPrefab != null)
                {
                    GameObject arrowObj = Instantiate(attackArrowPrefab, canvas.transform);
                    attackArrow = arrowObj.GetComponent<AttackArrow>();
                    Debug.Log($"[AbilityDragHandler] Created attack arrow from prefab: {attackArrow != null}");
                }
                else if (attackArrow != null)
                {
                    Debug.Log("[AbilityDragHandler] Found existing attack arrow in scene");
                }
            }

            if (attackArrow == null)
            {
                Debug.LogError("[AbilityDragHandler] Failed to initialize attack arrow");
            }
            else
            {
                attackArrow.Initialize(matchView);
                // Ensure it's initially disabled
                attackArrow.gameObject.SetActive(false);
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
            if (cardView != null && cardView.IsBeingDragged)
            {
                EndDragging();
            }
        }

        // Helper method to cleanly end the dragging state
        private void EndDragging()
        {
            cardView.IsBeingDragged = false;

            if (attackArrow != null)
            {
                attackArrow.CancelDrawing();
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
            // Abort if the card cannot attack
            if (!CanCardAttack())
            {
                return;
            }

            // Store the initial pointer position for arrow drawing
            pointerStartPosition = eventData.position;
            Debug.Log($"[AbilityDragHandler] Pointer down at: {pointerStartPosition}");

            // Invoke the pointer down event
            OnPointerDownEvent?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // If this component is disabled or we're not dragging, don't handle the event
            if (!enabled || cardView == null || !cardView.IsBeingDragged)
                return;

            cardView.IsBeingDragged = false;

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
                    Vector2 targetPosition = new Vector2(targetCardView.transform.position.x, targetCardView.transform.position.y);
                    attackArrow.UpdateEndPosition(targetPosition);

                    // Initiate the attack
                    matchManager.InitiateAttack(cardView.Card, targetCard);
                }
            }
            // Hide the attack arrow immediately
            attackArrow.CancelDrawing();
            // Clear all highlights
            ClearAllHighlights();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Get the card view component
            cardView = GetComponent<CardView>();
            if (cardView == null || !CanCardAttack())
            {
                return;
            }

            // Initialize drag state
            cardView.IsBeingDragged = true;

            // Initialize the attack arrow if needed
            if (attackArrow == null)
            {
                InitializeAttackArrow();
            }

            if (attackArrow != null)
            {
                // Use the stored pointer start position for the initial arrow position
                attackArrow.StartDrawing();
                attackArrow.UpdateStartPosition(pointerStartPosition);
                attackArrow.UpdateEndPosition(pointerStartPosition);

                Debug.Log($"[AbilityDragHandler] Started drawing arrow from pointer position: {pointerStartPosition}");
            }
            else
            {
                Debug.LogError("[AbilityDragHandler] Failed to initialize attack arrow");
            }

            // Highlight valid targets
            HighlightValidTargets(eventData);

            // Invoke the drag started event
            OnDragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (cardView == null || !cardView.IsBeingDragged || !CanCardAttack())
            {
                return;
            }

            // Update the arrow end position
            if (attackArrow != null)
            {
                attackArrow.UpdateEndPosition(eventData.position);
            }

            // Update target highlights based on current pointer position
            OpponentCardSlot currentSlot = GetTargetUnderPointer(eventData);
            
            // Only proceed if we're over a different slot than before
            if (currentSlot != currentTarget)
            {
                // Clear previous target highlight
                if (currentTarget != null)
                {
                    // Use the same color as in HighlightValidTargets
                    Card previousTargetCard = matchManager?.Opponent?.Battlefield?.GetCardAt(currentTarget.SlotIndex);
                    if (previousTargetCard != null)
                    {
                        Color availableColor = new Color(1f, 0.2f, 0f, 0.9f); // Bright orange-red with high alpha
                        currentTarget.SetHighlight(availableColor, true);
                    }
                    else
                    {
                        // If the slot is empty, clear the highlight
                        currentTarget.SetHighlight(Color.clear, false);
                    }
                }

                // Set new target highlight
                currentTarget = currentSlot;
                if (currentTarget != null && IsValidTarget(currentTarget))
                {
                    // Make the current hover target more prominent
                    Color dropTargetColor = new Color(1f, 0.8f, 0f, 1.0f); // Bright golden color
                    currentTarget.SetHighlight(dropTargetColor, true);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (cardView == null || !cardView.IsBeingDragged || !CanCardAttack())
            {
                return;
            }

            // Refresh the valid targets highlights
            HighlightValidTargets(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (cardView == null)
                return;

            // Reset dragging state
            cardView.IsBeingDragged = false;

            // Hide the attack arrow
            if (attackArrow != null)
            {
                attackArrow.CancelDrawing();
            }

            // Clear all highlights
            ClearAllHighlights();
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            OnPointerUp(eventData);
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
            // Clear the current target highlight
            if (currentTarget != null)
            {
                currentTarget.SetHighlight(Color.clear, false);
                currentTarget = null;
            }

            // Then clear all highlights through the matchView
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

        private OpponentCardSlot GetTargetUnderPointer(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                // Only look for OpponentCardSlot components (not PlayerCardSlot)
                OpponentCardSlot slot = result.gameObject.GetComponent<OpponentCardSlot>();
                if (slot != null)
                {
                    // Only return the slot if it contains a card
                    Card targetCard = matchManager?.Opponent?.Battlefield?.GetCardAt(slot.SlotIndex);
                    if (targetCard != null)
                    {
                        return slot;
                    }
                }
            }

            return null;
        }

        private bool IsValidTarget(OpponentCardSlot target)
        {
            if (target == null || cardView == null || matchManager == null)
            {
                return false;
            }

            // Get the card data
            Card attackerCard = cardView.Card;

            // Get the target card using the battlefield
            var targetPlayer = matchManager.Opponent;
            var defenderCard = targetPlayer?.Battlefield.GetCardAt(target.SlotIndex);

            // Check if the target has a card
            if (defenderCard == null)
            {
                return false;
            }

            // Check if the attacker can attack this target (implement your game rules here)
            // For example, check if the target is in range, if the attacker has enough action points, etc.

            // Simple implementation: all opponent cards are valid targets
            return true;
        }
    }
}
