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
        private Transform[] battlefieldSlots = new Transform[5]; // 5 fixed positions

        [SerializeField]
        private Transform opponentHandArea;

        [SerializeField]
        private Transform[] opponentBattlefieldSlots = new Transform[5]; // 5 fixed positions for opponent

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
        private float cardSpacing = 1.2f;

        [SerializeField]
        private float handCurveHeight = 0.5f;

        // Non-serialized fields
        private Dictionary<Card, GameObject> cardUIElements = new();
        private Dictionary<int, CardView> deployedCards = new Dictionary<int, CardView>();
        private Dictionary<int, CardView> opponentDeployedCards = new Dictionary<int, CardView>();

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
                return false;

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

            // Clear existing card UI elements
            foreach (var cardUI in cardUIElements.Values)
            {
                Destroy(cardUI);
            }
            cardUIElements.Clear();

            // Update player's hand (bottom)
            var playerHand = playerState.Hand;
            for (int i = 0; i < playerHand.Count; i++)
            {
                CreateHandCardUI(playerHand[i], handArea, i, true);
            }

            // Update opponent's hand (top, face down)
            var opponentHand = opponentState.Hand;
            for (int i = 0; i < opponentHand.Count; i++)
            {
                CreateHandCardUI(opponentHand[i], opponentHandArea, i, false);
            }

            // Update player's battlefield (bottom)
            for (int i = 0; i < battlefieldSlots.Length; i++)
            {
                if (deployedCards.TryGetValue(i, out var cardView))
                {
                    cardView.transform.SetParent(battlefieldSlots[i]);
                }
            }

            // Update opponent's battlefield (top)
            for (int i = 0; i < opponentBattlefieldSlots.Length; i++)
            {
                if (opponentDeployedCards.TryGetValue(i, out var cardView))
                {
                    cardView.transform.SetParent(opponentBattlefieldSlots[i]);
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
            if (cardPrefab == null)
                return null;

            var cardGO = Instantiate(cardPrefab, parent);
            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.Initialize(card);
                cardView.SetDraggable(faceUp);

                // For face down cards, hide the card details and show card back
                if (!faceUp)
                {
                    var image = cardGO.GetComponent<Image>();
                    if (image != null && cardBackSprite != null)
                    {
                        image.sprite = cardBackSprite;
                    }
                }
            }

            cardUIElements[card] = cardGO;
            return cardGO;
        }
    }
}
