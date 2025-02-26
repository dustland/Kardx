using System.Collections.Generic;
using System.Linq;

namespace Kardx.Core.Strategy
{
  /// <summary>
  /// Represents a strategy consisting of a sequence of actions to be executed.
  /// </summary>
  public class Strategy
  {
    /// <summary>
    /// Gets or sets the list of actions that make up this strategy.
    /// </summary>
    public List<StrategyAction> Actions { get; set; } = new List<StrategyAction>();

    /// <summary>
    /// Gets or sets a description of the overall reasoning behind this strategy.
    /// </summary>
    public string Reasoning { get; set; }

    /// <summary>
    /// Creates a new instance of the Strategy class.
    /// </summary>
    public Strategy()
    {
    }

    /// <summary>
    /// Creates a new instance of the Strategy class with the specified reasoning.
    /// </summary>
    /// <param name="reasoning">The reasoning behind this strategy.</param>
    public Strategy(string reasoning)
    {
      Reasoning = reasoning;
    }

    /// <summary>
    /// Ensures that the strategy ends with an end turn action.
    /// </summary>
    public void EnsureEndTurn()
    {
      // Check if the last action is already an end turn action
      if (Actions.Count == 0 || Actions.Last().Type != StrategyActionType.EndTurn)
      {
        // Add an end turn action
        Actions.Add(new StrategyAction
        {
          Type = StrategyActionType.EndTurn
        });
      }
    }

    /// <summary>
    /// Adds a deploy card action to the strategy.
    /// </summary>
    /// <param name="cardId">The ID of the card to deploy.</param>
    /// <param name="reason">The reason for deploying this card.</param>
    public void AddDeployCardAction(int cardId, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.DeployCard,
        TargetCardId = cardId,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds an attack action to the strategy.
    /// </summary>
    /// <param name="sourceCardId">The ID of the attacking card.</param>
    /// <param name="targetCardId">The ID of the target card or 0 for the opponent player.</param>
    /// <param name="reason">The reason for this attack.</param>
    public void AddAttackAction(int sourceCardId, int targetCardId, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.AttackWithCard,
        SourceCardId = sourceCardId,
        TargetCardId = targetCardId,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds a card ability action to the strategy.
    /// </summary>
    /// <param name="sourceCardId">The ID of the card using the ability.</param>
    /// <param name="targetCardId">The ID of the target card or 0 for the opponent player.</param>
    /// <param name="abilityIndex">The index of the ability to use.</param>
    /// <param name="reason">The reason for using this ability.</param>
    public void AddUseAbilityAction(int sourceCardId, int targetCardId, int abilityIndex = 0, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.UseCardAbility,
        SourceCardId = sourceCardId,
        TargetCardId = targetCardId,
        AbilityIndex = abilityIndex,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds a move card action to the strategy.
    /// </summary>
    /// <param name="sourceCardId">The ID of the card to move.</param>
    /// <param name="targetPosition">The target position (stored in TargetCardId).</param>
    /// <param name="reason">The reason for moving this card.</param>
    public void AddMoveCardAction(int sourceCardId, int targetPosition, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.MoveCard,
        SourceCardId = sourceCardId,
        TargetCardId = targetPosition, // Repurposing TargetCardId for position
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds a buff card action to the strategy.
    /// </summary>
    /// <param name="sourceCardId">The ID of the card applying the buff.</param>
    /// <param name="targetCardId">The ID of the target card to buff.</param>
    /// <param name="reason">The reason for buffing this card.</param>
    public void AddBuffCardAction(int sourceCardId, int targetCardId, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.BuffCard,
        SourceCardId = sourceCardId,
        TargetCardId = targetCardId,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds a debuff card action to the strategy.
    /// </summary>
    /// <param name="sourceCardId">The ID of the card applying the debuff.</param>
    /// <param name="targetCardId">The ID of the target card to debuff.</param>
    /// <param name="reason">The reason for debuffing this card.</param>
    public void AddDebuffCardAction(int sourceCardId, int targetCardId, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.DebuffCard,
        SourceCardId = sourceCardId,
        TargetCardId = targetCardId,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds a draw card action to the strategy.
    /// </summary>
    /// <param name="reason">The reason for drawing a card.</param>
    public void AddDrawCardAction(string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.DrawCard,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds a discard card action to the strategy.
    /// </summary>
    /// <param name="cardId">The ID of the card to discard.</param>
    /// <param name="reason">The reason for discarding this card.</param>
    public void AddDiscardCardAction(int cardId, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.DiscardCard,
        TargetCardId = cardId,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds a return card to hand action to the strategy.
    /// </summary>
    /// <param name="cardId">The ID of the card to return to hand.</param>
    /// <param name="reason">The reason for returning this card to hand.</param>
    public void AddReturnCardToHandAction(int cardId, string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.ReturnCardToHand,
        TargetCardId = cardId,
        Reasoning = reason
      });
    }

    /// <summary>
    /// Adds an end turn action to the strategy.
    /// </summary>
    /// <param name="reason">The reason for ending the turn.</param>
    public void AddEndTurnAction(string reason = null)
    {
      Actions.Add(new StrategyAction
      {
        Type = StrategyActionType.EndTurn,
        Reasoning = reason
      });
    }
  }
}