using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kardx.Core;
using Kardx.Utils;
using Newtonsoft.Json;

namespace Kardx.Core
{
    public class MatchManager
    {
        private Board board;
        private readonly ILogger logger;
        private readonly int startingHandSize = 2;
        private readonly int maxTurns = 50;

        // Public properties
        public bool IsMatchInProgress { get; private set; }
        public string CurrentPlayerId => board.CurrentPlayerId;
        public int TurnNumber => board.TurnNumber;
        public Player Player => board.Player;
        public Player Opponent => board.Opponent;

        // Essential events for UI updates
        public event Action<Card, int> OnCardDeployed;
        public event Action<Card> OnCardDrawn;
        public event Action<Card> OnCardDiscarded;
        public event Action<string> OnTurnStarted;
        public event Action<string> OnTurnEnded;
        public event Action<string> OnMatchStarted;
        public event Action<string> OnMatchEnded;

        public MatchManager(ILogger logger = null)
        {
            this.logger = logger;
        }

        private List<Card> LoadDeck()
        {
            return CardLoader.LoadCards();
        }

        public void StartMatch(string player1Id, string player2Id)
        {
            if (IsMatchInProgress)
                return;

            // Initialize players with their decks
            var player1 = new Player(player1Id, LoadDeck(), logger);
            var player2 = new Player(player2Id, LoadDeck(), logger);

            // Create new board with initialized players
            board = new Board(player1, player2, logger);

            IsMatchInProgress = true;

            // Draw starting hands
            for (int i = 0; i < startingHandSize; i++)
            {
                var drawnCard = board.Player.DrawCard();
                if (drawnCard != null)
                {
                    NotifyCardDrawn(drawnCard);
                }

                drawnCard = board.Opponent.DrawCard();
                if (drawnCard != null)
                {
                    NotifyCardDrawn(drawnCard);
                }
            }

            OnMatchStarted?.Invoke($"Match started between {player1Id} and {player2Id}");
            logger?.Log($"Match started between {player1Id} and {player2Id}");
        }

        public void EndMatch()
        {
            if (!IsMatchInProgress)
                return;

            IsMatchInProgress = false;
            OnMatchEnded?.Invoke($"Match ended after {TurnNumber} turns");
            logger?.Log($"Match ended after {TurnNumber} turns");
        }

        public void StartTurn()
        {
            if (!IsMatchInProgress)
                return;

            if (TurnNumber >= maxTurns)
            {
                EndMatch();
                return;
            }

            // Get current player and start their turn
            var currentPlayer = GetCurrentPlayer();
            var drawnCard = currentPlayer.DrawCard();
            if (drawnCard != null)
            {
                NotifyCardDrawn(drawnCard);
            }

            OnTurnStarted?.Invoke($"Turn {TurnNumber} started for {CurrentPlayerId}");
            logger?.Log($"Turn {TurnNumber} started for {CurrentPlayerId}");
        }

        public void EndTurn()
        {
            if (!IsMatchInProgress)
                return;

            OnTurnEnded?.Invoke($"Turn {TurnNumber} ended for {CurrentPlayerId}");
            logger?.Log($"Turn {TurnNumber} ended for {CurrentPlayerId}");

            // Use Board's built-in turn management
            board.StartNextTurn();
        }

        public bool CanDeployCard(Card card)
        {
            if (!IsMatchInProgress || card == null)
                return false;

            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer == null)
                return false;

            return currentPlayer.Hand.Contains(card)
                && currentPlayer.Credits >= card.DeploymentCost
                && currentPlayer.Battlefield.Count < Player.BATTLEFIELD_SLOT_COUNT;
        }

        public bool DeployCard(Card card, int position)
        {
            if (!CanDeployCard(card))
                return false;

            var currentPlayer = GetCurrentPlayer();

            // Try to deploy the card
            if (!currentPlayer.DeployCard(card, position))
                return false;

            // Notify listeners
            NotifyCardDeployed(card, position);

            return true;
        }

        private Player GetCurrentPlayer()
        {
            return board.CurrentPlayer;
        }

        private void NotifyCardDeployed(Card card, int position)
        {
            OnCardDeployed?.Invoke(card, position);
            logger?.Log($"[{CurrentPlayerId}] Card deployed: {card.Title} at position {position}");
        }

        private void NotifyCardDrawn(Card card)
        {
            OnCardDrawn?.Invoke(card);
            logger?.Log($"[{CurrentPlayerId}] Card drawn: {card.Title}");
        }

        private void NotifyCardDiscarded(Card card)
        {
            OnCardDiscarded?.Invoke(card);
            logger?.Log($"[{CurrentPlayerId}] Card discarded: {card.Title}");
        }
    }
}
