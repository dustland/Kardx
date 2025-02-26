using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kardx.Core;
using Kardx.Core.Strategy;
using Kardx.Utils;
using Newtonsoft.Json;

namespace Kardx.Core
{
    /// <summary>
    /// Manages the game match, handling turns and player actions.
    /// </summary>
    public class MatchManager
    {
        private readonly ILogger logger;
        private Board board;
        private StrategyController strategyController;
        private StrategySource strategySource;
        private string aiPersonality;
        private readonly int startingHandSize = 4;
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
        public event Action<Kardx.Core.Strategy.Strategy> OnStrategyDetermined;
        public event Action<StrategyAction> OnStrategyActionExecuting;
        public event Action<StrategyAction> OnStrategyActionExecuted;
        public event EventHandler<Player> OnTurnStarted;
        public event EventHandler<Player> OnTurnEnded;
        public event Action<string> OnMatchStarted;
        public event Action<string> OnMatchEnded;

        /// <summary>
        /// Creates a new instance of the MatchManager class.
        /// </summary>
        /// <param name="logger">Optional logger for debugging.</param>
        public MatchManager(ILogger logger = null)
        {
            this.logger = logger;
            this.strategySource = StrategySource.Dummy;
            this.aiPersonality = "balanced";
        }

        /// <summary>
        /// Starts a new match with the specified players.
        /// </summary>
        /// <param name="player1">The first player.</param>
        /// <param name="player2">The second player.</param>
        public void StartMatch(Player player1, Player player2)
        {
            if (player1 == null)
                throw new ArgumentNullException(nameof(player1));

            if (player2 == null)
                throw new ArgumentNullException(nameof(player2));

            // Create a new board
            board = new Board(player1, player2);

            // Create the strategy controller for the opponent (player2)
            CreateStrategyController(player2);

            // Subscribe to strategy controller events
            SubscribeToStrategyEvents();

            logger?.Log($"[MatchManager] Match started between {player1.Id} and {player2.Id}");

            // Start the first turn
            StartTurn(board.CurrentPlayer);
        }

        /// <summary>
        /// Creates the strategy controller for the opponent.
        /// </summary>
        /// <param name="opponent">The opponent player.</param>
        private void CreateStrategyController(Player opponent)
        {
            // Create the strategy controller based on the selected strategy source
            switch (strategySource)
            {
                case StrategySource.AI:
                    strategyController = new StrategyController(strategySource, opponent, aiPersonality, logger);
                    break;
                case StrategySource.Dummy:
                    strategyController = new StrategyController(strategySource, opponent, logger);
                    break;
                case StrategySource.Remote:
                    strategyController = new StrategyController(strategySource, opponent, logger);
                    break;
                default:
                    throw new ArgumentException($"Unsupported strategy source: {strategySource}");
            }
        }

        /// <summary>
        /// Subscribes to the strategy controller events.
        /// </summary>
        private void SubscribeToStrategyEvents()
        {
            if (strategyController == null)
                return;

            strategyController.OnStrategyDetermined += (sender, strategy) =>
                OnStrategyDetermined?.Invoke(strategy);

            strategyController.OnStrategyActionExecuting += (sender, action) =>
                OnStrategyActionExecuting?.Invoke(action);

            strategyController.OnStrategyActionExecuted += (sender, action) =>
                OnStrategyActionExecuted?.Invoke(action);
        }

        /// <summary>
        /// Sets the strategy source to use for the opponent.
        /// </summary>
        /// <param name="source">The strategy source to use.</param>
        public void SetStrategySource(StrategySource source)
        {
            strategySource = source;

            // If a match is already in progress, update the strategy controller
            if (board != null && strategyController != null)
            {
                strategyController.ChangeStrategySource(source);
            }
        }

        /// <summary>
        /// This method is kept for backward compatibility but does nothing since the dummy strategy type is no longer used.
        /// </summary>
        /// <param name="type">The dummy strategy type (ignored).</param>
        public void SetDummyStrategyType(StrategyType type)
        {
            logger?.Log("[MatchManager] Dummy strategy type is no longer used. The DummyStrategyProvider now uses a single simple strategy.");

            // If a match is already in progress and using a dummy strategy, call the no-op method on the controller for consistency
            if (board != null && strategyController != null && strategySource == StrategySource.Dummy)
            {
                strategyController.ChangeDummyStrategyType(type);
            }
        }

        /// <summary>
        /// Sets the AI personality to use for the opponent.
        /// </summary>
        /// <param name="personality">The AI personality to use.</param>
        public void SetAIPersonality(string personality)
        {
            if (string.IsNullOrEmpty(personality))
                throw new ArgumentException("Personality cannot be null or empty", nameof(personality));

            aiPersonality = personality;

            // If a match is already in progress and using an AI strategy, update the strategy controller
            if (board != null && strategyController != null && strategySource == StrategySource.AI)
            {
                strategyController.ChangeAIPersonality(personality);
            }
        }

        /// <summary>
        /// Starts a new turn for the specified player.
        /// </summary>
        /// <param name="player">The player whose turn is starting.</param>
        private void StartTurn(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            logger?.Log($"[MatchManager] Starting turn for player {player.Id}");

            // Notify listeners
            OnTurnStarted?.Invoke(this, player);

            // If it's the opponent's turn, process it automatically
            if (player.Id == board.Player2.Id)
            {
                ProcessOpponentTurnAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        logger?.LogError($"[MatchManager] Error processing opponent turn: {t.Exception}");
                    }
                });
            }
        }

        /// <summary>
        /// Ends the current turn and starts the next player's turn.
        /// </summary>
        public void EndTurn()
        {
            if (board == null)
                throw new InvalidOperationException("Match not started");

            var currentPlayer = board.CurrentPlayer;
            logger?.Log($"[MatchManager] Ending turn for player {currentPlayer.Id}");

            // Notify listeners
            OnTurnEnded?.Invoke(this, currentPlayer);

            // Switch to the next player
            board.EndTurn();

            // Start the next turn
            StartTurn(board.CurrentPlayer);
        }

        /// <summary>
        /// Processes the opponent's turn automatically.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ProcessOpponentTurnAsync()
        {
            if (board == null)
                throw new InvalidOperationException("Match not started");

            if (strategyController == null)
                throw new InvalidOperationException("Strategy controller not initialized");

            logger?.Log("[MatchManager] Processing opponent turn");

            try
            {
                // Get the next strategy from the strategy controller
                Kardx.Core.Strategy.Strategy strategy = await strategyController.GetNextStrategyAsync(board);

                // Notify listeners that a strategy has been determined
                OnStrategyDetermined?.Invoke(strategy);

                // Execute the strategy
                await strategyController.ExecuteStrategyAsync(strategy, board);
            }
            catch (Exception ex)
            {
                logger?.LogError($"[MatchManager] Error processing opponent turn: {ex.Message}");

                // End the turn if an error occurs
                EndTurn();
            }
        }

        /// <summary>
        /// Gets the current board state.
        /// </summary>
        /// <returns>The current board state.</returns>
        public Board GetBoard()
        {
            return board;
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

            var currentPlayer = GetCurrentPlayer();
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
