using System.Threading.Tasks;

namespace Kardx.Core.Strategy
{
  /// <summary>
  /// Interface for strategy providers that determine the next actions for a player.
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
    /// <param name="board">The current game board state.</param>
    /// <returns>A strategy containing a sequence of actions to execute.</returns>
    Task<Strategy> GetNextStrategyAsync(Board board);
  }
}