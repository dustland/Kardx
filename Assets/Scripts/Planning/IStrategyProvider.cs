using System.Collections.Generic;
using Kardx.Models;
using Kardx.Models.Match;

namespace Kardx.Planning
{
    /// <summary>
    /// Interface for strategy providers that generate strategies for players.
    /// </summary>
    public interface IStrategyProvider
    {
        /// <summary>
        /// Initializes the strategy provider with the player it represents.
        /// </summary>
        /// <param name="player">The player this provider represents.</param>
        void Initialize(Player player);

        /// <summary>
        /// Gets the next strategy for the player based on the current game state.
        /// </summary>
        /// <param name="board">The current game board.</param>
        /// <returns>A strategy containing a sequence of decisions to execute.</returns>
        Strategy GetNextStrategy(Board board);
    }
}
