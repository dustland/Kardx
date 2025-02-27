using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core.Planning;
using Kardx.Utils;

namespace Kardx.Core
{
    /// <summary>
    /// Manages the game match, handling turns and player actions.
    /// </summary>
    public class MatchManager
    {
        private Kardx.Utils.ILogger logger;
        private Board board;
        private StrategyPlanner strategyPlanner;
        private int startingHandSize = 4;
        private int maxTurns = 50;
        private const int CREDITS_PER_TURN = 1;

        // Public properties
        public bool IsMatchInProgress { get; private set; }
        public string CurrentPlayerId => board.CurrentPlayerId;
        public int TurnNumber => board.TurnNumber;
        public Player Player => board.Player;
        public Player Opponent => board.Opponent;
        public Board Board => board;
        public Player CurrentPlayer => board.CurrentPlayer;

        // Essential events for UI updates
        public event Action<Card, int> OnCardDeployed;
        public event Action<Card> OnCardDrawn;
        public event Action<Card> OnCardDiscarded;
        public event Action<Strategy> OnStrategyDetermined;
        public event Action<Decision> OnDecisionExecuting;
        public event Action<Decision> OnDecisionExecuted;
        public event EventHandler<Player> OnTurnStarted;
        public event EventHandler<Player> OnTurnEnded;
        public event Action<string> OnMatchStarted;
        public event Action<string> OnMatchEnded;

        // Event for signaling when AI needs to process its turn
        public event Action<Board, StrategyPlanner, Action> OnProcessAITurn;

        /// <summary>
        /// Creates a new instance of the MatchManager class.
        /// </summary>
        /// <param name="logger">Optional logger for debugging.</param>
        public MatchManager(Kardx.Utils.ILogger logger = null)
        {
            // Use the SimpleLogger if no logger is provided
            this.logger = logger ?? new SimpleLogger("MatchManager");
        }

        /// <summary>
        /// Starts a new match with default players.
        /// </summary>
        public void StartMatch()
        {
            var player1 = new Player(
                "Player 1",
                LoadDeck(Faction.UnitedStates),
                Faction.UnitedStates
            );
            var player2 = new Player(
                "Player 2",
                LoadDeck(Faction.SovietUnion),
                Faction.SovietUnion
            );

            // Create a new board
            board = new Board(player1, player2);

            // Create the strategy planner for the opponent (player2)
            strategyPlanner = new StrategyPlanner(StrategySource.Dummy, player2, this, logger);

            // Subscribe to strategy planner events
            if (strategyPlanner != null)
            {
                strategyPlanner.OnStrategyDetermined += (strategy) =>
                    OnStrategyDetermined?.Invoke(strategy);

                strategyPlanner.OnDecisionExecuting += (decision) =>
                    OnDecisionExecuting?.Invoke(decision);

                strategyPlanner.OnDecisionExecuted += (decision) =>
                    OnDecisionExecuted?.Invoke(decision);

                // Subscribe to additional events for opponent actions
                SubscribeToStrategyPlannerEvents(strategyPlanner);
            }

            logger?.Log($"[MatchManager] Match started between {player1.Id} and {player2.Id}");
            IsMatchInProgress = true;
            OnMatchStarted?.Invoke($"Match started between {player1.Id} and {player2.Id}");

            // Start the first turn
            if (board == null)
                throw new InvalidOperationException("Match not started");

            var currentPlayer = board.CurrentPlayer;
            logger?.Log($"[MatchManager] Starting first turn for player {currentPlayer.Id}");

            // Initialize the player for the first turn
            // Add credits based on the turn number
            int creditsToAdd = CREDITS_PER_TURN * board.TurnNumber;
            currentPlayer.AddCredits(creditsToAdd);
            logger?.Log(
                $"[MatchManager] Added {creditsToAdd} credits to player {currentPlayer.Id}"
            );

            // Draw a card for the player
            Card drawnCard = currentPlayer.DrawCard();
            if (drawnCard != null)
            {
                OnCardDrawn?.Invoke(drawnCard);
                logger?.Log($"[{CurrentPlayerId}] Card drawn: {drawnCard.Title}");
            }

            // Notify listeners that a new turn is starting
            OnTurnStarted?.Invoke(this, currentPlayer);

            // Reset card attack status
            currentPlayer.ResetCardAttackStatus();

            // If it's the opponent's turn, process it automatically
            if (currentPlayer.Id == board.Player2.Id)
            {
                ProcessOpponentTurn();
            }
        }

