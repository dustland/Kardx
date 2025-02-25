using System.Collections.Generic;
using System.Linq;
using Kardx.Core;
using Kardx.Utils;
using Kardx.UI.Components;
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
        private Dictionary<int, CardView> deployedCards = new Dictionary<int, CardView>();
        private Dictionary<int, CardView> opponentDeployedCards = new Dictionary<int, CardView>();

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
                        Debug.LogError("[CardCollectionView] CardDetailView not found in scene. Please ensure CardPanel has CardDetailView component.");
                        return;
                    }
                }
            }

            Debug.Log($"[CardCollectionView] Found CardDetailView on: {cardDetailView.gameObject.name}");
            CardView.InitializeSharedDetailView(cardDetailView);

            // Make sure the CardPanel is initially inactive
            cardDetailView.gameObject.SetActive(false);

            InitializeBattlefieldSlots();
        }

        private void InitializeBattlefieldSlots()
        {
            if (battlefieldArea == null || opponentBattlefieldArea == null || cardSlotPrefab == null)
            {
                Debug.LogError("[MatchView] Missing required components for battlefield initialization");
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
            // Create BattleManager instance
            matchManager = new MatchManager(new UnityLogger());

            // Subscribe to BattleManager events
            matchManager.OnCardDeployed += HandleCardDeployed;
            matchManager.OnCardDrawn += HandleCardDrawn;
            matchManager.OnCardDiscarded += HandleCardDiscarded;

            // Start the battle
            matchManager.StartMatch("Player1", "Player2");
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
            }
        }

        // Event handlers
        private void HandleCardDeployed(Card card, int position)
        {
            UpdateUI(); // Refresh the entire UI
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

            if (deployedCards.ContainsKey(slotIndex))
            {
                Debug.LogWarning($"Slot {slotIndex} is already occupied");
                return false;
            }

            // Check and deploy via match manager
            if (!matchManager.DeployCard(card, slotIndex))
            {
                Debug.LogWarning("Failed to deploy card via match manager");
                return false;
            }

            // Create card UI in the specific slot
            var cardGO = CreateCardUI(card, battlefieldSlots[slotIndex], true);
            if (cardGO != null)
            {
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    deployedCards[slotIndex] = cardView;
                    cardView.transform.localPosition = Vector3.zero;
                    cardView.transform.localScale = Vector3.one;
                }
            }

            return true;
        }

        private void RemoveCard(int slotIndex, bool isOpponent = false)
        {
            var cards = isOpponent ? opponentDeployedCards : deployedCards;
            if (cards.TryGetValue(slotIndex, out var cardView))
            {
                cards.Remove(slotIndex);
                if (cardView != null)
                {
                    // Handle card removal animation/effects here
                    Destroy(cardView.gameObject);
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

        private void UpdateUI()
        {
            if (matchManager == null)
                return;

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

            // Clear only hand card UI elements, preserving battlefield cards
            var cardsToPreserve = new HashSet<Card>();
            foreach (var deployedCard in deployedCards.Values)
            {
                if (deployedCard != null && deployedCard.Card != null)
                {
                    cardsToPreserve.Add(deployedCard.Card);
                }
            }
            foreach (var deployedCard in opponentDeployedCards.Values)
            {
                if (deployedCard != null && deployedCard.Card != null)
                {
                    cardsToPreserve.Add(deployedCard.Card);
                }
            }

            // Only destroy cards that aren't on the battlefield
            foreach (var cardEntry in cardUIElements.ToList())
            {
                if (!cardsToPreserve.Contains(cardEntry.Key))
                {
                    Destroy(cardEntry.Value);
                    cardUIElements.Remove(cardEntry.Key);
                }
            }

            // Update player's hand (bottom)
            var playerHand = playerState.Hand;
            for (int i = 0; i < playerHand.Count; i++)
            {
                if (!cardUIElements.ContainsKey(playerHand[i]))
                {
                    CreateHandCardUI(playerHand[i], handArea, i, true);
                }
            }

            // Update opponent's hand (top, face down)
            var opponentHand = opponentState.Hand;
            for (int i = 0; i < opponentHand.Count; i++)
            {
                if (!cardUIElements.ContainsKey(opponentHand[i]))
                {
                    CreateHandCardUI(opponentHand[i], opponentHandArea, i, false);
                }
            }

            // Update player's battlefield (bottom)
            var battlefield = playerState.Battlefield;
            for (int i = 0; i < battlefieldSlots.Length; i++)
            {
                if (deployedCards.TryGetValue(i, out var cardView))
                {
                    cardView.transform.SetParent(battlefieldSlots[i]);
                    cardView.transform.localPosition = Vector3.zero;
                    cardView.transform.localScale = Vector3.one;
                }
            }

            // Update opponent's battlefield (top)
            var opponentBattlefield = opponentState.Battlefield;
            for (int i = 0; i < opponentBattlefieldSlots.Length; i++)
            {
                if (opponentDeployedCards.TryGetValue(i, out var cardView))
                {
                    cardView.transform.SetParent(opponentBattlefieldSlots[i]);
                    cardView.transform.localPosition = Vector3.zero;
                    cardView.transform.localScale = Vector3.one;
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

            Debug.Log($"[MatchView] Card UI setup complete - Name: {cardGO.name}, " +
                     $"Has CardView: {cardView != null}, " +
                     $"Has Image: {image != null}, " +
                     $"RaycastTarget: {image?.raycastTarget ?? false}, " +
                     $"Parent: {parent.name}, " +
                     $"Canvas: {cardGO.GetComponentInParent<Canvas>()?.name ?? "None"}");

            cardUIElements[card] = cardGO;
            return cardGO;
        }
    }
}
