using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Utils;
using UnityEngine;

namespace Kardx.Core.Planning
{
    /// <summary>
    /// Responsible for planning and executing strategies for players.
    /// </summary>
    public class StrategyPlanner
    {
        private readonly Kardx.Utils.ILogger logger;
        private readonly MatchManager matchManager;
        private readonly StrategySource strategySource;

        /// <summary>
        /// Event fired when a strategy has been determined.
        /// </summary>
        public event Action<Strategy> OnStrategyDetermined;

        /// <summary>
        /// Event fired when a decision is about to be executed.
        /// </summary>
        public event Action<Decision> OnDecisionExecuting;

        /// <summary>
        /// Event fired when a decision has been executed.
        /// </summary>
        public event Action<Decision> OnDecisionExecuted;

        /// <summary>
        /// Creates a new instance of the StrategyPlanner class.
        /// </summary>
        /// <param name="matchManager">The match manager to use for executing strategies.</param>
        /// <param name="strategySource">The source of strategies to use.</param>
        /// <param name="logger">Optional logger for debugging.</param>
        public StrategyPlanner(
            MatchManager matchManager,
            StrategySource strategySource = StrategySource.Dummy,
            Kardx.Utils.ILogger logger = null
        )
        {
            this.matchManager = matchManager ?? throw new ArgumentNullException(nameof(matchManager));
            this.strategySource = strategySource;
            this.logger = logger ?? new SimpleLogger("StrategyPlanner");
        }

        /// <summary>
        /// Gets and executes the next strategy for the player.
        /// </summary>
        /// <param name="board">The current game board.</param>
        public void ExecuteNextStrategy(Board board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            // Get the current player from the board
            Player currentPlayer = board.CurrentTurnPlayer;

            // Get the appropriate strategy provider for the current player
            IStrategyProvider strategyProvider = CreateStrategyProvider(currentPlayer);

            // Get the next strategy from the strategy provider
            Strategy strategy = strategyProvider.GetNextStrategy(board);

            if (strategy == null)
            {
                logger?.LogError("Strategy provider did not return a valid strategy");
                return;
            }

            // Notify listeners that a strategy has been determined
            OnStrategyDetermined?.Invoke(strategy);

            // Execute the strategy
            ExecuteStrategy(strategy, board);
        }

        /// <summary>
        /// Executes a strategy on the board.
        /// </summary>
        /// <param name="strategy">The strategy to execute.</param>
        /// <param name="board">The board to execute the strategy on.</param>
        public void ExecuteStrategy(Strategy strategy, Board board)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            if (board == null)
                throw new ArgumentNullException(nameof(board));

            logger?.Log($"Executing strategy with {strategy.Decisions.Count} decisions");

            // Execute each decision in the strategy
            foreach (var decision in strategy.Decisions)
            {
                // Notify listeners that a decision is about to be executed
                OnDecisionExecuting?.Invoke(decision);

                // Execute the decision on the board
                ExecuteActionOnBoard(decision, board);

                // Notify listeners that a decision has been executed
                OnDecisionExecuted?.Invoke(decision);
            }
        }

        /// <summary>
        /// Executes a single action on the board.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="board">The board to execute the action on.</param>
        private void ExecuteActionOnBoard(Decision action, Board board)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            // Get the current player from the board
            Player currentPlayer = board.CurrentTurnPlayer;

            // Execute the action based on its type
            switch (action.Type)
            {
                case DecisionType.DeployCard:
                    // Find the card in the player's hand
                    Card cardToDeploy = FindCardById(currentPlayer.Hand.Cards, action.TargetCardId);
                    if (cardToDeploy != null)
                    {
                        // Use the target slot from the decision
                        int targetSlot = action.TargetSlot;
                        if (targetSlot >= 0 && targetSlot < currentPlayer.Battlefield.SlotCount)
                        {
                            logger?.Log(
                                $"Attempting to deploy {currentPlayer.Id}'s card {cardToDeploy.Title} (Category: {cardToDeploy.CardType.Category}) to slot {targetSlot}"
                            );

                            bool success;
                            // Use MatchManager.DeployCard if available, otherwise fall back to direct method
                            if (matchManager != null)
                            {
                                success = matchManager.DeployCard(cardToDeploy, targetSlot);
                                logger?.Log(
                                    $"Deployed card {cardToDeploy.Title} to slot {targetSlot} via MatchManager, success: {success}"
                                );
                            }
                            else
                            {
                                logger?.LogError(
                                    "MatchManager not available, using direct Player.DeployUnitCard method which won't update UI"
                                );
                            }
                        }
                        else
                        {
                            logger?.LogError($"Invalid target slot: {targetSlot} with count {currentPlayer.Battlefield.Count}");
                        }
                    }
                    else
                    {
                        logger?.LogError(
                            $"Card with ID {action.TargetCardId} not found in player's hand"
                        );
                    }
                    break;

                case DecisionType.AttackWithCard:
                    // This would require combat logic which should be implemented in the game rules
                    logger?.Log($"Attack action not implemented yet");
                    break;

                case DecisionType.UseCardAbility:
                    // This would require ability logic which should be implemented in the game rules
                    logger?.Log($"Card ability action not implemented yet");
                    break;

                case DecisionType.EndTurn:
                    // End turn is handled by the MatchManager after strategy execution
                    logger?.Log($"End turn action");
                    matchManager.NextTurn();
                    break;

                default:
                    logger?.LogError($"Unknown action type: {action.Type}");
                    break;
            }
        }

        // Helper method to find a card by ID in a collection
        private Card FindCardById(IEnumerable<Card> cards, string cardId)
        {
            return cards.FirstOrDefault(c => c.InstanceId.ToString() == cardId);
        }

        private IStrategyProvider CreateStrategyProvider(Player player)
        {
            switch (strategySource)
            {
                case StrategySource.Dummy:
                    var provider = new DummyStrategyProvider(logger);
                    provider.Initialize(player);
                    return provider;
                default:
                    throw new ArgumentException($"Unsupported strategy source: {strategySource}");
            }
        }
    }

    /// <summary>
    /// Enum defining the source of strategies to use.
    /// </summary>
    public enum StrategySource
    {
        /// <summary>
        /// Use a dummy strategy provider that makes simple decisions.
        /// </summary>
        Dummy
    }
}