        /// <summary>
        /// Advances to the next turn in the game.
        /// </summary>
        public void NextTurn()
        {
            if (board == null)
                throw new InvalidOperationException("Match not started");

            // End the current turn
            var currentPlayer = board.CurrentPlayer;
            logger?.Log($"[MatchManager] Ending turn for player {currentPlayer.Id}");

            // Process end of turn effects
            board.ProcessEndOfTurnEffects();

            // Notify listeners that the turn is ending
            OnTurnEnded?.Invoke(this, currentPlayer);

            // Start the next turn
            // Switch to the next player
            board.SwitchCurrentPlayer();
            board.IncrementTurnNumber();

            var nextPlayer = board.CurrentPlayer;
            logger?.Log(
                $"[MatchManager] Starting turn {board.TurnNumber} for player {nextPlayer.Id}"
            );

            // Initialize the player for the new turn
            // Add credits based on the turn number
            int creditsToAdd = CREDITS_PER_TURN * board.TurnNumber;
            nextPlayer.AddCredits(creditsToAdd);
            logger?.Log($"[MatchManager] Added {creditsToAdd} credits to player {nextPlayer.Id}");

            // Draw a card for the player
            Card drawnCard = nextPlayer.DrawCard(nextPlayer.Id == board.Player2.Id);
            if (drawnCard != null)
            {
                OnCardDrawn?.Invoke(drawnCard);
                logger?.Log($"[{CurrentPlayerId}] Card drawn: {drawnCard.Title}");
            }

            // Process start of turn effects
            board.ProcessStartOfTurnEffects();

            // Notify listeners that a new turn is starting
            OnTurnStarted?.Invoke(this, nextPlayer);

            // Reset card attack status
            nextPlayer.ResetCardAttackStatus();

            // If it's the opponent's turn, process it automatically
            if (nextPlayer.Id == board.Player2.Id)
            {
                ProcessOpponentTurn();
            }
        }

        /// <summary>
        /// Processes the opponent's turn automatically.
        /// </summary>
        private void ProcessOpponentTurn()
        {
            if (board == null)
                throw new InvalidOperationException("Match not started");

            logger?.Log("Processing opponent turn");

            // Trigger the event for the UI layer to handle AI processing
            OnProcessAITurn?.Invoke(board, strategyPlanner, NextTurn);
        }

        private List<Card> LoadDeck(Faction ownerFaction)
        {
            var cardTypes = CardLoader.LoadCardTypes();
            var deck = new List<Card>();

            // Shuffle the card types before adding them to the deck
            var shuffledCardTypes = cardTypes.OrderBy(x => Guid.NewGuid()).ToList();

            foreach (var cardType in shuffledCardTypes)
            {
                // TODO: Add deck building rules here (e.g., card limits, faction restrictions)
                deck.Add(new Card(cardType, ownerFaction));
            }

            return deck;
        }

        public void EndMatch()
        {
            if (!IsMatchInProgress)
                return;

            IsMatchInProgress = false;
            OnMatchEnded?.Invoke($"Match ended after {TurnNumber} turns");
            logger?.Log($"Match ended after {TurnNumber} turns");
        }

        public bool CanDeployCard(Card card)
        {
            if (!IsMatchInProgress || card == null)
            {
                logger?.LogError("[MatchManager] Cannot deploy card: card is null");
                return false;
            }

            var currentPlayer = board.CurrentPlayer;
            if (currentPlayer == null)
            {
                logger?.LogError("[MatchManager] Cannot deploy card: current player is null");
                return false;
            }

            // Count non-null slots in the battlefield instead of using Count
            var occupiedSlots = currentPlayer.Battlefield.Count(c => c != null);

            return currentPlayer.Hand.Contains(card)
                && currentPlayer.Credits >= card.DeploymentCost
                && occupiedSlots < Player.BATTLEFIELD_SLOT_COUNT;
        }

        public bool DeployCard(Card card, int position)
        {
            if (!CanDeployCard(card))
            {
                // Add more detailed logging to help diagnose the issue
                var player = board.CurrentPlayer;
                if (card == null)
                {
                    logger?.LogError("[MatchManager] Cannot deploy card: card is null");
                }
                else if (!player.Hand.Contains(card))
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy card {card.Title}: not in player's hand"
                    );
                }
                else if (player.Credits < card.DeploymentCost)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy card {card.Title}: insufficient credits (has {player.Credits}, needs {card.DeploymentCost})"
                    );
                }
                else if (position < 0 || position >= Player.BATTLEFIELD_SLOT_COUNT)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy card {card.Title}: invalid position {position}"
                    );
                }
                else if (player.Battlefield[position] != null)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy card {card.Title}: position {position} is already occupied"
                    );
                }
                else
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy card {card.Title}: unknown reason"
                    );
                }
                return false;
            }

            var currentPlayer = board.CurrentPlayer;

            // Try to deploy the card
            if (!currentPlayer.DeployCard(card, position))
                return false;

            // The Player.DeployCard method already sets the card to face-up
            // No need to set it again here

            // Notify listeners
            OnCardDeployed?.Invoke(card, position);
            logger?.Log($"[{CurrentPlayerId}] Card deployed: {card.Title} at position {position}");

            return true;
        }

        private void SubscribeToStrategyPlannerEvents(StrategyPlanner planner)
        {
            if (planner == null)
                return;

            // Since we're now using matchManager.DeployCard directly in the StrategyPlanner,
            // we don't need to handle card deployments here anymore as the OnCardDeployed event
            // will be triggered automatically by the DeployCard method.

            // We can still handle other decision types here if needed
            planner.OnDecisionExecuted += (decision) => {
                // Handle other decision types if needed
                // For example, card discards, attacks, etc.
            };
        }

        /// <summary>
        /// Gets the opponent of the specified player.
        /// </summary>
        /// <param name="player">The player to find the opponent for.</param>
        /// <returns>The opponent player, or null if the input is null.</returns>
        public Player GetOpponentPlayer(Player player)
        {
            if (player == null)
                return null;

            return player == Player ? Opponent : Player;
        }
    }
}
