using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using Kardx.Views.Cards;
using Kardx.Views.Match;
using Kardx.Models.Match;
using Kardx.Models.Cards;

namespace Kardx.Controllers.DragHandlers
{
    [RequireComponent(typeof(CardView))]
    public class AbilityDragHandler
        : MonoBehaviour,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler,
            IPointerDownHandler,
            IPointerUpHandler
    {
        [SerializeField]
        private GameObject attackArrowPrefab;

        private CardView cardView;
        private AttackArrow attackArrow;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private MatchManager matchManager;
        private UnitDeployDragHandler deployDragHandler;
        private OpponentBattlefieldView opponentBattlefieldView;

        // Events for drag state
        public event Action OnDragStarted;
        public event Action<bool> OnDragEnded;
        public event Action OnPointerDownEvent;

        private void Awake()
        {
            // Cache component references
            cardView = GetComponent<CardView>();
            canvasGroup = GetComponent<CanvasGroup>();
            deployDragHandler = GetComponent<UnitDeployDragHandler>();

            // Find the canvas
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("[AbilityDragHandler] No canvas found in scene");
                }
            }

            // Find the opponent battlefield view in the scene
            opponentBattlefieldView = FindAnyObjectByType<OpponentBattlefieldView>();
            
            // Get reference to match manager
            matchManager = FindAnyObjectByType<MatchManager>();
            if (matchManager == null)
            {
                Debug.LogError("[AbilityDragHandler] No MatchManager found in scene");
            }
            
