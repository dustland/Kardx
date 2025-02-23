using System;
using System.Collections.Generic;
using Kardx.Core.Data.Cards;
using Kardx.Core.Data.States;
using UnityEngine;

namespace Kardx.Core.Game
{
    public class BattleManager : MonoBehaviour
    {
        [Header("State References")]
        [SerializeField]
        private BoardState boardState;

        [SerializeField]
        private SharedState sharedState;

        [SerializeField]
        private PlayerState player1State;

        [SerializeField]
        private PlayerState player2State;

        [Header("Battle Configuration")]
        [SerializeField]
        private int startingHandSize = 5;

        [SerializeField]
        private int maxTurns = 50;

        // Public properties
        public bool IsBattleInProgress { get; private set; }
        public string CurrentPlayerId => boardState.CurrentPlayerId;
        public int TurnNumber => boardState.TurnNumber;

        // Events for UI synchronization
        public event Action<string, string> OnBattleStarted; // player1Id, player2Id
        public event Action OnBattleEnded;
        public event Action<string> OnTurnStarted; // playerId
        public event Action<Card, Vector2Int> OnCardDeployed; // card, position
        public event Action<Card> OnCardActivated; // card
        public event Action<Card> OnCardDrawn; // card
        public event Action<Card> OnCardDiscarded; // card
        public event Action<Card, int> OnCardDamaged; // card, amount
        public event Action<Card, int> OnCardHealed; // card, amount
        public event Action<Card, Modifier> OnModifierAdded; // card, modifier
        public event Action<Card, Modifier> OnModifierRemoved; // card, modifier

        private void Awake()
        {
            ValidateReferences();
        }

        private void ValidateReferences()
        {
            if (boardState == null)
                boardState = GetComponent<BoardState>();
            if (boardState == null)
                Debug.LogError("BoardState reference is missing!");
            if (sharedState == null)
                Debug.LogError("SharedState reference is missing!");
            if (player1State == null)
                Debug.LogError("Player1State reference is missing!");
            if (player2State == null)
                Debug.LogError("Player2State reference is missing!");
        }

        public void StartBattle(string player1Id, string player2Id)
        {
            if (IsBattleInProgress)
            {
                Debug.LogWarning("Battle is already in progress!");
                return;
            }

            // Initialize board state
            InitializeBoardState(player1Id, player2Id);

            // Shuffle decks
            player1State.ShuffleDeck();
            player2State.ShuffleDeck();
            sharedState.ShuffleNeutralDeck();

            // Draw starting hands
            DrawStartingHands();

            IsBattleInProgress = true;

            // Notify battle start
            OnBattleStarted?.Invoke(player1Id, player2Id);
        }

        private void DrawStartingHands()
        {
            // Draw cards for player 1
            for (int i = 0; i < startingHandSize; i++)
            {
                Card drawnCard = player1State.DrawCard();
                if (drawnCard != null)
                {
                    OnCardDrawn?.Invoke(drawnCard);
                }
            }

            // Draw cards for player 2
            for (int i = 0; i < startingHandSize; i++)
            {
                Card drawnCard = player2State.DrawCard();
                if (drawnCard != null)
                {
                    OnCardDrawn?.Invoke(drawnCard);
                }
            }
        }

        private void InitializeBoardState(string player1Id, string player2Id)
        {
            boardState.Reset();
            boardState.AddPlayer(player1Id, player1State);
            boardState.AddPlayer(player2Id, player2State);
            boardState.SetCurrentPlayer(player1Id);
        }

        public void EndBattle()
        {
            if (!IsBattleInProgress)
                return;

            IsBattleInProgress = false;
            OnBattleEnded?.Invoke();
        }

        public void StartNextTurn()
        {
            if (!IsBattleInProgress)
                return;

            if (TurnNumber >= maxTurns)
            {
                EndBattle();
                return;
            }

            // Process turn transition
            boardState.StartNextTurn();

            // Draw card for new turn
            var currentPlayer = GetCurrentPlayerState();
            if (currentPlayer.DrawCard() is Card card)
            {
                OnCardDrawn?.Invoke(card);
            }

            OnTurnStarted?.Invoke(CurrentPlayerId);
        }

        public bool DeployCard(Card card, Vector2Int position)
        {
            if (!IsBattleInProgress)
                return false;

            var currentPlayer = GetCurrentPlayerState();

            // Check if player has enough credits
            if (!currentPlayer.SpendCredits(card.CardType.DeploymentCost))
            {
                return false;
            }

            // Deploy the card
            if (currentPlayer.DeployCard(card, position))
            {
                OnCardDeployed?.Invoke(card, position);
                return true;
            }

            // Refund credits if deployment failed
            currentPlayer.AddCredits(card.CardType.DeploymentCost);
            return false;
        }

        public bool ActivateCard(Card card)
        {
            if (!IsBattleInProgress)
                return false;

            var currentPlayer = GetCurrentPlayerState();

            // Check if player has enough credits
            if (!currentPlayer.SpendCredits(card.CardType.OperationCost))
            {
                return false;
            }

            // Activate card abilities
            foreach (var ability in card.CardType.Abilities)
            {
                // TODO: Implement ability activation logic
            }

            OnCardActivated?.Invoke(card);
            return true;
        }

        public void DamageCard(Card card, int amount)
        {
            card.TakeDamage(amount);
            OnCardDamaged?.Invoke(card, amount);
        }

        public void HealCard(Card card, int amount)
        {
            card.Heal(amount);
            OnCardHealed?.Invoke(card, amount);
        }

        public void AddModifier(Card card, Modifier modifier)
        {
            card.AddModifier(modifier);
            OnModifierAdded?.Invoke(card, modifier);
        }

        public void RemoveModifier(Card card, Modifier modifier)
        {
            card.RemoveModifier(modifier);
            OnModifierRemoved?.Invoke(card, modifier);
        }

        private PlayerState GetCurrentPlayerState()
        {
            return boardState.Players[boardState.CurrentPlayerId];
        }
    }
}
