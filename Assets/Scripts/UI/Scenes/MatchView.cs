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

        [Header("Card Settings")]
        [SerializeField]
        private Sprite cardBackSprite;

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
            matchManager = new MatchManager(new SimpleLogger("[MatchView]"));

            // Subscribe to MatchManager events
            matchManager.OnCardDeployed += HandleCardDeployed;
            matchManager.OnCardDrawn += HandleCardDrawn;
            matchManager.OnCardDiscarded += HandleCardDiscarded;

            // Subscribe to AI turn processing event
            matchManager.OnProcessAITurn += HandleProcessAITurn;

            // Start the battle
            matchManager.StartMatch();
            UpdateUI();
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
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (matchManager != null)
            {
                // Unsubscribe from BattleManager events
                matchManager.OnCardDeployed -= HandleCardDeployed;
                matchManager.OnCardDrawn -= HandleCardDrawn;
                matchManager.OnCardDiscarded -= HandleCardDiscarded;
                matchManager.OnProcessAITurn -= HandleProcessAITurn;
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

                    // Make sure the card is face up and properly positioned
                    var cardView = existingCardUI.GetComponent<CardView>();
                    if (cardView != null)
                    {
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
                    var cardGO = CreateCardUI(card, slotTransform, true);
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

            // We don't need to call UpdateUI here anymore since we're directly
            // manipulating the card UI element
        }

        private void HandleCardDrawn(Card card)
        {
            Debug.Log("[MatchView] HandleCardDrawn: " + card.Title);
            UpdateUI(); // Refresh the entire UI
        }

        private void HandleCardDiscarded(Card card)
        {
            UpdateUI(); // Refresh the entire UI
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

        public void UpdateUI()
        {
            if (matchManager == null)
                return;

            Debug.Log("[MatchView] UpdateUI called");

            // Update turn info
            if (turnText != null)
            {
                turnText.text = $"Turn {matchManager.TurnNumber}";
            }

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
            for (int i = 0; i < battlefieldSlots.Length; i++)
            {
                // Check if there's a card in the data model
                if (battlefield[i] != null)
                {
                    // Check if there's already a CardView in this slot
                    bool hasCardView = false;
                    Transform slotTransform = battlefieldSlots[i];
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
                        var cardGO = CreateCardUI(battlefield[i], battlefieldSlots[i], true);
                        if (cardGO != null)
                        {
                            var cardView = cardGO.GetComponent<CardView>();
                            if (cardView != null)
                            {
                                cardView.transform.localPosition = Vector3.zero;
                                cardView.transform.localScale = Vector3.one;
                            }
                        }
                    }
                }
                // If there's no card in the data model but there's a UI element, remove it
                else
                {
                    RemoveCard(i, false);
                }
            }

            // Update opponent's battlefield (top)
            Debug.Log(
                $"[MatchView] Updating opponent battlefield. Cards in data model: {opponentBattlefield.Count(c => c != null)}"
            );

            for (int i = 0; i < opponentBattlefieldSlots.Length; i++)
            {
                // Check if there's a card in the data model
                if (opponentBattlefield[i] != null)
                {
                    Debug.Log(
                        $"[MatchView] Opponent battlefield slot {i} has card: {opponentBattlefield[i].Title}"
                    );

                    // Check if there's already a CardView in this slot
                    bool hasCardView = false;
                    Transform slotTransform = opponentBattlefieldSlots[i];
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
                        Debug.Log(
                            $"[MatchView] Creating opponent card UI for slot {i}: {opponentBattlefield[i].Title}"
                        );
                        var cardGO = CreateCardUI(
                            opponentBattlefield[i],
                            opponentBattlefieldSlots[i],
                            true
                        );
                        if (cardGO != null)
                        {
                            var cardView = cardGO.GetComponent<CardView>();
                            if (cardView != null)
                            {
                                cardView.transform.localPosition = Vector3.zero;
                                cardView.transform.localScale = Vector3.one;
                                Debug.Log(
                                    $"[MatchView] Successfully created opponent card UI for slot {i}"
                                );
                            }
                            else
                            {
                                Debug.LogError(
                                    $"[MatchView] Failed to get CardView component for opponent card in slot {i}"
                                );
                            }
                        }
                        else
                        {
                            Debug.LogError(
                                $"[MatchView] Failed to create card UI for opponent card in slot {i}"
                            );
                        }
                    }
                }
                // If there's no card in the data model but there's a UI element, remove it
                else
                {
                    // Remove card if slot is now empty
                    Debug.Log(
                        $"[MatchView] Removing opponent card UI from slot {i} as it's now empty"
                    );
                    RemoveCard(i, true);
                }
            }

            // Update player's hand (bottom)
            for (int i = 0; i < playerHand.Count; i++)
            {
                if (!cardUIElements.ContainsKey(playerHand[i]))
                {
                    CreateHandCardUI(playerHand[i], handArea, i, true);
                }
            }

            // Update opponent's hand (top, face down)
            for (int i = 0; i < opponentHand.Count; i++)
            {
                if (!cardUIElements.ContainsKey(opponentHand[i]))
                {
                    CreateHandCardUI(opponentHand[i], opponentHandArea, i, false);
                }
            }
        }

        private GameObject CreateHandCardUI(Card card, Transform parent, int position, bool faceUp)
        {
            var cardGO = CreateCardUI(card, parent, faceUp);
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
            }
            return cardGO;
        }

        private GameObject CreateCardUI(Card card, Transform parent, bool faceUp)
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
            cardView.SetDraggable(faceUp);

            // For face down cards, hide the card details and show card back
            if (!faceUp && image != null && cardBackSprite != null)
            {
                image.sprite = cardBackSprite;
            }

            Debug.Log(
                $"[MatchView] Card UI setup complete - Name: {cardGO.name}, "
                    + $"Has CardView: {cardView != null}, "
                    + $"Has Image: {image != null}, "
                    + $"RaycastTarget: {image?.raycastTarget ?? false}, "
                    + $"Parent: {parent.name}, "
                    + $"Canvas: {cardGO.GetComponentInParent<Canvas>()?.name ?? "None"}"
            );

            cardUIElements[card] = cardGO;
            return cardGO;
        }
    }
}