            // Update component state based on card abilities
            UpdateComponentState();
        }

        private void OnEnable()
        {
            if (cardView == null)
            {
                cardView = GetComponent<CardView>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            
            // Update component state when enabled
            UpdateComponentState();
        }

        public void Initialize(CardView cardView)
        {
            this.cardView = cardView;
            this.canvasGroup = cardView.GetComponent<CanvasGroup>();
            opponentBattlefieldView = FindAnyObjectByType<OpponentBattlefieldView>();
            matchManager = FindAnyObjectByType<MatchManager>();
            deployDragHandler = GetComponent<UnitDeployDragHandler>();
            
            // Update component state after initialization
            UpdateComponentState();
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
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // Abort if the card cannot attack
            if (!CanCardAttack())
            {
                return;
            }

            // Invoke the pointer down event
            OnPointerDownEvent?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // If this component is disabled or we're not dragging, don't handle the event
            if (!enabled || cardView == null || !cardView.IsBeingDragged)
                return;

            // This is now handled in OnEndDrag
            // Only do minimal cleanup here if needed
            Debug.Log("[AbilityDragHandler] OnPointerUp called");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("[AbilityDragHandler] Begin Drag");
            
            // Ensure the card can be dragged
            if (!CanCardAttack())
            {
                Debug.Log("[AbilityDragHandler] Card cannot attack, aborting drag");
                return;
            }

            // Set the CardView.IsBeingDragged flag
            cardView.IsBeingDragged = true;

            // Disable raycasting on this card while dragging
            // This is critical to allow the drop target to receive the drop event
            canvasGroup.blocksRaycasts = false;

            // Initialize the attack arrow if needed
            if (attackArrow == null)
            {
                InitializeAttackArrow();
            }

            if (attackArrow != null)
            {
                // Start drawing the attack arrow
                attackArrow.StartDrawing();
                attackArrow.UpdateStartPosition(transform.position);
                attackArrow.UpdateEndPosition(eventData.position);
            }

            // Highlight valid targets
            if (opponentBattlefieldView != null && cardView.Card != null)
            {
                Debug.Log("[AbilityDragHandler] Highlighting valid targets via OpponentBattlefieldView");
                opponentBattlefieldView.HighlightValidTargets(cardView.Card);
            }
            else
            {
                Debug.LogError("[AbilityDragHandler] OpponentBattlefieldView not available for highlighting");
            }

            // Invoke drag started event
            OnDragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!cardView.IsBeingDragged)
                return;

            // Update the attack arrow end position
            if (attackArrow != null)
            {
                attackArrow.UpdateEndPosition(eventData.position);
            }
            
            // Debug raycast under pointer to help diagnose drop issues
            if (Input.GetKeyDown(KeyCode.D))
            {
                DebugRaycastUnderPointer(eventData.position);
            }
        }

        private void DebugRaycastUnderPointer(Vector2 position)
        {
            // Cast rays to find all objects under the pointer
            var tempEventData = new PointerEventData(EventSystem.current)
            {
                position = position
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(tempEventData, results);

            Debug.Log($"[AbilityDragHandler] Objects under pointer: {results.Count}");
            foreach (var result in results)
            {
                Debug.Log($"[AbilityDragHandler] - {result.gameObject.name} (Layer: {result.gameObject.layer}, SortingOrder: {result.sortingOrder})");
                
                // Check if it's an opponent card slot
                OpponentCardSlot slot = result.gameObject.GetComponent<OpponentCardSlot>();
                if (slot != null)
                {
                    Debug.Log($"[AbilityDragHandler] - Found OpponentCardSlot: {slot.SlotIndex}");
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"[AbilityDragHandler] End Drag - pointerEnter: {eventData.pointerEnter?.name ?? "null"}");

            if (!cardView.IsBeingDragged)
                return;

            // Reset the CardView.IsBeingDragged flag
            cardView.IsBeingDragged = false;

            // Re-enable raycast blocking
            canvasGroup.blocksRaycasts = true;

            // Debug raycast under pointer to help diagnose drop issues
            DebugRaycastUnderPointer(eventData.position);

            // The attack logic is handled by the drop target (OpponentCardSlot.OnDrop)
            // This drag handler only needs to clean up if it wasn't dropped on a valid target

            // Check if it was dropped on a valid target
            bool droppedOnTarget = false;
            
            // Cast rays to find all objects under the pointer
            var tempEventData = new PointerEventData(EventSystem.current)
            {
                position = eventData.position
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(tempEventData, results);
            
            // Check if any of the objects under the pointer is an OpponentCardSlot
            foreach (var result in results)
            {
                OpponentCardSlot slot = result.gameObject.GetComponent<OpponentCardSlot>();
                if (slot != null)
                {
                    droppedOnTarget = true;
                    Debug.Log($"[AbilityDragHandler] Found drop target: {slot.name} (SlotIndex: {slot.SlotIndex})");
                    
                    // Manually call the OnDrop method on the slot
                    // This is a workaround for when the drop event isn't being detected
                    slot.OnDrop(eventData);
                    break;
                }
            }

            Debug.Log($"[AbilityDragHandler] Dropped on target: {droppedOnTarget}");

            if (!droppedOnTarget)
            {
                // Hide the attack arrow
                if (attackArrow != null)
                {
                    attackArrow.CancelDrawing();
                }

                // Clear any highlights
                if (opponentBattlefieldView != null)
                {
                    opponentBattlefieldView.ClearCardHighlights();
                }

                // Invoke drag ended event
                OnDragEnded?.Invoke(false);
            }
            else
            {
                // The drop handler will handle the attack logic and cleanup
                Debug.Log("[AbilityDragHandler] Dropped on valid target, waiting for drop handler to process");
                
                // Note: We don't clean up here because the drop handler will call NotifyDropSuccess
                // which will handle the cleanup
            }
        }

        /// <summary>
        /// Called by the drop target when a drop is successful
        /// </summary>
        public void NotifyDropSuccess()
        {
            Debug.Log("[AbilityDragHandler] Drop successful");
            
            // Hide the attack arrow
            if (attackArrow != null)
            {
                attackArrow.CancelDrawing();
            }
            
            // Invoke the event
            OnDragEnded?.Invoke(true);
        }

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

        private void InitializeAttackArrow()
        {
            if (attackArrowPrefab != null)
            {
                GameObject arrowObj = Instantiate(attackArrowPrefab, canvas.transform);
                attackArrow = arrowObj.GetComponent<AttackArrow>();
                if (attackArrow == null)
                {
                    Debug.LogError("[AbilityDragHandler] Attack arrow prefab does not have an AttackArrow component");
                }
            }
            else
            {
                Debug.LogError("[AbilityDragHandler] No attack arrow prefab assigned");
            }
        }
    }
}
