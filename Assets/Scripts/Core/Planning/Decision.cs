using System;

namespace Kardx.Core.Planning
{
    /// <summary>
    /// Represents a single decision in a strategy, such as playing a card, attacking, or using an ability.
    /// </summary>
    public class Decision
    {
        /// <summary>
        /// Gets or sets the type of decision.
        /// </summary>
        public DecisionType Type { get; set; }

        /// <summary>
        /// Gets or sets the ID of the source card (for attack and ability decisions).
        /// </summary>
        public string SourceCardId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the target card (for play, attack, and ability decisions).
        /// </summary>
        public string TargetCardId { get; set; }

        /// <summary>
        /// Gets or sets the index of the ability to use (for ability decisions).
        /// </summary>
        public int AbilityIndex { get; set; }

        /// <summary>
        /// Gets or sets the reasoning behind this decision.
        /// </summary>
        public string Reasoning { get; set; }
    }

    /// <summary>
    /// Enumerates the types of decisions that can be taken in a strategy.
    /// </summary>
    public enum DecisionType
    {
        /// <summary>
        /// Deploy a card from hand to the battlefield.
        /// </summary>
        DeployCard,

        /// <summary>
        /// Attack an enemy card or player with a card.
        /// </summary>
        AttackWithCard,

        /// <summary>
        /// Use a card's ability.
        /// </summary>
        UseCardAbility,

        /// <summary>
        /// Move a card to a different position on the battlefield.
        /// </summary>
        MoveCard,

        /// <summary>
        /// Apply a buff to a friendly card.
        /// </summary>
        BuffCard,

        /// <summary>
        /// Apply a debuff to an enemy card.
        /// </summary>
        DebuffCard,

        /// <summary>
        /// Draw a card from the deck.
        /// </summary>
        DrawCard,

        /// <summary>
        /// Discard a card from hand.
        /// </summary>
        DiscardCard,

        /// <summary>
        /// Return a card from the battlefield to hand.
        /// </summary>
        ReturnCardToHand,

        /// <summary>
        /// End the turn.
        /// </summary>
        EndTurn,
    }
}
