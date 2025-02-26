using System;
using System.Threading.Tasks;
using Kardx.Utils;

namespace Kardx.Core.Strategy
{
  /// <summary>
  /// Controls the execution of strategies in the game.
  /// Acts as a factory for creating strategy providers based on the selected strategy source.
  /// </summary>
  public class StrategyController
  {
    private readonly ILogger logger;
    private IStrategyProvider strategyProvider;
    private Player player;
    private StrategySource strategySource;
    private string aiPersonality;

    // Events
    public event EventHandler<Strategy> OnStrategyDetermined;
    public event EventHandler<StrategyAction> OnStrategyActionExecuting;
    public event EventHandler<StrategyAction> OnStrategyActionExecuted;

    /// <summary>
    /// Creates a new instance of the StrategyController class.
    /// </summary>
    /// <param name="strategySource">The source of strategies to use.</param>
    /// <param name="player">The player this controller represents.</param>
    /// <param name="logger">Optional logger for debugging.</param>
    public StrategyController(StrategySource strategySource, Player player, ILogger logger = null)
    {
      this.strategySource = strategySource;
      this.player = player ?? throw new ArgumentNullException(nameof(player));
      this.logger = logger;
      this.aiPersonality = "balanced";

      // Create the appropriate strategy provider
      CreateStrategyProvider();
    }

    /// <summary>
    /// Creates a new instance of the StrategyController class with specific configuration for the AI provider.
    /// </summary>
    /// <param name="strategySource">The source of strategies to use.</param>
    /// <param name="player">The player this controller represents.</param>
    /// <param name="aiPersonality">The AI personality to use (only relevant if strategySource is AI).</param>
    /// <param name="logger">Optional logger for debugging.</param>
    public StrategyController(StrategySource strategySource, Player player, string aiPersonality, ILogger logger = null)
        : this(strategySource, player, logger)
    {
      this.aiPersonality = aiPersonality ?? throw new ArgumentNullException(nameof(aiPersonality));

      // Recreate the strategy provider with the new configuration
      if (strategySource == StrategySource.AI)
      {
        CreateStrategyProvider();
      }
    }

    /// <summary>
    /// Creates a new instance of the StrategyController class with specific configuration for the dummy provider.
    /// This constructor is kept for backward compatibility but ignores the dummyStrategyType parameter.
    /// </summary>
    /// <param name="strategySource">The source of strategies to use.</param>
    /// <param name="player">The player this controller represents.</param>
    /// <param name="dummyStrategyType">The type of dummy strategy to use (ignored).</param>
    /// <param name="logger">Optional logger for debugging.</param>
    public StrategyController(StrategySource strategySource, Player player, StrategyType dummyStrategyType, ILogger logger = null)
        : this(strategySource, player, logger)
    {
      logger?.Log("[StrategyController] Dummy strategy type is no longer used. The DummyStrategyProvider now uses a single simple strategy.");
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
      logger?.Log($"[StrategyController] Created {strategySource} strategy provider for player {player.Id}");
    }

    /// <summary>
    /// Changes the strategy source and recreates the strategy provider.
    /// </summary>
    /// <param name="newSource">The new strategy source to use.</param>
    public void ChangeStrategySource(StrategySource newSource)
    {
      if (strategySource == newSource)
        return;

      strategySource = newSource;
      CreateStrategyProvider();
    }

    /// <summary>
    /// Changes the AI personality and recreates the strategy provider if using an AI source.
    /// </summary>
    /// <param name="newPersonality">The new AI personality to use.</param>
    public void ChangeAIPersonality(string newPersonality)
    {
      if (string.IsNullOrEmpty(newPersonality) || aiPersonality == newPersonality)
        return;

      aiPersonality = newPersonality;

      if (strategySource == StrategySource.AI)
      {
        CreateStrategyProvider();
      }
    }

