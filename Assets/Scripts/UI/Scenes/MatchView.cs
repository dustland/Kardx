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
        private GameObject cardSlotPrefab; // Prefab for card slot

        [SerializeField]
        private GameObject cardPrefab;

        [SerializeField]
        private TextMeshProUGUI turnText;

        [SerializeField]
        private TextMeshProUGUI creditsText;

        [SerializeField]
        private TextMeshProUGUI opponentCreditsText;

        [Header("Layout Settings")]
        [SerializeField]
        private float handCurveHeight = 0.5f;

        [SerializeField]
        private float cardSpacing = 1.2f;

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
                    Debug.Log("[CardCollectionView] Found CardDetailView on CardPanel");
                }

                // If still not found, try finding it anywhere in the scene
                if (cardDetailView == null)
                {
                    cardDetailView = FindObjectOfType<CardDetailView>(true);
                    if (cardDetailView == null)
                    {
                        Debug.LogError(
                            "[CardCollectionView] CardDetailView not found in scene. Please ensure CardPanel has CardDetailView component."
                        );
                        return;
                    }
                }
            }

            Debug.Log(
                $"[CardCollectionView] Found CardDetailView on: {cardDetailView.gameObject.name}"
            );
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
                || cardSlotPrefab == null
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
                var slot = Instantiate(cardSlotPrefab, battlefieldArea);
                slot.name = $"CardSlot{i + 1}";
                battlefieldSlots[i] = slot.transform;
                var cardSlot = slot.GetComponent<CardSlot>();
                if (cardSlot != null)
                {
                    cardSlot.SetPosition(i);
                }
            }

            // Create opponent's battlefield slots
            for (int i = 0; i < Player.BATTLEFIELD_SLOT_COUNT; i++)
            {
                var slot = Instantiate(cardSlotPrefab, opponentBattlefieldArea);
                slot.name = $"OpponentCardSlot{i + 1}";
                opponentBattlefieldSlots[i] = slot.transform;
                var cardSlot = slot.GetComponent<CardSlot>();
                if (cardSlot != null)
                {
                    cardSlot.SetPosition(i);
                }
            }
        }

        private void Start()
        {
            // Create MatchManager instance
            matchManager = new MatchManager(new SimpleLogger("[MatchManager]"));

            // Subscribe to MatchManager events
            matchManager.OnCardDeployed += HandleCardDeployed;
            matchManager.OnCardDrawn += HandleCardDrawn;
            matchManager.OnCardDiscarded += HandleCardDiscarded;

            // Subscribe to turn events
            matchManager.OnTurnStarted += HandleTurnStarted;
            matchManager.OnTurnEnded += HandleTurnEnded;

            // Subscribe to match events
            matchManager.OnMatchStarted += HandleMatchStarted;
            matchManager.OnMatchEnded += HandleMatchEnded;

            // Subscribe to AI turn processing event
            matchManager.OnProcessAITurn += HandleProcessAITurn;

            // Start the battle
            matchManager.StartMatch();
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
            bool hasCardView = false;
            for (int i = 0; i < slotTransform.childCount; i++)
            {
                if (slotTransform.GetChild(i).GetComponent<CardView>() != null)
                {
                    hasCardView = true;
                    break;
                }
            }

            if (!hasCardView)
            {
                // Try to move the existing card UI from hand to battlefield if it exists
                if (cardUIElements.TryGetValue(card, out GameObject existingCardUI))
                {
                    Debug.Log(
                        $"[MatchView] Moving {(isOpponent ? "opponent" : "player")} card UI from hand to battlefield: {card.Title}"
                    );

                    // First remove it from the dictionary to avoid duplicate entries
                    cardUIElements.Remove(card);

                    // Make sure the card is active (it might have been hidden during drag)
                    existingCardUI.SetActive(true);

                    // Move the card to the battlefield slot
                    existingCardUI.transform.SetParent(slotTransform);
                    existingCardUI.transform.localPosition = Vector3.zero;
                    existingCardUI.transform.localScale = Vector3.one;

                    // Make sure the card view reflects the current state of the card
                    var cardView = existingCardUI.GetComponent<CardView>();
                    if (cardView != null)
                    {
                        // Just reflect the model state, don't modify it
                        cardView.SetFaceDown(card.FaceDown);
                        cardView.SetDraggable(false); // Disable dragging once deployed
                    }

                    // Add it back to the dictionary with the updated GameObject
                    cardUIElements[card] = existingCardUI;
                }
                else
                {
                    // If no existing UI element was found, create a new one
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
                            cardView.SetDraggable(false); // Disable dragging once deployed
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
            }
            else
            {
                Debug.Log(
                    $"[MatchView] Card {card.Title} already in slot {position} (isOpponent: {isOpponent})"
                );
            }

            // We don't need to call UpdateUI here anymore since we're directly
            // manipulating the card UI element
        }

        private void HandleCardDrawn(Card card)
        {
            Debug.Log("[MatchView] HandleCardDrawn: " + card.Title);

            // Determine if this is an opponent card
            bool isOpponent = card.OwnerFaction == matchManager.Opponent.Faction;
            Transform parent = isOpponent ? opponentHandArea : handArea;

            // Get the position in hand
            var hand = isOpponent ? matchManager.Opponent.Hand : matchManager.Player.Hand;
            int position = hand.ToList().IndexOf(card);

            if (position >= 0)
            {
                // Create UI for the new card - the card's face-down state should already be set by the game logic
                CreateHandCardUI(card, parent, position, isOpponent);

                // Update credits display
                UpdateCreditsDisplay();
            }
        }

        private void HandleCardDiscarded(Card card)
        {
            Debug.Log("[MatchView] HandleCardDiscarded: " + card.Title);

            // Remove the UI element for the discarded card
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                Debug.Log($"[MatchView] Removing UI element for discarded card: {card.Title}");
                Destroy(cardGO);
                cardUIElements.Remove(card);
            }

            // Update hand positions for remaining cards
            UpdateHandPositions(card.OwnerFaction == matchManager.Opponent.Faction);

            // Update credits display
            UpdateCreditsDisplay();
        }

        private void UpdateHandPositions(bool isOpponent)
        {
            // Get the appropriate hand and parent
            var hand = isOpponent ? matchManager.Opponent.Hand : matchManager.Player.Hand;
            Transform parent = isOpponent ? opponentHandArea : handArea;

            // Update positions for all cards in hand
            for (int i = 0; i < hand.Count; i++)
            {
                if (cardUIElements.TryGetValue(hand[i], out GameObject cardGO))
                {
                    var rectTransform = cardGO.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        float xOffset = i * cardSpacing;
                        float yOffset =
                            parent == handArea ? Mathf.Sin(i * 0.5f) * handCurveHeight : 0;
                        rectTransform.anchoredPosition = new Vector2(xOffset, yOffset);
                    }
                }
            }
        }

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
            if (slotIndex < 0 || slotIndex >= battlefieldSlots.Length)
            {
                Debug.LogError($"Invalid slot index: {slotIndex}");
                return false;
            }

            // Check if the slot is already occupied by looking for a CardView component
            Transform slotTransform = battlefieldSlots[slotIndex];
            bool hasCardView = false;
            for (int i = 0; i < slotTransform.childCount; i++)
            {
                if (slotTransform.GetChild(i).GetComponent<CardView>() != null)
                {
                    hasCardView = true;
                    break;
                }
            }

            if (hasCardView)
            {
                Debug.LogWarning($"Slot {slotIndex} is already occupied");
                return false;
            }

            // Check and deploy via match manager
            // This will trigger the HandleCardDeployed event which will handle the UI update
            if (!matchManager.DeployCard(card, slotIndex))
            {
                Debug.LogWarning("Failed to deploy card via match manager");
                return false;
            }

            // The HandleCardDeployed method will be called by the event system
            // and will handle moving the card UI to the battlefield
            return true;
        }

        private void RemoveCard(int slotIndex, bool isOpponent = false)
        {
            var slots = isOpponent ? opponentBattlefieldSlots : battlefieldSlots;
            Transform slotTransform = slots[slotIndex];

            // Find and remove only the CardView component
            for (int i = 0; i < slotTransform.childCount; i++)
            {
                Transform child = slotTransform.GetChild(i);
                if (child.GetComponent<CardView>() != null)
                {
                    Debug.Log(
                        $"[MatchView] Removing card from slot {slotIndex} (isOpponent: {isOpponent})"
                    );
                    Destroy(child.gameObject);
                    return; // Only remove one card
                }
            }
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

        private void UpdateBattlefield(IReadOnlyList<Card> battlefield, Transform[] slots, bool isOpponent)
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
                                cardView.SetDraggable(false); // Disable dragging for battlefield cards
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
            for (int i = 0; i < hand.Count; i++)
            {
                if (!cardUIElements.ContainsKey(hand[i]))
                {
                    CreateHandCardUI(hand[i], parent, i, isOpponent);
                }
                else
                {
                    // Update the UI if needed
                    if (cardUIElements.TryGetValue(hand[i], out GameObject cardGO))
                    {
                        var cardView = cardGO.GetComponent<CardView>();
                        if (cardView != null)
                        {
                            // Just reflect the model state, don't modify it
                            cardView.SetFaceDown(hand[i].FaceDown);
                            cardView.SetDraggable(!isOpponent);

                            // Update position
                            var rectTransform = cardGO.GetComponent<RectTransform>();
                            if (rectTransform != null)
                            {
                                float xOffset = i * cardSpacing;
                                float yOffset =
                                    parent == handArea ? Mathf.Sin(i * 0.5f) * handCurveHeight : 0;
                                rectTransform.anchoredPosition = new Vector2(xOffset, yOffset);
                            }
                        }
                    }
                }
            }
        }

        private GameObject CreateHandCardUI(
            Card card,
            Transform parent,
            int position,
            bool isOpponent
        )
        {
            // Note: The card's face-down state should already be set by the game logic
            // We just create the UI to reflect that state

            var cardGO = CreateCardUI(card, parent, card.FaceDown);
            if (cardGO != null)
            {
                var rectTransform = cardGO.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    float xOffset = position * cardSpacing;
                    float yOffset =
                        parent == handArea ? Mathf.Sin(position * 0.5f) * handCurveHeight : 0;
                    rectTransform.anchoredPosition = new Vector2(xOffset, yOffset);
                }

                // Set draggable only for player's hand cards
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.SetDraggable(parent == handArea);
                }
            }
            return cardGO;
        }

        private GameObject CreateCardUI(Card card, Transform parent, bool faceDown)
        {
            Debug.Log("[MatchView] Creating card UI: " + card.Title);
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
            cardView.Initialize(card);

            // Ensure the UI reflects the current state of the card
            // Note: faceDown parameter is used only for the UI, not to modify the card's state
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

            // Set draggable state after initialization
            cardView.SetDraggable(true);

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
                creditsText.text = $"Credits: {playerState.Credits}";
            }
            if (opponentCreditsText != null)
            {
                opponentCreditsText.text = $"Credits: {opponentState.Credits}";
            }
        }
    }
}
