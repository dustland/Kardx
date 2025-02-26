using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kardx.Utils;
using UnityEngine;

namespace Kardx.Core.Planning
{
    /// <summary>
    /// Plans and executes strategies in the game.
    /// Acts as a factory for creating strategy providers based on the selected strategy source.
    /// </summary>
    public class StrategyPlanner
    {
        private readonly Kardx.Utils.ILogger logger;
        private IStrategyProvider strategyProvider;
        private Player player;
        private StrategySource strategySource;

        // Events
        public event Action<Strategy> OnStrategyDetermined;
        public event Action<Decision> OnDecisionExecuting;
        public event Action<Decision> OnDecisionExecuted;

        /// <summary>
        /// Creates a new instance of the StrategyPlanner class.
        /// </summary>
        /// <param name="strategySource">The source of strategies to use.</param>
        /// <param name="player">The player this planner represents.</param>
        /// <param name="logger">Optional logger for debugging.</param>
        public StrategyPlanner(
            StrategySource strategySource,
            Player player,
            Kardx.Utils.ILogger logger = null
        )
        {
            this.strategySource = strategySource;
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            // Use SimpleLogger if no logger is provided
            this.logger = logger ?? new SimpleLogger("StrategyPlanner");

            // Create the appropriate strategy provider
            CreateStrategyProvider();
        }

        /// <summary>
        /// Creates the appropriate strategy provider based on the strategy source.
        /// </summary>
        private void CreateStrategyProvider()
        {
            switch (strategySource)
            {
                case StrategySource.Dummy:
                    strategyProvider = new DummyStrategyProvider(logger);
                    break;
                default:
                    throw new ArgumentException($"Unsupported strategy source: {strategySource}");
            }

            // Initialize the strategy provider
            strategyProvider.Initialize(player);
            logger?.Log($"Created {strategySource} strategy provider for player {player.Id}");
        }

        /// <summary>
        /// Gets and executes the next strategy for the player using coroutines.
        /// </summary>
        /// <param name="board">The current game board.</param>
        /// <returns>A coroutine enumerator.</returns>
        public IEnumerator ExecuteNextStrategyCoroutine(Board board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            logger?.Log("Executing next strategy");

            // Get the next strategy from the strategy provider using coroutines
            var strategyCoroutine = strategyProvider.GetNextStrategy(board);
            yield return strategyCoroutine;

            // Get the strategy from the last yielded value
            Strategy strategy = strategyCoroutine.Current as Strategy;
            if (strategy == null)
            {
                logger?.LogError("Strategy provider did not return a valid strategy");
                yield break;
            }

            // Notify listeners that a strategy has been determined
            OnStrategyDetermined?.Invoke(strategy);

            // Execute the strategy using coroutines
            yield return ExecuteStrategyCoroutine(strategy, board);
        }

        /// <summary>
        /// Executes a strategy on the board using coroutines.
        /// </summary>
        /// <param name="strategy">The strategy to execute.</param>
        /// <param name="board">The board to execute the strategy on.</param>
        /// <returns>A coroutine enumerator.</returns>
        public IEnumerator ExecuteStrategyCoroutine(Strategy strategy, Board board)
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
                yield return ExecuteActionOnBoardCoroutine(decision, board);

                // Notify listeners that a decision has been executed
                OnDecisionExecuted?.Invoke(decision);

                // Add a small delay between decisions for visual effect
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// Executes a single action on the board using coroutines.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="board">The board to execute the action on.</param>
        /// <returns>A coroutine enumerator.</returns>
        private IEnumerator ExecuteActionOnBoardCoroutine(Decision action, Board board)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            // Get the current player from the board
            Player currentPlayer = board.CurrentPlayer;

            // Execute the action based on its type
            switch (action.Type)
            {
                case DecisionType.DeployCard:
                    // Find the card in the player's hand
                    Card cardToDeploy = FindCardById(currentPlayer.Hand, action.TargetCardId);
                    if (cardToDeploy != null)
                    {
                        // Find an empty slot (for simplicity, use the first available)
                        int emptySlot = FindEmptySlot(currentPlayer.Battlefield);
                        if (emptySlot >= 0)
                        {
                            currentPlayer.DeployCard(cardToDeploy, emptySlot);
                            logger?.Log($"Deployed card {cardToDeploy.Title} to slot {emptySlot}");
                        }
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

                case DecisionType.MoveCard:
                    // This would require moving cards between slots
                    logger?.Log($"Move card action not implemented yet");
                    break;

                case DecisionType.BuffCard:
                    // This would require buff logic which should be implemented in the game rules
                    logger?.Log($"Buff card action not implemented yet");
                    break;

                case DecisionType.DebuffCard:
                    // This would require debuff logic which should be implemented in the game rules
                    logger?.Log($"Debuff card action not implemented yet");
                    break;

                case DecisionType.DrawCard:
                    // Draw a card from the deck
                    Card drawnCard = currentPlayer.DrawCard();
                    if (drawnCard != null)
                    {
                        logger?.Log($"Drew card: {drawnCard.Title}");
                    }
                    break;

                case DecisionType.DiscardCard:
                    // Find the card in the player's hand
                    Card cardToDiscard = FindCardById(currentPlayer.Hand, action.TargetCardId);
                    if (cardToDiscard != null)
                    {
                        currentPlayer.DiscardFromHand(cardToDiscard);
                        logger?.Log($"Discarded card: {cardToDiscard.Title}");
                    }
                    break;

                case DecisionType.ReturnCardToHand:
                    // This would require returning a card from battlefield to hand
                    logger?.Log($"Return card to hand action not implemented yet");
                    break;

                case DecisionType.EndTurn:
                    // End turn is handled by the MatchManager after strategy execution
                    logger?.Log($"End turn action");
                    break;

                default:
                    logger?.LogError($"Unknown action type: {action.Type}");
                    break;
            }

            yield return null;
        }

        // Helper method to find a card by ID in a collection
        private Card FindCardById(IEnumerable<Card> cards, string cardId)
        {
            return cards.FirstOrDefault(c => c.InstanceId.ToString() == cardId);
        }

        // Helper method to find an empty slot in the battlefield
        private int FindEmptySlot(IReadOnlyList<Card> battlefield)
        {
            for (int i = 0; i < battlefield.Count; i++)
            {
                if (battlefield[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    /// <summary>
    /// Enumerates the sources of strategies that can be used by the strategy planner.
    /// </summary>
    public enum StrategySource
    {
        /// <summary>
        /// Uses AI to determine strategies.
        /// </summary>
        AI,

        /// <summary>
        /// Uses a remote player to determine strategies.
        /// </summary>
        Remote,

        /// <summary>
        /// Uses predefined strategies for testing.
        /// </summary>
        Dummy,
    }
}