    /// <summary>
    /// Gets the next strategy from the strategy provider.
    /// </summary>
    /// <param name="board">The current game board.</param>
    /// <returns>A strategy containing a sequence of actions to execute.</returns>
    public async Task<Strategy> GetNextStrategyAsync(Board board)
    {
      if (board == null)
        throw new ArgumentNullException(nameof(board));

      logger?.Log($"[StrategyController] Getting next strategy for player {player.Id}");

      var strategy = await strategyProvider.GetNextStrategyAsync(board);

      // Raise the strategy determined event
      OnStrategyDetermined?.Invoke(this, strategy);

      return strategy;
    }

    /// <summary>
    /// Executes a strategy by processing each action in sequence.
    /// </summary>
    /// <param name="strategy">The strategy to execute.</param>
    /// <param name="board">The game board to execute actions on.</param>
    /// <returns>A task that completes when the strategy has been executed.</returns>
    public async Task ExecuteStrategyAsync(Strategy strategy, Board board)
    {
      if (strategy == null)
        throw new ArgumentNullException(nameof(strategy));

      if (board == null)
        throw new ArgumentNullException(nameof(board));

      logger?.Log($"[StrategyController] Executing strategy with {strategy.Actions.Count} actions");

      foreach (var action in strategy.Actions)
      {
        // Raise the action executing event
        OnStrategyActionExecuting?.Invoke(this, action);

        // Execute the action on the board
        await ExecuteActionOnBoard(action, board);

        // Raise the action executed event
        OnStrategyActionExecuted?.Invoke(this, action);
      }

      logger?.Log("[StrategyController] Strategy execution completed");
    }

    /// <summary>
    /// Executes a single action on the game board.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="board">The game board to execute the action on.</param>
    /// <returns>A task that completes when the action has been executed.</returns>
    private async Task ExecuteActionOnBoard(StrategyAction action, Board board)
    {
      logger?.Log($"[StrategyController] Executing action: {action.Type}");

      // Here you would implement the actual execution logic based on the action type
      switch (action.Type)
      {
        case StrategyActionType.DeployCard:
          // Logic to deploy a card from hand to the battlefield
          // Note: action.TargetCardId contains the hash code of the card's InstanceId
          break;

        case StrategyActionType.AttackWithCard:
          // Logic to attack with a card
          // Note: action.SourceCardId contains the hash code of the attacker's InstanceId
          // Note: action.TargetCardId contains the hash code of the target's InstanceId or 0 for the opponent player
          break;

        case StrategyActionType.UseCardAbility:
          // Logic to use a card's ability
          // Note: action.SourceCardId contains the hash code of the card's InstanceId
          // Note: action.TargetCardId contains the hash code of the target's InstanceId or 0 for the opponent player
          break;

        case StrategyActionType.MoveCard:
          // Logic to move a card to a different position
          // Note: action.SourceCardId contains the hash code of the card's InstanceId
          // Note: action.TargetCardId contains the target position
          break;

        case StrategyActionType.BuffCard:
          // Logic to buff a friendly card
          // Note: action.SourceCardId contains the hash code of the source card's InstanceId
          // Note: action.TargetCardId contains the hash code of the target card's InstanceId
          break;

        case StrategyActionType.DebuffCard:
          // Logic to debuff an enemy card
          // Note: action.SourceCardId contains the hash code of the source card's InstanceId
          // Note: action.TargetCardId contains the hash code of the target card's InstanceId
          break;

        case StrategyActionType.DrawCard:
          // Logic to draw a card from the deck
          break;

        case StrategyActionType.DiscardCard:
          // Logic to discard a card from hand
          // Note: action.TargetCardId contains the hash code of the card's InstanceId
          break;

        case StrategyActionType.ReturnCardToHand:
          // Logic to return a card from the battlefield to hand
          // Note: action.TargetCardId contains the hash code of the card's InstanceId
          break;

        case StrategyActionType.EndTurn:
          // Logic to end the turn
          board.EndTurn();
          break;

        default:
          logger?.LogError($"[StrategyController] Unknown action type: {action.Type}");
          break;
      }

      // Simulate some processing time
      await Task.Delay(100);
    }
  }

  /// <summary>
  /// Enumerates the sources of strategies that can be used by the strategy controller.
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
    Dummy
  }
}