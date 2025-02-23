using System.Collections.Generic;
using Kardx.Core.Data.Cards;
using Kardx.Core.Data.States;
using Kardx.Core.Game;
using Kardx.Core.Logging;
using Kardx.UI.Components.Card;
using TMPro;
using UnityEngine;

namespace Kardx.UI.Scenes.Battle
{
    using Card = Kardx.Core.Data.Cards.Card; // Alias for Card

    public class BattleView : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField]
        private Canvas battleCanvas;

        [Header("UI Areas")]
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

        [SerializeField]
        private Transform turnText;

        [SerializeField]
        private Transform creditsText;

        [SerializeField]
        private Transform opponentCreditsText;

        [Header("UI Prefabs")]
        [SerializeField]
        private GameObject cardPrefab;

        [Header("Visual Settings")]
        [SerializeField]
        private float cardSpacing = 1.2f;

        [SerializeField]
        private float handCurveHeight = 0.5f;

        [SerializeField]
        private BattlefieldDropZone battlefieldDropZone; // Single dropzone for player1's battlefield

        // Non-serialized fields
        private BattleManager battleManager;

        // Track UI elements
        private Dictionary<Card, GameObject> cardUIElements = new();

        private void Awake()
        {
            // Other initialization if needed
        }

        private void Start()
        {
            Debug.Log("Starting battle");
            // Create BattleManager instance
            battleManager = new BattleManager(new UnityLogger());

            // Start the battle
            battleManager.StartBattle("Player1", "Player2");
            UpdateUI();
            SubscribeToBattleEvents();
        }

        public void EndTurn()
        {
            battleManager.EndTurn();
            UpdateUI();
        }

        private void SubscribeToBattleEvents()
        {
            if (battleManager == null)
                return;

            // Only keep essential events for UI updates
            battleManager.OnCardDeployed += HandleCardDeployed;
            battleManager.OnCardDrawn += HandleCardDrawn;
            battleManager.OnCardDiscarded += HandleCardDiscarded;
        }

        private void UpdateUI()
        {
            if (battleManager == null)
                return;

            // Update turn text
            if (
                turnText != null
                && turnText.TryGetComponent<TMPro.TextMeshProUGUI>(out var turnTMP)
            )
            {
                turnTMP.text = $"Turn {battleManager.TurnNumber}";
            }

            // Update credits text
            if (
                creditsText != null
                && creditsText.TryGetComponent<TMPro.TextMeshProUGUI>(out var creditsTMP)
            )
            {
                creditsTMP.text = $"{battleManager.Player1State?.Credits ?? 0}";
            }

            if (
                opponentCreditsText != null
                && opponentCreditsText.TryGetComponent<TMPro.TextMeshProUGUI>(out var oppCreditsTMP)
            )
            {
                oppCreditsTMP.text = $"{battleManager.Player2State?.Credits ?? 0}";
            }
        }

        // UI Card Creation and Management
        private GameObject CreateCardUI(Card card, Transform parent)
        {
            var cardGO = Instantiate(cardPrefab, parent);
            var cardView = cardGO.GetComponent<CardView>();

            if (cardView != null)
            {
                cardView.Initialize(card);
                cardUIElements[card] = cardGO;
            }

            return cardGO;
        }

        private void UpdateCardUI(Card card)
        {
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.UpdateCardView();
                }
            }
        }

        private void ArrangeHandCards()
        {
            var cards = handArea.GetComponentsInChildren<CardView>();
            int cardCount = cards.Length;

            for (int i = 0; i < cardCount; i++)
            {
                float t = cardCount > 1 ? i / (float)(cardCount - 1) : 0.5f;
                float x = Mathf.Lerp(
                    -cardSpacing * (cardCount - 1) * 0.5f,
                    cardSpacing * (cardCount - 1) * 0.5f,
                    t
                );
                float y = -Mathf.Sin(t * Mathf.PI) * handCurveHeight;

                cards[i].transform.localPosition = new Vector3(x, y, 0);
                cards[i].transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-5, 5, t));
            }
        }

        // Zone Management
        private void MoveCardToHand(Card card)
        {
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                cardGO.transform.SetParent(handArea, false);
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null)
                {
                    cardView.SetDraggable(true);
                }
                ArrangeHandCards();
            }
        }

        private void MoveCardToBattlefield(Card card, int position)
        {
            if (!cardUIElements.TryGetValue(card, out GameObject cardGO))
                return;

            cardGO.transform.SetParent(battlefieldDropZone.transform, false);
            cardGO.transform.localPosition = Vector3.zero;

            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.SetDraggable(false);
                cardView.PlayDeployAnimation();
            }

            battlefieldDropZone.SetOccupied(true);
        }

        // Event Handlers
        private void HandleCardDeployed(Card card, int position)
        {
            MoveCardToBattlefield(card, position);
            UpdateCardUI(card);
        }

        private void HandleCardDrawn(Card card)
        {
            var cardGO = CreateCardUI(card, handArea);
            ArrangeHandCards();
        }

        private void HandleCardDiscarded(Card card)
        {
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                Destroy(cardGO);
                cardUIElements.Remove(card);
            }
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                // Unsubscribe from essential events
                battleManager.OnCardDeployed -= HandleCardDeployed;
                battleManager.OnCardDrawn -= HandleCardDrawn;
                battleManager.OnCardDiscarded -= HandleCardDiscarded;
            }
        }
    }
}
