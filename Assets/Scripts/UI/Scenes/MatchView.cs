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

        [Header("Layout Settings")]
        // Removed cardSpacing as it's no longer needed - layout groups handle spacing

        // Non-serialized fields
        private Dictionary<Card, GameObject> cardUIElements = new();
        private Transform[] battlefieldSlots = new Transform[Player.BATTLEFIELD_SLOT_COUNT];
        private Transform[] opponentBattlefieldSlots = new Transform[Player.BATTLEFIELD_SLOT_COUNT];

        [SerializeField]
        private CardDetailView cardDetailView; // Reference to the CardDetailView

        private List<CardType> cards = new();

        private void Awake()
        {
            // First try to use the serialized reference
            if (cardDetailView == null)
            {
                // Try to find CardPanel in the scene
                var cardPanel = GameObject.Find("CardPanel");
                if (cardPanel != null)
                {
                    cardDetailView = cardPanel.GetComponent<CardDetailView>();
                    Debug.Log("[MatchView] Found CardDetailView on CardPanel");
                }

                // If still not found, try finding it anywhere in the scene
                if (cardDetailView == null)
                {
                    cardDetailView = FindObjectOfType<CardDetailView>(true);
                    if (cardDetailView == null)
                    {
                        Debug.LogError(
                            "[MatchView] CardDetailView not found in scene. Please ensure CardPanel has CardDetailView component."
                        );
                        return;
                    }
                }
            }

            Debug.Log($"[MatchView] Found CardDetailView on: {cardDetailView.gameObject.name}");

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
                // If no existing UI element was found, create a new one
                CreateCardInBattlefield(card, slotTransform, position, isOpponent);
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
            cardGO.transform.SetParent(slotTransform);
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
            Debug.Log(
                $"[MatchView] Creating new {(isOpponent ? "opponent" : "player")} card UI for slot {position}: {card.Title}"
            );

            // Create UI based on the current state of the card
            var cardGO = CreateCardUI(card, slotTransform, card.FaceDown);
            if (cardGO != null)
            {
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.transform.localPosition = Vector3.zero;
                    cardView.transform.localScale = Vector3.one;

                    // Configure the card for battlefield
                    ConfigureCardForBattlefield(card, cardGO, isOpponent);

                    Debug.Log(
                        $"[MatchView] Successfully created {(isOpponent ? "opponent" : "player")} card UI for slot {position}"
                    );
                }
                else
                {
                    Debug.LogError(
                        $"[MatchView] Failed to get CardView component for {(isOpponent ? "opponent" : "player")} card in slot {position}"
                    );
                }
            }
            else
            {
                Debug.LogError(
                    $"[MatchView] Failed to create card UI for {(isOpponent ? "opponent" : "player")} card in slot {position}"
                );
            }
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
                    cardView.SetDraggable(!isOpponent);
                }

                // No need to manually position - the layout group will handle it
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
        public bool CanDeployCard(Card card)
        {
            if (matchManager == null)
            {
                Debug.LogError("[MatchView] MatchManager is null");
                return false;
            }

            // Check game rules via match manager
            return matchManager.CanDeployCard(card);
        }

        public bool DeployCard(Card card, int slotIndex)
        {
            Debug.Log(
                $"[MatchView] Attempting to deploy card {card.Title} to position {slotIndex}"
            );

            // Check if the card can be deployed
            if (!CanDeployCard(card))
            {
                Debug.LogWarning($"[MatchView] Cannot deploy card {card.Title}");
                return false;
            }

            // Deploy the card in the game model
            bool success = matchManager.DeployCard(card, slotIndex);
            if (!success)
            {
                Debug.LogWarning(
                    $"[MatchView] Failed to deploy card {card.Title} at position {slotIndex}"
                );
                return false;
            }

            Debug.Log(
                $"[MatchView] Successfully deployed card {card.Title} at position {slotIndex}"
            );

            // Update only the credits display instead of full UI refresh
            UpdateCreditsDisplay();

            // Make sure the card has the correct drag handlers enabled
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    // Ensure AbilityDragHandler is added and enabled for battlefield cards
                    var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                    if (abilityDragHandler == null)
                    {
                        abilityDragHandler = cardGO.AddComponent<AbilityDragHandler>();
                        Debug.Log(
                            $"[MatchView] Added AbilityDragHandler to deployed card {card.Title}"
                        );
                    }
                    abilityDragHandler.enabled = true;

                    // Disable DeployDragHandler for battlefield cards
                    var deployDragHandler = cardGO.GetComponent<DeployDragHandler>();
                    if (deployDragHandler != null)
                    {
                        deployDragHandler.enabled = false;
                    }

                    Debug.Log(
                        $"[MatchView] Updated drag handlers for deployed card {card.Title}: AbilityDragHandler enabled, DeployDragHandler disabled"
                    );
                }
            }

            return true;
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
                    // Check if there's already a CardView in this slot
                    bool hasCardView = false;
                    Transform slotTransform = slots[i];
                    for (int j = 0; j < slotTransform.childCount; j++)
                    {
                        if (slotTransform.GetChild(j).GetComponent<CardView>() != null)
                        {
                            hasCardView = true;
                            break;
                        }
                    }

                    // If there's no UI element for this card, create one
                    if (!hasCardView)
                    {
                        // Create UI based on the current state of the card
                        var cardGO = CreateCardUI(
                            battlefield[i],
                            slotTransform,
                            battlefield[i].FaceDown
                        );
                        if (cardGO != null)
                        {
                            var cardView = cardGO.GetComponent<CardView>();
                            if (cardView != null)
                            {
                                cardView.transform.localPosition = Vector3.zero;
                                cardView.transform.localScale = Vector3.one;
                                cardView.SetDraggable(true); // Enable dragging for battlefield cards to allow abilities
                            }
                        }
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
            // First, make sure all existing cards that should be in the hand are properly parented
            for (int i = 0; i < hand.Count; i++)
            {
                Card card = hand[i];

                // Get or create the card UI element
                GameObject cardGO;
                if (!cardUIElements.TryGetValue(card, out cardGO))
                {
                    // Create a new card UI if it doesn't exist
                    cardGO = CreateCardUI(card, parent, card.FaceDown);

                    // Configure it for hand use
                    var cardView = cardGO.GetComponent<CardView>();
                    if (cardView != null)
                    {
                        cardView.SetDraggable(!isOpponent);
                    }
                }
                else
                {
                    // Ensure existing card is properly parented
                    if (cardGO.transform.parent != parent)
                    {
                        cardGO.transform.SetParent(parent, false);
                    }

                    // Update card state
                    var cardView = cardGO.GetComponent<CardView>();
                    if (cardView != null)
                    {
                        cardView.SetFaceDown(card.FaceDown);
                        cardView.SetDraggable(!isOpponent);
                    }
                }

                // No need to manually position - the layout group will handle it
            }
        }

        private GameObject CreateCardUI(Card card, Transform parent, bool faceDown)
        {
            Debug.Log(
                $"[MatchView] Creating card UI: {card.Title}, FaceDown: {faceDown}, Owner: {card.Owner.Id}"
            );
            if (cardPrefab == null)
            {
                Debug.LogError("[MatchView] Card prefab is null");
                return null;
            }

            var cardGO = Instantiate(cardPrefab, parent);
            Debug.Log($"[MatchView] Created card UI: {cardGO.name}, Parent: {parent.name}");

            var cardView = cardGO.GetComponent<CardView>();
            if (cardView == null)
            {
                Debug.LogError($"[MatchView] CardView component missing on prefab: {cardGO.name}");
                return cardGO;
            }

            // Initialize card data first, before any other setup
            Debug.Log($"[MatchView] Initializing card data for {card.Title}");
            cardView.Initialize(card);

            // Ensure the UI reflects the current state of the card
            // Note: faceDown parameter is used only for the UI, not to modify the card's state
            Debug.Log($"[MatchView] Setting face down state to {faceDown} for {card.Title}");
            cardView.SetFaceDown(faceDown);

            // Ensure the card has required UI components
            var rectTransform = cardGO.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"[MatchView] RectTransform missing on card: {cardGO.name}");
                rectTransform = cardGO.AddComponent<RectTransform>();
            }

            var image = cardGO.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError($"[MatchView] Image component missing on card: {cardGO.name}");
                image = cardGO.AddComponent<Image>();
            }
            image.raycastTarget = true;
            Debug.Log($"[MatchView] Set raycastTarget to true for {card.Title}");

            // Set up the appropriate drag handler based on where the card is
            bool isInHand = parent == handArea;
            bool isOnBattlefield = parent == battlefieldArea || parent.IsChildOf(battlefieldArea);

            // For cards in hand, add DeployDragHandler if it doesn't exist
            if (isInHand)
            {
                var deployDragHandler = cardGO.GetComponent<DeployDragHandler>();
                if (deployDragHandler == null)
                {
                    deployDragHandler = cardGO.AddComponent<DeployDragHandler>();
                    Debug.Log($"[MatchView] Added DeployDragHandler to {card.Title}");
                }
                deployDragHandler.enabled = true;

                // Disable AbilityDragHandler for hand cards
                var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                if (abilityDragHandler != null)
                {
                    abilityDragHandler.enabled = false;
                }
            }
            // For cards on battlefield, add AbilityDragHandler if it doesn't exist
            else if (isOnBattlefield)
            {
                var abilityDragHandler = cardGO.GetComponent<AbilityDragHandler>();
                if (abilityDragHandler == null)
                {
                    abilityDragHandler = cardGO.AddComponent<AbilityDragHandler>();
                    Debug.Log($"[MatchView] Added AbilityDragHandler to {card.Title}");
                }
                abilityDragHandler.enabled = true;

                // Disable DeployDragHandler for battlefield cards
                var deployDragHandler = cardGO.GetComponent<DeployDragHandler>();
                if (deployDragHandler != null)
                {
                    deployDragHandler.enabled = false;
                }
            }

            // Set draggable state after initialization - this will be handled by the CardView
            // based on the parent and the card's state
            cardView.SetDraggable(true);
            Debug.Log($"[MatchView] Card UI creation complete for {card.Title}");

            // Note: We don't need to manually set the card back sprite here
            // The CardView component already handles face-down cards with its own cardBackOverlay

            cardUIElements[card] = cardGO;
            return cardGO;
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
    }
}
