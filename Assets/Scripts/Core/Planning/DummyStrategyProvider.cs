using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kardx.Utils;
using UnityEngine;

namespace Kardx.Core.Planning
{
    /// <summary>
    /// A dummy strategy provider that returns simple strategies for testing purposes.
    /// </summary>
    public class DummyStrategyProvider : IStrategyProvider
    {
        private readonly Kardx.Utils.ILogger logger;
        private Player player;
        private System.Random random;

        /// <summary>
        /// Creates a new instance of the DummyStrategyProvider class.
        /// </summary>
        /// <param name="logger">Optional logger for debugging.</param>
        public DummyStrategyProvider(Kardx.Utils.ILogger logger = null)
        {
            this.logger = logger ?? new SimpleLogger("DummyStrategyProvider");
            this.random = new System.Random();
        }

        /// <summary>
        /// Initializes the strategy provider with the player it represents.
        /// </summary>
        /// <param name="player">The player this provider represents.</param>
        public void Initialize(Player player)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            logger?.Log($"Initialized for player {player.Id}");
        }

        /// <summary>
        /// Gets the next strategy for the player based on the current game state.
        /// </summary>
        /// <param name="board">The current game board.</param>
        /// <returns>A coroutine that yields a strategy containing a sequence of decisions to execute.</returns>
        public IEnumerator GetNextStrategy(Board board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            if (player == null)
                throw new InvalidOperationException(
                    "Provider not initialized. Call Initialize first."
                );

            logger?.Log($"Generating simple strategy for player {player.Id}");

            // Simulate some processing time
            yield return new WaitForSeconds(0.5f);

            // Create a simple strategy
            var strategy = CreateSimpleStrategy(board);

            // Ensure the strategy ends with an end turn decision
            EnsureEndTurnAction(strategy);

            logger?.Log($"Generated strategy with {strategy.Decisions.Count} decisions");
            yield return strategy;
        }

        /// <summary>
        /// Creates a simple strategy that makes the easiest valid moves with minimal decision-making.
        /// </summary>
        /// <param name="board">The current game board.</param>
        /// <returns>A simple strategy.</returns>
        private Strategy CreateSimpleStrategy(Board board)
        {
            var strategy = new Strategy("Make simple moves based on available cards and credits");

            // Get the current player from the board
            Player currentPlayer = board.CurrentPlayer;

            // Check if the player has cards in hand
            if (currentPlayer.Hand.Count > 0)
            {
                // Sort cards by deployment cost (cheapest first)
                var sortedCards = currentPlayer.Hand.OrderBy(c => c.DeploymentCost).ToList();

                // Try to deploy cards that the player can afford
                foreach (var card in sortedCards)
                {
                    if (currentPlayer.Credits >= card.DeploymentCost)
                    {
                        // Check if there's space on the battlefield
                        int emptySlotCount = currentPlayer.Battlefield.Count(c => c == null);
                        if (emptySlotCount > 0)
                        {
                            // Add a deploy card action
                            strategy.AddDeployCardAction(
                                card.InstanceId.ToString(),
                                $"Deploy {card.Title} (Cost: {card.DeploymentCost})"
                            );

                            // Simulate the deployment to update the player's state for subsequent decisions
                            currentPlayer.SpendCredits(card.DeploymentCost);
                            logger?.Log($"Added deploy action for {card.Title}");
                        }
                    }
                }
            }

            return strategy;
        }

        /// <summary>
        /// Ensures that the strategy ends with an end turn decision.
        /// </summary>
        /// <param name="strategy">The strategy to ensure ends with an end turn decision.</param>
        private void EnsureEndTurnAction(Strategy strategy)
        {
            strategy.EnsureEndTurn();
        }
    }

    /// <summary>
    /// Enumerates the types of strategies that can be used by the dummy strategy provider.
    /// </summary>
    public enum StrategyType
    {
        /// <summary>
        /// Makes the simplest valid moves with minimal decision-making.
        /// </summary>
        Simple,
    }
}
