using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;
using Kardx.Views.Match;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Single drag entry point for all card interactions.
    ///
    /// Workflow:
    ///   RefreshCapability() → resolve mode from game state
    ///   OnBeginDrag        → highlight valid targets, float card / show attack arrow
    ///   OnDrag             → follow pointer or update arrow
    ///   OnEndDrag          → raycast target → CardDropResolver → always cleanup in finally
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public class CardDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float dragOffset = 15f;
        [SerializeField] private GameObject attackArrowPrefab;

        private CardView cardView;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private MatchManager matchManager;
        private PlayerBattlefieldView playerBattlefieldView;
        private OpponentBattlefieldView opponentBattlefieldView;
        private AttackArrow attackArrow;

        private CardDragMode activeMode = CardDragMode.None;
        private Vector3 originalPosition;
        private Transform originalParent;
        private bool isDragging;

        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = FindAnyObjectByType<Canvas>();

            matchManager = FindAnyObjectByType<MatchManager>();
            playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
            opponentBattlefieldView = FindAnyObjectByType<OpponentBattlefieldView>();
        }

        private void OnEnable()
        {
            RefreshCapability();
        }

        /// <summary>
        /// Re-evaluates whether this card can be dragged right now.
        /// </summary>
        public void RefreshCapability()
        {
            if (cardView == null)
                cardView = GetComponent<CardView>();

            bool canDrag =
                !isDragging
                && cardView?.Card != null
                && CardDragCapability.ResolveMode(cardView.Card, matchManager) != CardDragMode.None;

            enabled = canDrag;

            var background = GetComponent<Image>();
            if (background != null)
                background.raycastTarget = canDrag;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            activeMode = CardDragCapability.ResolveMode(cardView.Card, matchManager);
            if (activeMode == CardDragMode.None)
                return;

            isDragging = true;
            cardView.IsBeingDragged = true;
            originalParent = transform.parent;
            originalPosition = transform.position;

            canvasGroup.blocksRaycasts = false;

            if (ShouldFloatCard())
            {
                if (rootCanvas != null)
                    transform.SetParent(rootCanvas.transform, true);

                if (activeMode == CardDragMode.PlayOrder && playerBattlefieldView != null)
                    playerBattlefieldView.SetSlotsRaycastActive(false);
            }

            BeginHighlights();

            if (ShouldShowAttackArrow())
            {
                var arrow = EnsureAttackArrow();
                arrow?.StartDrawing();
            }

            OnDragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            if (ShouldFloatCard())
            {
                transform.position = eventData.position + new Vector2(0, dragOffset);
            }
            else if (ShouldShowAttackArrow() && attackArrow != null)
            {
                attackArrow.UpdateStartPosition(transform.position);
                attackArrow.UpdateEndPosition(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            bool restoreTransform = true;
            bool success = false;

            try
            {
                var target = CardDragTargetFinder.Find(
                    eventData.position,
                    activeMode,
                    matchManager,
                    cardView.Card
                );

                if (target.Kind != CardDragTargetKind.None)
                {
                    success = CardDropResolver.TryExecute(
                        activeMode,
                        cardView.Card,
                        target,
                        matchManager,
                        cardView,
                        out restoreTransform
                    );

                    if (success)
                    {
                        if (
                            activeMode == CardDragMode.BattlefieldAction
                            && target.Kind == CardDragTargetKind.PlayerBattlefieldSlot
                        )
                        {
                            ReparentToPlayerSlot(target.SlotIndex);
                            restoreTransform = false;
                        }
                        else if (activeMode == CardDragMode.PlayOrder)
                        {
                            CardDragFeedback.PlayOrderDeployedPulse(
                                cardView.Card,
                                transform.root
                            );
                        }
                    }
                }
            }
            finally
            {
                EndDragCleanup(restoreTransform);
                OnDragEnded?.Invoke(success);
            }
        }

        private void EndDragCleanup(bool restoreTransform)
        {
            if (activeMode == CardDragMode.PlayOrder && playerBattlefieldView != null)
                playerBattlefieldView.SetSlotsRaycastActive(true);

            CancelAttackArrow();
            ClearHighlights();

            canvasGroup.blocksRaycasts = true;
            isDragging = false;
            cardView.IsBeingDragged = false;
            activeMode = CardDragMode.None;

            if (restoreTransform)
                RestoreTransform();

            RefreshCapability();
        }

        private void BeginHighlights()
        {
            var card = cardView.Card;
            if (card == null)
                return;

            switch (activeMode)
            {
                case CardDragMode.DeployUnit:
                    playerBattlefieldView?.HighlightEmptySlotsForCard(card);
                    break;

                case CardDragMode.BattlefieldAction:
                    if (CardDragCapability.CanAttack(card, matchManager))
                        opponentBattlefieldView?.HighlightValidTargets(card);
                    if (CardDragCapability.CanMove(card, matchManager))
                        playerBattlefieldView?.HighlightMoveTargets(card);
                    break;
            }
        }

        private void ClearHighlights()
        {
            playerBattlefieldView?.ClearCardHighlights();
            opponentBattlefieldView?.ClearCardHighlights();

            foreach (var hq in FindObjectsByType<HeadquarterView>(FindObjectsSortMode.None))
                hq.SetHighlight(false);
        }

        private bool ShouldFloatCard()
        {
            if (activeMode != CardDragMode.BattlefieldAction)
                return true;

            return CardDragCapability.CanMove(cardView.Card, matchManager)
                && !CardDragCapability.CanAttack(cardView.Card, matchManager);
        }

        private bool ShouldShowAttackArrow()
        {
            return activeMode == CardDragMode.BattlefieldAction
                && CardDragCapability.CanAttack(cardView.Card, matchManager);
        }

        private void RestoreTransform()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                transform.localPosition = Vector3.zero;
            }
            else
            {
                transform.position = originalPosition;
            }
        }

        private void ReparentToPlayerSlot(int slotIndex)
        {
            if (playerBattlefieldView == null)
                return;

            var slots = playerBattlefieldView.GetSlots();
            if (slotIndex < 0 || slotIndex >= slots.Length)
                return;

            var slot = slots[slotIndex];
            transform.SetParent(slot.CardContainer, false);
            transform.localPosition = Vector3.zero;
        }

        private AttackArrow EnsureAttackArrow()
        {
            if (attackArrow != null)
                return attackArrow;

            if (attackArrowPrefab != null && rootCanvas != null)
            {
                var arrowObj = Instantiate(attackArrowPrefab, rootCanvas.transform);
                attackArrow = arrowObj.GetComponent<AttackArrow>();
            }
            else
            {
                attackArrow = FindAnyObjectByType<AttackArrow>();
            }

            if (attackArrow != null)
            {
                attackArrow.UpdateStartPosition(transform.position);
            }

            return attackArrow;
        }

        private void CancelAttackArrow()
        {
            if (attackArrow != null)
                attackArrow.CancelDrawing();
        }
    }
}
