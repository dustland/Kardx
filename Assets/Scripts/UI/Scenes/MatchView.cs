using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core;
using Kardx.Core.Planning;
using Kardx.UI.Components;
using Kardx.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI.Scenes
{
    using Card = Kardx.Core.Card;

    public class MatchView : MonoBehaviour
    {
        [Header("References")]
        private MatchManager matchManager;

        [Header("Layout Areas")]
        [SerializeField]
        private Transform handArea;

        [SerializeField]
        private Transform battlefieldArea; // Area for player's battlefield with HorizontalLayoutGroup

        [SerializeField]
        private Transform opponentBattlefieldArea; // Area for opponent's battlefield with HorizontalLayoutGroup

        [SerializeField]
        private Transform opponentHandArea;

        [SerializeField]
        private Transform discardArea;

        [SerializeField]
        private Transform opponentDiscardArea;

        [SerializeField]
        private Transform headquarter;

        [SerializeField]
        private Transform opponentHeadquarter;

        [SerializeField]
        private Transform orderArea;

        [Header("UI Elements")]
        [SerializeField]
        private GameObject playerCardSlotPrefab; // Prefab for player card slot

        [SerializeField]
        private GameObject opponentCardSlotPrefab; // Prefab for opponent card slot

        [SerializeField]
        private GameObject cardPrefab;

        [SerializeField]
        private TextMeshProUGUI turnText;

        [SerializeField]
        private TextMeshProUGUI creditsText;

        [SerializeField]
        private TextMeshProUGUI opponentCreditsText;

        // Non-serialized fields
        private Dictionary<Card, GameObject> cardUIElements = new();
        private Transform[] battlefieldSlots = new Transform[Player.BATTLEFIELD_SLOT_COUNT];
        private Transform[] opponentBattlefieldSlots = new Transform[Player.BATTLEFIELD_SLOT_COUNT];

        [SerializeField]
        private CardDetailView cardDetailView; // Reference to the CardDetailView

        private List<CardType> cards = new();

        private void Awake()
        {
            // Ensure the CardPanel is initially active so it can run coroutines
            // but immediately hide it
            if (!cardDetailView.gameObject.activeSelf)
            {
                cardDetailView.gameObject.SetActive(true);
                cardDetailView.Hide(); // This will properly hide it after initialization
            }

            // Initialize the shared reference
            CardView.InitializeSharedDetailView(cardDetailView);

            // Make sure the CardPanel is initially inactive
            cardDetailView.gameObject.SetActive(false);

            InitializeBattlefieldSlots();
        }

        private void InitializeBattlefieldSlots()
        {
            if (
                battlefieldArea == null
                || opponentBattlefieldArea == null
                || playerCardSlotPrefab == null
                || opponentCardSlotPrefab == null
            )
            {
                Debug.LogError(
                    "[MatchView] Missing required components for battlefield initialization"
                );
                return;
            }

            // Create player's battlefield slots
            for (int i = 0; i < Player.BATTLEFIELD_SLOT_COUNT; i++)
            {
                var slot = Instantiate(playerCardSlotPrefab, battlefieldArea);
                slot.name = $"CardSlot{i + 1}";
                battlefieldSlots[i] = slot.transform;
                var cardSlot = slot.GetComponent<PlayerCardSlot>();
                if (cardSlot != null)
                {
                    cardSlot.SetPosition(i);
                    // PlayerCardSlot is designed to be droppable by default
                }
            }

            // Create opponent's battlefield slots
            for (int i = 0; i < Player.BATTLEFIELD_SLOT_COUNT; i++)
            {
                var slot = Instantiate(opponentCardSlotPrefab, opponentBattlefieldArea);
                slot.name = $"OpponentCardSlot{i + 1}";
                opponentBattlefieldSlots[i] = slot.transform;
                var cardSlot = slot.GetComponent<OpponentCardSlot>();
                if (cardSlot != null)
                {
                    cardSlot.SetPosition(i);
                    // No need to set droppable for OpponentCardSlot as it's designed specifically for opponent cards
                }
            }
        }

        private void Start()
        {
            // Set up the match
            SetupMatch();
        }

        private void SetupMatch()
        {
            // Create MatchManager instance
            matchManager = new MatchManager(new SimpleLogger("[MatchManager]"));

            // Subscribe to MatchManager events
            matchManager.OnCardDeployed += HandleCardDeployed;
            matchManager.OnCardDrawn += HandleCardDrawn;
            matchManager.OnCardDiscarded += HandleCardDiscarded;
            matchManager.OnTurnStarted += HandleTurnStarted;
            matchManager.OnTurnEnded += HandleTurnEnded;
            matchManager.OnMatchStarted += HandleMatchStarted;
            matchManager.OnMatchEnded += HandleMatchEnded;
            matchManager.OnAttackCompleted += HandleAttackCompleted;
            matchManager.OnCardDied += HandleCardDied;

            // Subscribe to AI turn processing
            matchManager.OnProcessAITurn += HandleProcessAITurn;

            // Start the match
            matchManager.StartMatch();

            // Initialize the UI
            UpdateUI();
        }

        private void HandleMatchStarted(string message)
        {
            Debug.Log($"[MatchView] Match started: {message}");

            // Do a full UI refresh at the start of the match
            UpdateUI();
        }

        private void HandleMatchEnded(string message)
        {
            Debug.Log($"[MatchView] Match ended: {message}");

            // Handle match end UI updates if needed
        }

        private void HandleTurnStarted(object sender, Player player)
        {
            UpdateTurnDisplay();
            UpdateCreditsDisplay();
        }

        private void HandleTurnEnded(object sender, Player player)
        {
            // Any cleanup needed at the end of a turn
        }

        private void UpdateTurnDisplay()
        {
            if (matchManager == null)
                return;

            // Update turn info
            if (turnText != null)
            {
                turnText.text = $"Turn {matchManager.TurnNumber}";
            }
        }

        /// <summary>
        /// Handles the AI turn processing using Unity coroutines
        /// </summary>
        private void HandleProcessAITurn(
            Board board,
            StrategyPlanner strategyPlanner,
            Action onComplete
        )
        {
            StartCoroutine(ProcessAITurnCoroutine(board, strategyPlanner, onComplete));
        }

        /// <summary>
        /// Coroutine that processes the AI turn
        /// </summary>
        private IEnumerator ProcessAITurnCoroutine(
            Board board,
            StrategyPlanner strategyPlanner,
            Action onComplete
        )
        {
            // Execute the next strategy using the strategy planner's coroutine
            yield return StartCoroutine(strategyPlanner.ExecuteNextStrategyCoroutine(board));

            // After the AI has finished its turn, call the completion callback
            onComplete?.Invoke();
        }

        public void NextTurn()
        {
            matchManager.NextTurn();
            // UpdateUI is no longer needed here as all UI updates will be handled by event callbacks
            // UpdateUI();
        }

        private void OnDestroy()
        {
            if (matchManager != null)
            {
                // Unsubscribe from MatchManager events
                matchManager.OnCardDeployed -= HandleCardDeployed;
                matchManager.OnCardDrawn -= HandleCardDrawn;
                matchManager.OnCardDiscarded -= HandleCardDiscarded;
                matchManager.OnProcessAITurn -= HandleProcessAITurn;

                // Unsubscribe from turn events
                matchManager.OnTurnStarted -= HandleTurnStarted;
                matchManager.OnTurnEnded -= HandleTurnEnded;

                // Unsubscribe from match events
                matchManager.OnMatchStarted -= HandleMatchStarted;
                matchManager.OnMatchEnded -= HandleMatchEnded;
                matchManager.OnAttackCompleted -= HandleAttackCompleted;
                matchManager.OnCardDied -= HandleCardDied;
            }
        }

        // Event handlers
        private void HandleCardDeployed(Card card, int position)
        {
            Debug.Log(
                $"[MatchView] HandleCardDeployed: {card.Title} at position {position} by {matchManager.CurrentPlayerId}"
            );

            // Determine if this is an opponent deployment
            bool isOpponent = matchManager.CurrentPlayerId == matchManager.Opponent.Id;

            // Get the appropriate slot transform
            Transform slotTransform = isOpponent
                ? opponentBattlefieldSlots[position]
                : battlefieldSlots[position];

            // Check if there's already a CardView in this slot
            if (IsSlotOccupied(slotTransform))
            {
                Debug.Log(
                    $"[MatchView] Card {card.Title} already in slot {position} (isOpponent: {isOpponent})"
                );
                return;
            }

            // Try to move the existing card UI from hand to battlefield if it exists
            if (cardUIElements.TryGetValue(card, out GameObject existingCardUI))
            {
                MoveCardToBattlefield(card, existingCardUI, slotTransform, isOpponent);
            }
            else
            {
                Debug.LogError(
                    $"[MatchView] When deploying card {card.Title} at position {position} (isOpponent: {isOpponent}), no existing UI element found"
                );

                // If no existing UI element was found, log an error
                Debug.LogError(
                    $"[MatchView] Error: CreateCardInBattlefield called for {card.Title} in slot {position}. "
                        + "Cards should be moved from hand to battlefield, not created directly. "
                        + "This is a logic error that should be fixed."
                );
            }
        }

        // Helper method to check if a slot is occupied
        private bool IsSlotOccupied(Transform slotTransform)
        {
            for (int i = 0; i < slotTransform.childCount; i++)
            {
                if (slotTransform.GetChild(i).GetComponent<CardView>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        // Helper method to move an existing card to the battlefield
        private void MoveCardToBattlefield(
            Card card,
            GameObject cardGO,
            Transform slotTransform,
            bool isOpponent
        )
        {
            Debug.Log(
                $"[MatchView] Moving {(isOpponent ? "opponent" : "player")} card UI from hand to battlefield: {card.Title}"
            );

            // First remove it from the dictionary to avoid duplicate entries
            cardUIElements.Remove(card);

            // Make sure the card is active (it might have been hidden during drag)
            cardGO.SetActive(true);

            // Move the card to the battlefield slot
            cardGO.transform.SetParent(slotTransform, false);
            cardGO.transform.localPosition = Vector3.zero;
            cardGO.transform.localScale = Vector3.one;

            // Configure the card for battlefield
            ConfigureCardForBattlefield(card, cardGO, isOpponent);

            // Add it back to the dictionary with the updated GameObject
            cardUIElements[card] = cardGO;
        }

        // Helper method to create a new card in the battlefield
        private void CreateCardInBattlefield(
            Card card,
            Transform slotTransform,
            int position,
            bool isOpponent
        )
        {
            // This method should not be used - cards should be moved from hand to battlefield
            Debug.LogError(
                $"[MatchView] Error: CreateCardInBattlefield called for {card.Title} in slot {position}. "
                    + "Cards should be moved from hand to battlefield, not created directly. "
                    + "This is a logic error that should be fixed."
            );
        }

        private void HandleCardDrawn(Card card)
        {
            Debug.Log("[MatchView] HandleCardDrawn: " + card.Title);

            // Determine if this is an opponent card
            bool isOpponent = card.Owner.Id == matchManager.Opponent.Id;
            Transform parent = isOpponent ? opponentHandArea : handArea;

            // Create UI for the new card directly
            var cardGO = CreateCardUI(card, parent, card.FaceDown);
            if (cardGO != null)
            {
                // Set draggable only for player's hand cards
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.UpdateUI();
                }
            }

            // Update credits display
            UpdateCreditsDisplay();
        }

        private void HandleCardDiscarded(Card card)
        {
            Debug.Log("[MatchView] HandleCardDiscarded: " + card.Title);

            // Remove the card UI
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                Destroy(cardGO);
                cardUIElements.Remove(card);
            }

            // Update credits display
            UpdateCreditsDisplay();
        }

        // UpdateHandPositions method removed as it's no longer needed with layout groups

        // Public methods for BattlefieldDropZone
        public bool CanDeployUnitCard(Card card)
        {
            if (matchManager == null)
            {
                Debug.LogError("[MatchView] MatchManager is null");
                return false;
            }

            // Check game rules via match manager
            return matchManager.CanDeployUnitCard(card);
        }

        public bool CanDeployOrderCard(Card card)
        {
            if (matchManager == null)
            {
                Debug.LogError("[MatchView] MatchManager is null");
                return false;
            }

            // Check game rules via match manager
            return matchManager.CanDeployOrderCard(card);
        }

        // For backward compatibility
        public bool CanDeployCard(Card card)
        {
            if (card == null)
                return false;

            return card.CardType.Category == CardCategory.Unit ? CanDeployUnitCard(card)
                : card.CardType.Category == CardCategory.Order ? CanDeployOrderCard(card)
                : false;
        }

        public bool DeployUnitCard(Card card, int slotIndex)
        {
            Debug.Log(
                $"[MatchView] Attempting to deploy unit card {card.Title} to position {slotIndex}"
            );

            // Check if the card can be deployed
            if (!CanDeployUnitCard(card))
            {
                Debug.LogWarning($"[MatchView] Cannot deploy unit card {card.Title}");
                return false;
            }

            // Get the existing card GameObject before deployment
            GameObject cardGO = null;
            if (!cardUIElements.TryGetValue(card, out cardGO) || cardGO == null)
            {
                Debug.LogWarning($"[MatchView] Card UI element not found for {card.Title}");
                // This shouldn't happen, but if it does, we'll let UpdateUI create a new one
            }

            // Deploy the card in the game model
            bool success = matchManager.DeployUnitCard(card, slotIndex);
            if (!success)
            {
                Debug.LogWarning(
                    $"[MatchView] Failed to deploy unit card {card.Title} at position {slotIndex}"
                );
                return false;
            }

            Debug.Log(
                $"[MatchView] Successfully deployed unit card {card.Title} at position {slotIndex}"
            );

            // If we have the card GameObject, move it to the battlefield slot
            if (cardGO != null)
            {
                // Get the target slot
                Transform slotTransform = battlefieldSlots[slotIndex];

                // Move the card to the battlefield slot
                cardGO.transform.SetParent(slotTransform, false);
                cardGO.transform.localPosition = Vector3.zero;
                cardGO.transform.localScale = Vector3.one;

                // Update the card's drag handlers
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    // Disable the DeployDragHandler
                    var deployDragHandler = cardGO.GetComponent<DeployDragHandler>();
                    if (deployDragHandler != null)
                    {
                        deployDragHandler.enabled = false;
                    }

                    // Enable the AbilityDragHandler if it exists, or add it if it doesn't
                    var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                    if (abilityDragHandler == null)
                    {
                        abilityDragHandler = cardGO.AddComponent<AbilityDragHandler>();
                        Debug.Log($"[MatchView] Added AbilityDragHandler to {card.Title}");
                    }

                    // Make sure the AbilityDragHandler is enabled
                    abilityDragHandler.enabled = true;
                    Debug.Log($"[MatchView] Enabled AbilityDragHandler on {card.Title}");

                    // Force the AbilityDragHandler to update its state
                    abilityDragHandler.SendMessage(
                        "UpdateComponentState",
                        null,
                        SendMessageOptions.DontRequireReceiver
                    );

                    // Update the card's UI
                    cardView.UpdateUI();
                }

                Debug.Log($"[MatchView] Moved card {card.Title} to battlefield slot {slotIndex}");
            }
            else
            {
                // If we don't have the card GameObject, update the UI to create it
                Debug.LogError(
                    $"[MatchView] Error: Card GameObject not found for {card.Title}. This is a logic error that should be fixed."
                );
                return false;
            }

            // Clear any highlights that might be active
            ClearAllHighlights();

            // Update credits display
            UpdateCreditsDisplay();

            return true;
        }

        // For backward compatibility
        public bool DeployCard(Card card, int slotIndex)
        {
            if (card == null)
                return false;

            return card.CardType.Category == CardCategory.Unit ? DeployUnitCard(card, slotIndex)
                : card.CardType.Category == CardCategory.Order ? DeployOrderCard(card)
                : false;
        }

        /// <summary>
        /// Handles the deployment of an Order card.
        /// Order cards are placed in the orderArea, their effect is played, and then they are discarded.
        /// </summary>
        /// <param name="card">The Order card to deploy</param>
        /// <returns>True if the deployment was successful, false otherwise</returns>
        public bool DeployOrderCard(Card card)
        {
            Debug.Log($"[MatchView] Attempting to deploy Order card {card.Title}");

            // Check if it's actually an Order card
            if (card.CardType.Category != CardCategory.Order)
            {
                Debug.LogWarning($"[MatchView] Card {card.Title} is not an Order card");
                return false;
            }

            // Check if the card can be deployed
            if (!CanDeployOrderCard(card))
            {
                Debug.LogWarning($"[MatchView] Cannot deploy Order card {card.Title}");
                return false;
            }

            // Get the existing card GameObject before deployment
            GameObject cardGO = null;
            if (!cardUIElements.TryGetValue(card, out cardGO) || cardGO == null)
            {
                Debug.LogError(
                    $"[MatchView] Error: Card UI element not found for Order card {card.Title}. "
                        + "This is a logic error that should be fixed."
                );
                return false;
            }
            else
            {
                // Move the existing card to the order area
                cardGO.transform.SetParent(orderArea, false);
                cardGO.transform.localPosition = Vector3.zero;
                cardGO.transform.localScale = Vector3.one;

                // Update the card's UI
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.UpdateUI();
                }

                Debug.Log($"[MatchView] Moved Order card {card.Title} to order area");
            }

            // Deploy the card in the game model (this will trigger its effect and discard it)
            bool success = matchManager.DeployOrderCard(card);
            if (!success)
            {
                Debug.LogWarning($"[MatchView] Failed to deploy Order card {card.Title}");
                return false;
            }

            Debug.Log($"[MatchView] Successfully deployed Order card {card.Title}");

            // Play an animation or effect for the Order card
            StartCoroutine(PlayOrderCardEffect(cardGO));

            // Clear any highlights that might be active
            ClearAllHighlights();

            // Update credits display
            UpdateCreditsDisplay();

            return true;
        }

        /// <summary>
        /// Plays an animation for the Order card effect and then destroys the card GameObject.
        /// </summary>
        private System.Collections.IEnumerator PlayOrderCardEffect(GameObject orderCardGO)
        {
            if (orderCardGO == null)
                yield break;

            // Play some animation or effect here
            // For now, we'll just scale the card up and down
            float duration = 1.5f;
            float elapsed = 0f;
            Vector3 originalScale = orderCardGO.transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;

            // Scale up
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2);
                orderCardGO.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2);
                orderCardGO.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            // Wait a moment before destroying the card
            yield return new WaitForSeconds(0.5f);

            // After the animation, update the UI to reflect the changes
            // This will ensure the card is removed from the hand and properly placed in the discard pile
            UpdateUI();
        }

        private void RemoveCard(int slotIndex, bool isOpponent = false)
        {
            // Check if the slot index is valid
            if (slotIndex < 0 || slotIndex >= Player.BATTLEFIELD_SLOT_COUNT)
            {
                Debug.LogError($"[MatchView] Invalid slot index: {slotIndex}");
                return;
            }

            var slots = isOpponent ? opponentBattlefieldSlots : battlefieldSlots;

            // Check if the slots array is initialized
            if (slots == null)
            {
                Debug.LogError(
                    $"[MatchView] Battlefield slots array is null for {(isOpponent ? "opponent" : "player")}"
                );
                return;
            }

            // Check if the slot at the given index exists
            if (slots[slotIndex] == null)
            {
                Debug.LogError(
                    $"[MatchView] Slot at index {slotIndex} is null for {(isOpponent ? "opponent" : "player")}"
                );
                return;
            }

            Transform slotTransform = slots[slotIndex];
            Debug.Log(
                $"[MatchView] Attempting to remove card from slot {slotIndex} (isOpponent: {isOpponent})"
            );

            // Find and remove only the CardView component
            for (int i = 0; i < slotTransform.childCount; i++)
            {
                Transform child = slotTransform.GetChild(i);
                if (child != null && child.GetComponent<CardView>() != null)
                {
                    Debug.Log(
                        $"[MatchView] Removing card from slot {slotIndex} (isOpponent: {isOpponent})"
                    );
                    Destroy(child.gameObject);
                    return; // Only remove one card
                }
            }

            Debug.Log($"[MatchView] No card found in slot {slotIndex} (isOpponent: {isOpponent})");
        }

        private void ClearBattlefield()
        {
            for (int i = 0; i < battlefieldSlots.Length; i++)
            {
                RemoveCard(i, false);
                RemoveCard(i, true);
            }
        }

        /// <summary>
        /// Full UI refresh - should only be used for initial setup or when a complete refresh is necessary.
        /// For normal gameplay, use the specific update methods instead.
        /// </summary>
        public void UpdateUI()
        {
            if (matchManager == null)
                return;

            Debug.Log("[MatchView] Full UI refresh called");

            // Update turn and credits displays
            UpdateTurnDisplay();
            UpdateCreditsDisplay();

            // Player1 is always at bottom, Player2 (opponent) always at top
            var playerState = matchManager.Player;
            var opponentState = matchManager.Opponent;

            // Collect all cards that should have UI elements
            var allActiveCards = new HashSet<Card>();

            // Add cards from player's hand
            var playerHand = playerState.Hand;
            foreach (var card in playerHand)
            {
                allActiveCards.Add(card);
            }

            // Add cards from opponent's hand
            var opponentHand = opponentState.Hand;
            foreach (var card in opponentHand)
            {
                allActiveCards.Add(card);
            }

            // Add cards from player's battlefield
            var battlefield = playerState.Battlefield;
            foreach (var card in battlefield)
            {
                if (card != null)
                {
                    allActiveCards.Add(card);
                }
            }

            // Add cards from opponent's battlefield
            var opponentBattlefield = opponentState.Battlefield;
            foreach (var card in opponentBattlefield)
            {
                if (card != null)
                {
                    allActiveCards.Add(card);
                }
            }

            // Remove UI elements for cards that are no longer active
            foreach (var cardEntry in cardUIElements.ToList())
            {
                if (!allActiveCards.Contains(cardEntry.Key))
                {
                    Debug.Log(
                        $"[MatchView] Removing UI element for card {cardEntry.Key.Title} as it's no longer active"
                    );
                    Destroy(cardEntry.Value);
                    cardUIElements.Remove(cardEntry.Key);
                }
            }

            // Update player's battlefield (bottom)
            UpdateBattlefield(battlefield, battlefieldSlots, false);

            // Update opponent's battlefield (top)
            UpdateBattlefield(opponentBattlefield, opponentBattlefieldSlots, true);

            // Update player's hand (bottom)
            UpdateHand(playerHand, handArea, false);

            // Update opponent's hand (top, face down)
            UpdateHand(opponentHand, opponentHandArea, true);
        }

        private void UpdateBattlefield(
            IReadOnlyList<Card> battlefield,
            Transform[] slots,
            bool isOpponent
        )
        {
            for (int i = 0; i < slots.Length; i++)
            {
                // Check if there's a card in the data model
                if (i < battlefield.Count && battlefield[i] != null)
                {
                    Card card = battlefield[i];
                    Transform slotTransform = slots[i];

                    // Check if there's already a CardView in this slot
                    CardView existingCardView = null;
                    for (int j = 0; j < slotTransform.childCount; j++)
                    {
                        CardView cardView = slotTransform.GetChild(j).GetComponent<CardView>();
                        if (cardView != null)
                        {
                            existingCardView = cardView;
                            break;
                        }
                    }

                    // Check if the card UI element already exists somewhere else
                    GameObject cardGO = null;
                    bool cardExists =
                        cardUIElements.TryGetValue(card, out cardGO) && cardGO != null;

                    // If there's a card in the slot but it's not the right one, remove it
                    if (existingCardView != null && existingCardView.Card != card)
                    {
                        Debug.Log($"[MatchView] Removing incorrect card from slot {i}");
                        Destroy(existingCardView.gameObject);
                        existingCardView = null;
                    }

                    // If the card exists elsewhere in the UI, move it to this slot
                    if (
                        cardExists
                        && (existingCardView == null || existingCardView.gameObject != cardGO)
                    )
                    {
                        Debug.Log(
                            $"[MatchView] Moving card {card.Title} to battlefield slot {i} (isOpponent: {isOpponent})"
                        );
                        cardGO.transform.SetParent(slotTransform, false);
                        cardGO.transform.localPosition = Vector3.zero;
                        cardGO.transform.localScale = Vector3.one;

                        // Update the card's UI and configure it for battlefield
                        CardView cardView = cardGO.GetComponent<CardView>();
                        if (cardView != null)
                        {
                            // Configure the card for battlefield
                            ConfigureCardForBattlefield(card, cardGO, isOpponent);

                            // Update the card's UI
                            cardView.UpdateUI();

                            // Make sure the face-down state is correct for opponent cards
                            if (isOpponent)
                            {
                                cardView.SetFaceDown(card.FaceDown);
                            }
                        }
                    }
                    // If the card doesn't exist anywhere, log an error
                    else if (!cardExists && existingCardView == null)
                    {
                        // This is an error case - we should not be creating new cards here
                        // Cards should be moved from hand to battlefield, not created directly in battlefield
                        Debug.LogError(
                            $"[MatchView] Error: Card {card.Title} not found in UI elements but is in battlefield slot {i}. "
                                + "Cards should be moved from hand to battlefield, not created directly."
                        );
                    }
                    // If the card is already in the right slot, just update it
                    else if (existingCardView != null)
                    {
                        Debug.Log(
                            $"[MatchView] Updating existing card UI for {card.Title} in battlefield slot {i}"
                        );
                        // Make sure the face-down state is correct for opponent cards
                        if (isOpponent)
                        {
                            existingCardView.SetFaceDown(card.FaceDown);
                        }
                        existingCardView.UpdateUI();
                    }
                }
                // If there's no card in the data model but there's a UI element, remove it
                else
                {
                    RemoveCard(i, isOpponent);
                }
            }
        }

        private void UpdateHand(IReadOnlyList<Card> hand, Transform parent, bool isOpponent)
        {
            // First, collect all cards that should be in this hand
            HashSet<Card> cardsInHand = new HashSet<Card>(hand);

            // Remove any cards that shouldn't be in this hand
            List<Transform> cardsToRemove = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                CardView cardView = child.GetComponent<CardView>();
                if (cardView != null && !cardsInHand.Contains(cardView.Card))
                {
                    cardsToRemove.Add(child);
                }
            }

            // Remove the cards that shouldn't be in this hand
            foreach (Transform child in cardsToRemove)
            {
                CardView cardView = child.GetComponent<CardView>();
                if (cardView != null)
                {
                    Debug.Log($"[MatchView] Removing card {cardView.Card.Title} from hand");
                    Destroy(child.gameObject);
                }
            }

            // Now add or update cards that should be in the hand
            for (int i = 0; i < hand.Count; i++)
            {
                Card card = hand[i];

                // Check if the card is already in the hand
                bool foundInHand = false;
                for (int j = 0; j < parent.childCount; j++)
                {
                    CardView cardView = parent.GetChild(j).GetComponent<CardView>();
                    if (cardView != null && cardView.Card == card)
                    {
                        // Card is already in the hand, just update it
                        Debug.Log($"[MatchView] Card {card.Title} is already in hand, updating");

                        // Make sure the face-down state is correct for opponent cards
                        if (isOpponent)
                        {
                            cardView.SetFaceDown(true); // Opponent hand cards are always face down
                        }
                        else
                        {
                            cardView.SetFaceDown(card.FaceDown);
                        }

                        cardView.UpdateUI();
                        foundInHand = true;
                        break;
                    }
                }

                // If the card isn't in the hand, check if it exists elsewhere
                if (!foundInHand)
                {
                    GameObject cardGO = null;
                    bool cardExists =
                        cardUIElements.TryGetValue(card, out cardGO) && cardGO != null;

                    if (cardExists)
                    {
                        // Move the existing card to the hand
                        Debug.Log(
                            $"[MatchView] Moving card {card.Title} to hand (isOpponent: {isOpponent})"
                        );
                        cardGO.transform.SetParent(parent, false);

                        // Update the card's UI
                        CardView cardView = cardGO.GetComponent<CardView>();
                        if (cardView != null)
                        {
                            // Make sure the face-down state is correct
                            if (isOpponent)
                            {
                                cardView.SetFaceDown(true); // Opponent hand cards are always face down
                            }
                            else
                            {
                                cardView.SetFaceDown(card.FaceDown);
                            }

                            cardView.UpdateUI();

                            // Update drag handlers for hand cards
                            if (!isOpponent)
                            {
                                // Enable DeployDragHandler
                                var deployDragHandler = cardGO.GetComponent<DeployDragHandler>();
                                if (deployDragHandler == null)
                                {
                                    deployDragHandler = cardGO.AddComponent<DeployDragHandler>();
                                }
                                deployDragHandler.enabled = true;

                                // Disable AbilityDragHandler
                                var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                                if (abilityDragHandler != null)
                                {
                                    abilityDragHandler.enabled = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        // This is an error case - we should not be creating new cards here
                        Debug.LogError(
                            $"[MatchView] Error: Card {card.Title} not found in UI elements but is in hand. "
                                + "This is a logic error that should be fixed."
                        );
                    }
                }
            }
        }

        private GameObject CreateCardUI(Card card, Transform parent, bool faceDown)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[MatchView] Card prefab is null");
                return null;
            }

            // Check if the card already exists in our dictionary
            GameObject existingCardGO = null;
            if (cardUIElements.TryGetValue(card, out existingCardGO) && existingCardGO != null)
            {
                Debug.Log(
                    $"[MatchView] Card {card.Title} already exists in UI elements dictionary, moving it to {parent.name}"
                );
                existingCardGO.transform.SetParent(parent, false);
                existingCardGO.transform.localPosition = Vector3.zero;
                existingCardGO.transform.localScale = Vector3.one;

                // Update the face-down state
                CardView existingCardView = existingCardGO.GetComponent<CardView>();
                if (existingCardView != null)
                {
                    existingCardView.SetFaceDown(faceDown);
                    existingCardView.UpdateUI();
                }

                return existingCardGO;
            }

            // Use the CardView's static factory method
            var cardView = CardView.CreateCard(card, parent, faceDown, cardPrefab);
            if (cardView != null)
            {
                // Store the card in our dictionary for later reference
                cardUIElements[card] = cardView.gameObject;
                Debug.Log(
                    $"[MatchView] Created new card UI for {card.Title} and added to dictionary"
                );
                return cardView.gameObject;
            }
            else
            {
                Debug.LogError($"[MatchView] Failed to create card UI for {card.Title}");
            }

            return null;
        }

        private void UpdateCreditsDisplay()
        {
            if (matchManager == null)
                return;

            // Player1 is always at bottom, Player2 (opponent) always at top
            var playerState = matchManager.Player;
            var opponentState = matchManager.Opponent;

            // Update credits display
            if (creditsText != null)
            {
                creditsText.text = $"Kredits: {playerState.Credits}";
            }
            if (opponentCreditsText != null)
            {
                opponentCreditsText.text = $"Kredits: {opponentState.Credits}";
            }
        }

        // Get the current player
        public Player GetCurrentPlayer()
        {
            if (matchManager == null)
            {
                Debug.LogError("[MatchView] MatchManager is null");
                return null;
            }
            return matchManager.Player;
        }

        // Get the opponent
        public Player GetOpponent()
        {
            if (matchManager == null)
            {
                Debug.LogError("[MatchView] MatchManager is null");
                return null;
            }
            return matchManager.Opponent;
        }

        // Get the MatchManager instance
        public MatchManager GetMatchManager()
        {
            return matchManager;
        }

        // Handle card attacks
        public void AttackCard(Card attackerCard, Card defenderCard)
        {
            if (matchManager == null)
            {
                Debug.LogError("[MatchView] Cannot attack: MatchManager is null");
                return;
            }

            // Process the attack using MatchManager directly
            matchManager.ProcessAttack(attackerCard, defenderCard);

            // Update the UI to reflect any changes
            UpdateUI();
        }

        // Handle attack completed event
        private void HandleAttackCompleted(
            Card attacker,
            Card defender,
            int attackDamage,
            int counterDamage
        )
        {
            Debug.Log(
                $"[MatchView] Attack completed: {attacker.Title} dealt {attackDamage} damage to {defender.Title}, received {counterDamage} counter damage"
            );

            // Update the UI to reflect the attack results
            UpdateUI();
        }

        // Handle card died event
        private void HandleCardDied(Card card)
        {
            Debug.Log($"[MatchView] Card died: {card.Title}");

            // Update the UI to reflect the card death
            UpdateUI();

            // Play death animation or sound effect if needed
            // TODO: Add death animation or sound effect
        }

        // Helper method to configure a card for the battlefield
        private void ConfigureCardForBattlefield(Card card, GameObject cardGO, bool isOpponent)
        {
            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                // Just reflect the model state, don't modify it
                cardView.SetFaceDown(card.FaceDown);

                // For player cards, we want to enable dragging to use abilities
                // For opponent cards, we want to disable dragging
                cardView.SetDraggable(!isOpponent);

                // Explicitly reset the isDragging flag to ensure the card can be clicked
                cardView.ResetDraggingState();

                // Switch from DeployDragHandler to AbilityDragHandler for battlefield cards
                // Only do this for the player's cards, not opponent cards
                if (!isOpponent)
                {
                    // Disable the DeployDragHandler
                    var deployDragHandler = cardGO.GetComponent<DeployDragHandler>();
                    if (deployDragHandler != null)
                    {
                        deployDragHandler.enabled = false;
                    }

                    // Enable the AbilityDragHandler if it exists, or add it if it doesn't
                    var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                    if (abilityDragHandler == null)
                    {
                        abilityDragHandler = cardGO.AddComponent<AbilityDragHandler>();
                        Debug.Log($"[MatchView] Added AbilityDragHandler to {card.Title}");
                    }

                    // Make sure the AbilityDragHandler is enabled
                    abilityDragHandler.enabled = true;
                    Debug.Log($"[MatchView] Enabled AbilityDragHandler on {card.Title}");

                    // Force the AbilityDragHandler to update its state
                    abilityDragHandler.SendMessage(
                        "UpdateComponentState",
                        null,
                        SendMessageOptions.DontRequireReceiver
                    );
                }
            }

            // Ensure the CanvasGroup's blocksRaycasts is enabled
            var canvasGroup = cardGO.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                Debug.Log(
                    $"[MatchView] Ensuring CanvasGroup.blocksRaycasts is true for {card.Title}"
                );
            }

            // Ensure all images have raycastTarget enabled
            EnableRaycastsOnImages(card, cardGO, isOpponent);
        }

        // Helper method to enable raycasts on all images
        private void EnableRaycastsOnImages(Card card, GameObject cardGO, bool isOpponent)
        {
            var images = cardGO.GetComponentsInChildren<Image>();
            Debug.Log(
                $"[MatchView] Card {card.Title} in {(isOpponent ? "opponent" : "player")} battlefield has {images.Length} Image components"
            );

            foreach (var img in images)
            {
                Debug.Log($"[MatchView] Image '{img.name}' raycastTarget: {img.raycastTarget}");
                // Ensure raycastTarget is enabled for all images
                img.raycastTarget = true;
            }
        }

        private void ClearAllHighlights()
        {
            // Clear PlayerCardSlot highlights
            var playerSlots = GetComponentsInChildren<PlayerCardSlot>(true);
            foreach (var slot in playerSlots)
            {
                slot.SetHighlight(false);
            }

            // Clear OpponentCardSlot highlights if they exist
            var opponentSlots = GetComponentsInChildren<OpponentCardSlot>(true);
            foreach (var slot in opponentSlots)
            {
                slot.SetHighlight(false);
            }

            // Clear OrderDropHandler highlight if it exists
            var orderDropHandler = GetComponentInChildren<OrderDropHandler>(true);
            if (orderDropHandler != null)
            {
                orderDropHandler.SetHighlight(false);
            }

            Debug.Log("[MatchView] Cleared all highlights");
        }
    }
}
