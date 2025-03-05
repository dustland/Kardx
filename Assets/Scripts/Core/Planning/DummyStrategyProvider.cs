using System;
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
        /// <returns>A strategy containing a sequence of decisions to execute.</returns>
        public Strategy GetNextStrategy(Board board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            if (player == null)
                throw new InvalidOperationException(
                    "Provider not initialized. Call Initialize first."
                );

            logger?.Log($"Generating simple strategy for player {player.Id}");

            // Create a simple strategy
            var strategy = CreateSimpleStrategy(board);

            // Ensure the strategy ends with an end turn decision
            EnsureEndTurnAction(strategy);

            logger?.Log($"Generated strategy with {strategy.Decisions.Count} decisions");
            return strategy;
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
            Player currentPlayer = board.CurrentTurnPlayer;
            if (board.IsPlayerTurn())
            {
                logger?.LogError($"Current player is {currentPlayer.Id}, should not be using this strategy provider");
            }

            // Create a simulated state for planning without modifying the actual player state
            int simulatedCredits = currentPlayer.Credits;
            bool[] simulatedOccupiedSlots = new bool[Battlefield.SLOT_COUNT];

            // Initialize the simulated occupied slots based on the current battlefield state
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                simulatedOccupiedSlots[i] = !currentPlayer.Battlefield.IsSlotEmpty(i);
            }

            // Check if the player has cards in hand
            if (currentPlayer.Hand.Count > 0)
            {
                // Try to deploy cards that the player can afford
                foreach (var card in currentPlayer.Hand.Cards)
                {
                    if (simulatedCredits >= card.DeploymentCost)
                    {
                        // Find an empty slot on the battlefield using our simulated state
                        int emptySlot = -1;
                        for (int i = 0; i < simulatedOccupiedSlots.Length; i++)
                        {
                            if (!simulatedOccupiedSlots[i])
                            {
                                emptySlot = i;
                                break;
                            }
                        }

                        // Check if there's space on the battlefield
                        if (emptySlot >= 0)
                        {
                            // Use the new method to add a deploy card action with a target slot
                            strategy.AddDeployCardAction(
                                card.InstanceId.ToString(),
                                $"Deploy {card.Title} (Cost: {card.DeploymentCost}) to slot {emptySlot}",
                                emptySlot
                            );

                            // Update our simulated state
                            simulatedCredits -= card.DeploymentCost;
                            simulatedOccupiedSlots[emptySlot] = true;

                            logger?.Log(
                                $"Added deploy action for {card.Title} to slot {emptySlot}"
                            );
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
}
