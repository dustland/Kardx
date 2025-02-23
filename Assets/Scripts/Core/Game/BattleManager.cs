using System;
using System.Collections.Generic;
using System.IO;
using Kardx.Core.Data;
using Kardx.Core.Data.Cards;
using Kardx.Core.Data.States;
using Kardx.Core.Logging;
using Newtonsoft.Json;

namespace Kardx.Core.Game
{
    public class BattleManager
    {
        private readonly BoardState boardState;
        private readonly ILogger logger;
        private readonly int startingHandSize = 5;
        private readonly int maxTurns = 50;

        // Public properties
        public bool IsBattleInProgress { get; private set; }
        public string CurrentPlayerId => boardState.CurrentPlayerId;
        public int TurnNumber => boardState.TurnNumber;
        public PlayerState Player1State => boardState.Players[player1Id];
        public PlayerState Player2State => boardState.Players[player2Id];

        private string player1Id = "Player1";
        private string player2Id = "Player2";

        // Essential events for UI updates
        public event Action<Card, int> OnCardDeployed;
        public event Action<Card> OnCardDrawn;
        public event Action<Card> OnCardDiscarded;

        public BattleManager(ILogger logger = null)
        {
            this.logger = logger;
            boardState = new BoardState();
        }

        private List<Card> LoadPlayerDeck(string playerId)
        {
            return DeckLoader.LoadDeck(playerId);
        }

        public void StartBattle(string player1Id, string player2Id)
        {
            if (IsBattleInProgress)
                return;

            this.player1Id = player1Id;
            this.player2Id = player2Id;

            // Reset and initialize board state
            boardState.Reset();
            boardState.AddPlayer(player1Id, new PlayerState(player1Id, LoadPlayerDeck(player1Id)));
            boardState.AddPlayer(player2Id, new PlayerState(player2Id, LoadPlayerDeck(player2Id)));
            boardState.SetCurrentPlayer(player1Id);

            IsBattleInProgress = true;

            // Shuffle decks and draw starting hands
            Player1State.ShuffleDeck();
            Player2State.ShuffleDeck();
            Player1State.DrawCards(startingHandSize);
            Player2State.DrawCards(startingHandSize);

            // Start first turn
            StartTurn();
        }

        public void EndBattle()
        {
            if (!IsBattleInProgress)
                return;

            IsBattleInProgress = false;
        }

        public void StartTurn()
        {
            if (!IsBattleInProgress)
                return;

            if (TurnNumber >= maxTurns)
            {
                EndBattle();
                return;
            }

            // Draw card for new turn
            var currentPlayer = GetCurrentPlayerState();
            if (currentPlayer.DrawCard() is Card card)
            {
                NotifyCardDrawn(card);
            }

            // End of turn is a action from player's turn, so here we should only do the draw action
        }

        public void EndTurn()
        {
            boardState.IncrementTurn();
            boardState.SwitchCurrentPlayer();
            // Trigger the new turn
            StartTurn();
        }

        public bool DeployCard(Card card, int position)
        {
            if (!IsBattleInProgress)
                return false;

            var currentPlayer = GetCurrentPlayerState();

            // Check if the position is valid
            if (!currentPlayer.IsValidBattlefieldPosition(position))
                return false;

            // Check if player has enough credits
            if (!currentPlayer.SpendCredits(card.CardType.DeploymentCost))
                return false;

            // Deploy the card
            if (currentPlayer.DeployCard(card, position))
            {
                NotifyCardDeployed(card, position);
                return true;
            }

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

            return true;
        }

        public void DamageCard(Card card, int amount)
        {
            card.TakeDamage(amount);
        }

        public void HealCard(Card card, int amount)
        {
            card.Heal(amount);
        }

        public void AddModifier(Card card, Modifier modifier)
        {
            card.AddModifier(modifier);
        }

        public void RemoveModifier(Card card, Modifier modifier)
        {
            card.RemoveModifier(modifier);
        }

        private void NotifyCardDeployed(Card card, int position)
        {
            OnCardDeployed?.Invoke(card, position);
        }

        private void NotifyCardDrawn(Card card)
        {
            OnCardDrawn?.Invoke(card);
        }

        private void NotifyCardDiscarded(Card card)
        {
            OnCardDiscarded?.Invoke(card);
        }

        private PlayerState GetCurrentPlayerState()
        {
            return boardState.Players[boardState.CurrentPlayerId];
        }
    }
}
