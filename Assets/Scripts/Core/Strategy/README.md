# Strategy System

The Strategy system is responsible for managing AI opponents and player interactions in the game. It provides a flexible way to determine and execute game actions based on the current game state.

## Core Components

### Strategy Controller

The `StrategyController` is the main entry point for the strategy system. It creates and manages strategy providers based on the selected strategy source (AI, Remote, or Dummy).

```csharp
// Create a strategy controller
var strategyController = new StrategyController();

// Initialize with a player
strategyController.Initialize(player);

// Set the strategy source
strategyController.SetStrategySource(StrategySource.AI);

// Get the next strategy
Strategy strategy = await strategyController.GetNextStrategyAsync(board);

// Execute the strategy
await strategyController.ExecuteStrategyAsync(strategy);
```

### Strategy Providers

Strategy providers implement the `IStrategyProvider` interface and are responsible for determining the next strategy based on the current game state.

```csharp
public interface IStrategyProvider
{
    void Initialize(Player player);
    Task<Strategy> GetNextStrategyAsync(Board board);
}
```

The system includes the following strategy providers:

- **AIStrategyProvider**: Uses AI to determine the next strategy.
- **RemoteStrategyProvider**: Gets strategies from a remote source (e.g., a server).
- **DummyStrategyProvider**: Returns predefined strategies for testing purposes.

### Strategy and Actions

The `Strategy` class represents a sequence of actions to be executed. It contains a list of `StrategyAction` objects and provides methods to add different types of actions.

```csharp
// Create a new strategy
var strategy = new Strategy("Aggressive strategy: Focus on attacking");

// Add actions to the strategy
strategy.AddDeployCardAction(cardId, "Deploying a strong minion");
strategy.AddAttackAction(sourceCardId, targetCardId, "Attacking enemy minion");
strategy.AddUseAbilityAction(sourceCardId, targetCardId, abilityIndex, "Using ability to deal damage");
strategy.AddMoveCardAction(sourceCardId, targetPosition, "Moving card to better position");
strategy.AddBuffCardAction(sourceCardId, targetCardId, "Buffing friendly minion");
strategy.AddDebuffCardAction(sourceCardId, targetCardId, "Debuffing enemy minion");
strategy.AddDrawCardAction("Drawing a card for more options");
strategy.AddDiscardCardAction(cardId, "Discarding a card to make room");
strategy.AddReturnCardToHandAction(cardId, "Returning damaged card to hand");
strategy.AddEndTurnAction("End of turn");
```

The `StrategyAction` class represents a single action in the game, with the following action types:

- **DeployCard**: Deploy a card from hand to the battlefield
- **AttackWithCard**: Attack an enemy card or player with a card
- **UseCardAbility**: Use a card's ability
- **MoveCard**: Move a card to a different position on the battlefield
- **BuffCard**: Apply a buff to a friendly card
- **DebuffCard**: Apply a debuff to an enemy card
- **DrawCard**: Draw a card from the deck
- **DiscardCard**: Discard a card from hand
- **ReturnCardToHand**: Return a card from the battlefield to hand
- **EndTurn**: End the turn

## Usage Examples

### Setting up the MatchManager

```csharp
// Create a match manager
var matchManager = new MatchManager();

// Set the strategy source
matchManager.SetStrategySource(StrategySource.Dummy);

// Set the dummy strategy type (if using the dummy provider)
matchManager.SetDummyStrategyType(StrategyType.Aggressive);

// Set the AI personality (if using the AI provider)
matchManager.SetAIPersonality("aggressive");

// Start a match
matchManager.StartMatch(player1, player2);
```

### Using the DummyStrategyProvider

```csharp
// Create a dummy strategy provider
var dummyProvider = new DummyStrategyProvider(StrategyType.Aggressive);

// Initialize with a player
dummyProvider.Initialize(player);

// Get the next strategy
Strategy strategy = await dummyProvider.GetNextStrategyAsync(board);
```

### Changing Strategy Sources at Runtime

```csharp
// Switch to AI strategy
matchManager.SetStrategySource(StrategySource.AI);

// Switch to dummy strategy
matchManager.SetStrategySource(StrategySource.Dummy);
matchManager.SetDummyStrategyType(StrategyType.Defensive);

// Switch to remote strategy
matchManager.SetStrategySource(StrategySource.Remote);
```

## Events

The strategy system provides several events that you can subscribe to:

```csharp
// Subscribe to strategy events
matchManager.OnStrategyDetermined += (strategy) =>
    Debug.Log($"Strategy determined: {strategy.Reasoning}");

matchManager.OnStrategyActionExecuting += (action) =>
    Debug.Log($"Executing action: {action.Type}");

matchManager.OnStrategyActionExecuted += (action) =>
    Debug.Log($"Action executed: {action.Type}");
```

## Integration with Board

The strategy system now directly interacts with the `Board` class instead of a separate `GameState` class. This simplifies the architecture and reduces duplication of game state information.

```csharp
// Get the next strategy based on the current board state
Strategy strategy = await strategyProvider.GetNextStrategyAsync(board);
```

## Future Enhancements

- Add more sophisticated AI strategy providers
- Implement machine learning-based strategy determination
- Add support for multiplayer strategies
