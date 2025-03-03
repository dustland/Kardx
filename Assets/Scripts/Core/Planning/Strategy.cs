using System.Collections.Generic;
using System.Linq;

namespace Kardx.Core.Planning
{
    /// <summary>
    /// Represents a strategy consisting of a sequence of decisions to be executed.
    /// </summary>
    public class Strategy
    {
        /// <summary>
        /// Gets or sets the list of decisions that make up this strategy.
        /// </summary>
        public List<Decision> Decisions { get; set; } = new List<Decision>();

        /// <summary>
        /// Gets or sets a description of the overall reasoning behind this strategy.
        /// </summary>
        public string Reasoning { get; set; }

        /// <summary>
        /// Creates a new instance of the Strategy class.
        /// </summary>
        public Strategy() { }

        /// <summary>
        /// Creates a new instance of the Strategy class with the specified reasoning.
        /// </summary>
        /// <param name="reasoning">The reasoning behind this strategy.</param>
        public Strategy(string reasoning)
        {
            Reasoning = reasoning;
        }

        /// <summary>
        /// Ensures that the strategy ends with an end turn decision.
        /// </summary>
        public void EnsureEndTurn()
        {
            // Check if the last decision is already an end turn decision
            if (Decisions.Count == 0 || Decisions.Last().Type != DecisionType.EndTurn)
            {
                // Add an end turn decision
                Decisions.Add(new Decision { Type = DecisionType.EndTurn });
            }
        }

        /// <summary>
        /// Adds a deploy card decision to the strategy.
        /// </summary>
        /// <param name="cardId">The ID of the card to deploy.</param>
        /// <param name="reason">The reason for deploying this card.</param>
        /// <param name="targetSlot">The target slot to deploy the card to. Default is -1 (no specific slot).</param>
        public void AddDeployCardAction(string cardId, string reason = null, int targetSlot = -1)
        {
            Decisions.Add(
                new Decision
                {
                    Type = DecisionType.DeployCard,
                    TargetCardId = cardId,
                    TargetSlot = targetSlot,
                    Reasoning = reason,
                }
            );
        }

        /// <summary>
        /// Adds an attack decision to the strategy.
        /// </summary>
        /// <param name="sourceCardId">The ID of the attacking card.</param>
        /// <param name="targetCardId">The ID of the target card or 0 for the opponent player.</param>
        /// <param name="reason">The reason for this attack.</param>
        public void AddAttackAction(string sourceCardId, string targetCardId, string reason = null)
        {
            Decisions.Add(
                new Decision
                {
                    Type = DecisionType.AttackWithCard,
                    SourceCardId = sourceCardId,
                    TargetCardId = targetCardId,
                    Reasoning = reason,
                }
            );
        }

        /// <summary>
        /// Adds a card ability decision to the strategy.
        /// </summary>
        /// <param name="sourceCardId">The ID of the card using the ability.</param>
        /// <param name="targetCardId">The ID of the target card or 0 for the opponent player.</param>
        /// <param name="abilityIndex">The index of the ability to use.</param>
        /// <param name="reason">The reason for using this ability.</param>
        public void AddUseAbilityAction(
            string sourceCardId,
            string targetCardId,
            int abilityIndex = 0,
            string reason = null
        )
        {
            Decisions.Add(
                new Decision
                {
                    Type = DecisionType.UseCardAbility,
                    SourceCardId = sourceCardId,
                    TargetCardId = targetCardId,
                    AbilityIndex = abilityIndex,
                    Reasoning = reason,
                }
            );
        }

        /// <summary>
        /// Adds a move card decision to the strategy.
        /// </summary>
        /// <param name="sourceCardId">The ID of the card to move.</param>
        /// <param name="targetPosition">The target position (stored in TargetCardId).</param>
        /// <param name="reason">The reason for moving this card.</param>
        public void AddMoveCardAction(
            string sourceCardId,
            string targetPosition,
            string reason = null
        )
        {
            Decisions.Add(
                new Decision
                {
                    Type = DecisionType.MoveCard,
                    SourceCardId = sourceCardId,
                    TargetCardId = targetPosition, // Repurposing TargetCardId for position
                    Reasoning = reason,
                }
            );
        }

        /// <summary>
        /// Adds a buff card decision to the strategy.
        /// </summary>
        /// <param name="sourceCardId">The ID of the card applying the buff.</param>
        /// <param name="targetCardId">The ID of the target card to buff.</param>
        /// <param name="reason">The reason for buffing this card.</param>
        public void AddBuffCardAction(
            string sourceCardId,
            string targetCardId,
            string reason = null
        )
        {
            Decisions.Add(
                new Decision
                {
                    Type = DecisionType.BuffCard,
                    SourceCardId = sourceCardId,
                    TargetCardId = targetCardId,
                    Reasoning = reason,
                }
            );
        }

        /// <summary>
        /// Adds a debuff card decision to the strategy.
        /// </summary>
        /// <param name="sourceCardId">The ID of the card applying the debuff.</param>
        /// <param name="targetCardId">The ID of the target card to debuff.</param>
        /// <param name="reason">The reason for debuffing this card.</param>
        public void AddDebuffCardAction(
            string sourceCardId,
            string targetCardId,
            string reason = null
        )
        {
            Decisions.Add(
                new Decision
                {
                    Type = DecisionType.DebuffCard,
                    SourceCardId = sourceCardId,
                    TargetCardId = targetCardId,
                    Reasoning = reason,
                }
            );
        }

        /// <summary>
        /// Adds a draw card decision to the strategy.
        /// </summary>
        /// <param name="reason">The reason for drawing a card.</param>
        public void AddDrawCardAction(string reason = null)
        {
            Decisions.Add(new Decision { Type = DecisionType.DrawCard, Reasoning = reason });
        }

        /// <summary>
        /// Adds a discard card decision to the strategy.
        /// </summary>
        /// <param name="cardId">The ID of the card to discard.</param>
        /// <param name="reason">The reason for discarding this card.</param>
        public void AddDiscardCardAction(string cardId, string reason = null)
        {
            Decisions.Add(
                new Decision
                {
                    Type = DecisionType.DiscardCard,
                    TargetCardId = cardId,
                    Reasoning = reason,
                }
            );
        }

        /// <summary>
        /// Adds an end turn decision to the strategy.
        /// </summary>
        /// <param name="reason">The reason for ending the turn.</param>
        public void AddEndTurnAction(string reason = null)
        {
            Decisions.Add(new Decision { Type = DecisionType.EndTurn, Reasoning = reason });
        }
    }
}
