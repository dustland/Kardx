using System.Collections.Generic;
using Kardx.Core.Data.Cards;
using Kardx.Core.Data.States;
using Kardx.Core.Game;
using Kardx.UI.Components.Card;
using UnityEngine;

namespace Kardx.UI.Scenes.Battle
{
    using Card = Kardx.Core.Data.Cards.Card; // Alias for Card

    public class BattleView : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField]
        private BattleManager battleManager;

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

        [SerializeField]
        private GameObject dropZonePrefab;

        [Header("Visual Settings")]
        [SerializeField]
        private float cardSpacing = 1.2f;

        [SerializeField]
        private float handCurveHeight = 0.5f;

        // Track UI elements
        private Dictionary<Card, GameObject> cardUIElements = new();
        private Dictionary<Vector2Int, BattlefieldDropZone> dropZones = new();

        private void Start()
        {
            InitializeBattlefield();
            SubscribeToBattleEvents();
        }

        private void InitializeBattlefield()
        {
            // Create drop zones in a grid pattern
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var position = new Vector2Int(x, y);
                    var dropZone = CreateDropZone(position);
                    dropZones[position] = dropZone;
                }
            }
        }

        private BattlefieldDropZone CreateDropZone(Vector2Int position)
        {
            var dropZoneGO = Instantiate(dropZonePrefab, battlefieldArea);
            var dropZone = dropZoneGO.GetComponent<BattlefieldDropZone>();

            // Position the drop zone in the grid
            var worldPos = GridToWorldPosition(position);
            dropZoneGO.transform.localPosition = worldPos;

            // Subscribe to drop events
            dropZone.OnCardDropped += HandleDropZoneCardDropped;

            return dropZone;
        }

        private void HandleDropZoneCardDropped(CardView cardView, Vector2Int position)
        {
            // Notify BattleManager that a card was played
            if (battleManager != null && cardView.Card != null)
            {
                battleManager.DeployCard(cardView.Card, position);
            }
        }

        private void SubscribeToBattleEvents()
        {
            if (battleManager == null)
                return;

            battleManager.OnCardDeployed += HandleCardDeployed;
            battleManager.OnCardActivated += HandleCardActivated;
            battleManager.OnCardDrawn += HandleCardDrawn;
            battleManager.OnCardDiscarded += HandleCardDiscarded;
            battleManager.OnCardDamaged += HandleCardDamaged;
            battleManager.OnCardHealed += HandleCardHealed;
            battleManager.OnModifierAdded += HandleModifierAdded;
            battleManager.OnModifierRemoved += HandleModifierRemoved;
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

        private void MoveCardToBattlefield(Card card, Vector2Int position)
        {
            if (!cardUIElements.TryGetValue(card, out GameObject cardGO))
                return;
            if (!dropZones.TryGetValue(position, out BattlefieldDropZone dropZone))
                return;

            cardGO.transform.SetParent(dropZone.transform, false);
            cardGO.transform.localPosition = Vector3.zero;

            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.SetDraggable(false);
                cardView.PlayDeployAnimation();
            }

            dropZone.SetOccupied(true);
        }

        private Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            float x = gridPos.x * 2.0f - 4.0f; // Center the grid
            float y = gridPos.y * 2.5f - 2.5f; // Center the grid
            return new Vector3(x, y, 0);
        }

        // Event Handlers
        private void HandleCardDeployed(Card card, Vector2Int position)
        {
            MoveCardToBattlefield(card, position);
            UpdateCardUI(card);
        }

        private void HandleCardActivated(Card card)
        {
            UpdateCardUI(card);
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                var cardView = cardGO.GetComponent<CardView>();
                cardView?.PlayAttackAnimation();
            }
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
                // Animate to discard pile before destroying
                StartCoroutine(AnimateToDiscard(cardGO));
            }
        }

        private System.Collections.IEnumerator AnimateToDiscard(GameObject cardGO)
        {
            float duration = 0.5f;
            float elapsed = 0;
            Vector3 startPos = cardGO.transform.position;
            Vector3 endPos = discardArea.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                cardGO.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            Destroy(cardGO);
            cardUIElements.Remove(cardGO.GetComponent<CardView>().Card);
        }

        private void HandleCardDamaged(Card card, int amount)
        {
            UpdateCardUI(card);
            if (cardUIElements.TryGetValue(card, out GameObject cardGO))
            {
                var cardView = cardGO.GetComponent<CardView>();
                cardView?.PlayDamageAnimation();
            }
        }

        private void HandleCardHealed(Card card, int amount)
        {
            UpdateCardUI(card);
        }

        private void HandleModifierAdded(Card card, Modifier modifier)
        {
            UpdateCardUI(card);
        }

        private void HandleModifierRemoved(Card card, Modifier modifier)
        {
            UpdateCardUI(card);
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.OnCardDeployed -= HandleCardDeployed;
                battleManager.OnCardActivated -= HandleCardActivated;
                battleManager.OnCardDrawn -= HandleCardDrawn;
                battleManager.OnCardDiscarded -= HandleCardDiscarded;
                battleManager.OnCardDamaged -= HandleCardDamaged;
                battleManager.OnCardHealed -= HandleCardHealed;
                battleManager.OnModifierAdded -= HandleModifierAdded;
                battleManager.OnModifierRemoved -= HandleModifierRemoved;
            }

            // Unsubscribe from drop zone events
            foreach (var dropZone in dropZones.Values)
            {
                if (dropZone != null)
                {
                    dropZone.OnCardDropped -= HandleDropZoneCardDropped;
                }
            }
        }
    }
}
