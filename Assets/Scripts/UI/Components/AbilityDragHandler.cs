using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Components
{
    using Kardx.UI.Scenes;
    using Card = Kardx.Core.Card;

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
        private DeployDragHandler deployDragHandler;
        private MatchView matchView;
        private Vector2 pointerStartPosition;
        private bool isDragging = false;
        private bool canAttack = false;
        private Transform originalParent;
        private Vector3 originalPosition;

        // Event to notify when an attack is initiated
        public event Action<Card, Card> OnAttackInitiated;

        // Events for drag state (similar to DeployDragHandler)
        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
            deployDragHandler = GetComponent<DeployDragHandler>();

            // Find Canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[AbilityDragHandler] No Canvas found in parents.");
                return;
            }

            // Find CanvasGroup
            canvasGroup = GetComponentInParent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogError("[AbilityDragHandler] No CanvasGroup found in parents.");
                return;
            }

            // Find MatchView
            matchView = FindObjectOfType<MatchView>();
            if (matchView == null)
            {
                Debug.LogError("[AbilityDragHandler] No MatchView found in the scene.");
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

        private void OnEnable()
        {
            // Check if this component should be enabled based on card state
            UpdateComponentState();
        }

        private void Update()
        {
            // We don't need to update component state every frame
            // UpdateComponentState will be called when needed:
            // - When the component is enabled (OnEnable)
            // - When explicitly requested via SendMessage
            // - When game state changes (turn changes, card state changes)
        }

        private void UpdateComponentState()
        {
            bool shouldBeEnabled = CanCardAttack();

            // Debug log to help diagnose issues with enabling/disabling
            // Debug.Log(
            //     $"[AbilityDragHandler] Card {(cardView?.Card?.Title ?? "unknown")} CanCardAttack: {shouldBeEnabled}"
            // );

            // Enable this component only when the card can attack
            enabled = shouldBeEnabled;

            // If we have a DeployDragHandler, make sure it's disabled when this component is enabled
            if (deployDragHandler != null)
            {
                // Only enable DeployDragHandler when this card is in hand, not on battlefield
                bool isInHand = false;
                if (cardView.Card != null && cardView.Card.Owner != null)
                {
                    var handCards = cardView.Card.Owner.Hand;
                    foreach (var card in handCards)
                    {
                        if (card == cardView.Card)
                        {
                            isInHand = true;
                            break;
                        }
                    }
                }

                deployDragHandler.enabled = isInHand && !shouldBeEnabled;
            }
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

                    // Hide the arrow after a short delay
                    Invoke("HideArrow", 0.5f);
                }
                else
                {
                    HideArrow();
                }
            }
            else
            {
                HideArrow();
            }

            // Clear any highlights
            ClearHighlights();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log(
                $"[AbilityDragHandler] OnBeginDrag called for {(cardView?.Card?.Title ?? "unknown")}"
            );

            // Store original state for resetting after drag
            originalParent = transform.parent;
            originalPosition = transform.position;

            // Disable blocksRaycasts during drag
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                Debug.Log("[AbilityDragHandler] Disabled blocksRaycasts for drag");
            }

            // Show the attack arrow
            ShowArrow();

            // Highlight valid targets
            HighlightValidTargets(eventData);

            OnDragStarted?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"[AbilityDragHandler] OnEndDrag called for {cardView.Card.Title}");

            // Re-enable blocksRaycasts to allow the card to be clicked again
            canvasGroup.blocksRaycasts = true;

            // Clear all highlights
            ClearHighlights();

            // Check if we dropped on a valid target
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            bool wasSuccessful = false;
            Card targetCard = null;

            foreach (var hit in raycastResults)
            {
                var opponentSlot = hit.gameObject.GetComponent<OpponentCardSlot>();
                if (opponentSlot != null)
                {
                    // Get the card at this position
                    var cardAtSlot = GetCardAtOpponentSlot(opponentSlot);
                    if (cardAtSlot != null && IsValidAttackTarget(cardAtSlot))
                    {
                        targetCard = cardAtSlot;
                        wasSuccessful = true;
                        Debug.Log($"[AbilityDragHandler] Valid target found: {targetCard.Title}");
                        break;
                    }
                }
            }

            // Return to original position
            transform.SetParent(originalParent);
            transform.position = originalPosition;

            // If we found a valid target, initiate the attack
            if (wasSuccessful && targetCard != null)
            {
                Debug.Log(
                    $"[AbilityDragHandler] Initiating attack from {cardView.Card.Title} to {targetCard.Title}"
                );
                OnAttackInitiated?.Invoke(cardView.Card, targetCard);
            }

            OnDragEnded?.Invoke(wasSuccessful);
        }

        private void HideArrow()
        {
            attackArrow.Hide();
        }

        private void ShowArrow()
        {
            if (attackArrow != null)
            {
                attackArrow.SetSource(transform);
                attackArrow.StartDrawing();
                Debug.Log("[AbilityDragHandler] Attack arrow shown");
            }
            else
            {
                Debug.LogWarning("[AbilityDragHandler] Attack arrow is null, cannot show");
            }
        }

        private bool CanCardAttack()
        {
            // Check if this card is on the battlefield and belongs to the player
            if (cardView == null || cardView.Card == null || cardView.Card.Owner == null)
            {
                Debug.Log("[AbilityDragHandler] Card or owner is null, can't attack");
                return false;
            }

            // Check if this card is on the battlefield
            var battlefieldCards = cardView.Card.Owner.Battlefield;
            bool isOnBattlefield = false;
            foreach (var card in battlefieldCards)
            {
                if (card == cardView.Card)
                {
                    isOnBattlefield = true;
                    break;
                }
            }
            if (!isOnBattlefield)
            {
                Debug.Log($"[AbilityDragHandler] Card {cardView.Card.Title} is not on battlefield");
                return false;
            }

            // Check if this card is the current player's card
            bool isCurrentPlayer = cardView.Card.Owner == matchView.GetCurrentPlayer();
            if (!isCurrentPlayer)
            {
                Debug.Log(
                    $"[AbilityDragHandler] Card {cardView.Card.Title} is not owned by current player"
                );
                return false;
            }

            // Check if the card has already attacked this turn
            if (cardView.Card.HasAttackedThisTurn)
            {
                Debug.Log(
                    $"[AbilityDragHandler] Card {cardView.Card.Title} has already attacked this turn"
                );
                return false;
            }

            // Debug.Log($"[AbilityDragHandler] Card {cardView.Card.Title} can attack");
            return true;
        }

        private void HighlightValidTargets(PointerEventData eventData)
        {
            // Find all valid target slots (opponent card slots)
            var opponentSlots = canvas.GetComponentsInChildren<OpponentCardSlot>(true);

            foreach (var slot in opponentSlots)
            {
                // Get the card at this position
                var targetCard = GetCardAtOpponentSlot(slot);
                if (targetCard != null && IsValidAttackTarget(targetCard))
                {
                    slot.SetHighlight(true);
                }
                else
                {
                    slot.SetHighlight(false);
                }
            }
        }

        private void ClearHighlights()
        {
            // Clear highlights from opponent slots
            var opponentSlots = canvas.GetComponentsInChildren<OpponentCardSlot>(true);
            foreach (var slot in opponentSlots)
            {
                slot.SetHighlight(false);
            }
        }

        private Card GetCardAtOpponentSlot(OpponentCardSlot slot)
        {
            if (matchView == null)
                return null;

            var opponent = matchView.GetOpponent();
            if (opponent == null)
                return null;

            int position = slot.GetPosition();
            if (position < 0 || position >= opponent.Battlefield.Count)
                return null;

            return opponent.Battlefield[position];
        }

        private bool IsValidAttackTarget(Card targetCard)
        {
            if (targetCard == null || targetCard.Owner == null || cardView.Card == null)
                return false;

            // Can't attack own cards
            if (targetCard.Owner == cardView.Card.Owner)
                return false;

            // Can only attack cards on the battlefield
            var battlefieldCards = targetCard.Owner.Battlefield;
            bool isOnBattlefield = false;
            foreach (var card in battlefieldCards)
            {
                if (card == targetCard)
                {
                    isOnBattlefield = true;
                    break;
                }
            }
            if (!isOnBattlefield)
                return false;

            // Add any other attack validation rules here

            return true;
        }

        private Card FindTargetCardUnderPointer(PointerEventData eventData)
        {
            // Raycast to find objects under the pointer
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                // Skip the current card
                if (result.gameObject == gameObject)
                    continue;

                CardView cv = result.gameObject.GetComponent<CardView>();
                if (cv != null && cv.Card != null)
                {
                    return cv.Card;
                }
            }

            return null;
        }

        private CardView FindCardViewForCard(Card card)
        {
            // Find all CardViews in the scene
            CardView[] allCardViews = canvas.GetComponentsInChildren<CardView>(true);

            foreach (CardView cv in allCardViews)
            {
                if (cv.Card == card)
                {
                    return cv;
                }
            }

            return null;
        }

        private void InitiateAttack(Card attackerCard, Card defenderCard)
        {
            Debug.Log(
                $"[AbilityDragHandler] Initiating attack from {attackerCard.Title} to {defenderCard.Title}"
            );

            // Notify listeners about the attack
            OnAttackInitiated?.Invoke(attackerCard, defenderCard);

            // Call the MatchView to handle the attack
            matchView.AttackCard(attackerCard, defenderCard);
        }
    }
}
