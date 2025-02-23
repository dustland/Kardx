using System.Collections.Generic;
using System.Linq;
using Kardx.Core.Data.Cards;
using Kardx.Core.Data.States;
using Kardx.Core.Game;
using Kardx.Core.Logging;
using Kardx.UI.Components.Card;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI.Scenes.Battle
{
    using Card = Kardx.Core.Data.Cards.Card; // Alias for Card

    public class BattleView : MonoBehaviour
    {
        [Header("References")]
        private BattleManager battleManager;

        [Header("Layout Areas")]
        [SerializeField]
        private Transform handArea;

        [SerializeField]
        private Transform battlefieldArea;

        [SerializeField]
        private Transform opponentHandArea;

        [SerializeField]
        private Transform opponentBattlefieldArea;

        [SerializeField]
        private Transform discardArea;

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

        private void Start()
        {
            // Create BattleManager instance
            battleManager = new BattleManager(new UnityLogger());

            // Subscribe to BattleManager events
            battleManager.OnCardDeployed += HandleCardDeployed;
            battleManager.OnCardDrawn += HandleCardDrawn;
            battleManager.OnCardDiscarded += HandleCardDiscarded;

            // Start the battle
            battleManager.StartBattle("Player1", "Player2");
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                // Unsubscribe from BattleManager events
                battleManager.OnCardDeployed -= HandleCardDeployed;
                battleManager.OnCardDrawn -= HandleCardDrawn;
                battleManager.OnCardDiscarded -= HandleCardDiscarded;
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
            if (battleManager == null)
            {
                Debug.LogError("CanDeployCard: Battle manager is null");
                return false;
            }

            var currentPlayer = battleManager.GetCurrentPlayerState();
            return currentPlayer.Hand.Contains(card)
                && currentPlayer.Credits >= card.DeploymentCost
                && currentPlayer.Battlefield.Count < PlayerState.MAX_BATTLEFIELD_SIZE;
        }

        public bool DeployCard(Card card)
        {
            if (battleManager == null)
            {
                Debug.LogError("DeployCard: Battle manager is null");
                return false;
            }

            return battleManager.DeployCard(card);
        }

        private void UpdateUI()
        {
            if (battleManager == null)
                return;

            // Update turn info
            if (turnText != null)
            {
                turnText.text = $"Turn {battleManager.TurnNumber}";
            }

            // Player1 is always at bottom, Player2 (opponent) always at top
            var playerState = battleManager.Player1State;
            var opponentState = battleManager.Player2State;

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
            var playerBattlefield = playerState.Battlefield;
            for (int i = 0; i < playerBattlefield.Count; i++)
            {
                CreateBattlefieldCardUI(playerBattlefield[i], battlefieldArea, i, true);
            }

            // Update opponent's battlefield (top)
            var opponentBattlefield = opponentState.Battlefield;
            for (int i = 0; i < opponentBattlefield.Count; i++)
            {
                CreateBattlefieldCardUI(opponentBattlefield[i], opponentBattlefieldArea, i, true);
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

        private GameObject CreateBattlefieldCardUI(
            Card card,
            Transform parent,
            int position,
            bool faceUp
        )
        {
            // Create card UI and let the Layout component handle positioning
            return CreateCardUI(card, parent, faceUp);
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
