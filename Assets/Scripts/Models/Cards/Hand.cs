using System;
using System.Collections.Generic;
using Kardx.Models;
using Kardx.Models.Match;

namespace Kardx.Models.Cards
{
    /// <summary>
    /// Represents a player's hand of cards.
    /// </summary>
    public class Hand : CardCollection
    {
        public Hand(Player owner) : base(owner) { }

        protected override ZoneType Zone => ZoneType.Hand;
        
        public override void AddCard(Card card)
        {
            if (cards.Count >= GameConstants.MaxHandSize)
            {
                throw new InvalidOperationException("Hand is full");
            }
            
            base.AddCard(card);
        }
        
        public bool IsFull => cards.Count >= GameConstants.MaxHandSize;
        
        /// <summary>
        /// Gets all cards in the player's hand.
        /// </summary>
        /// <returns>A list of cards in the hand.</returns>
        public List<Card> GetCards()
        {
            return new List<Card>(cards);
        }
    }
}
