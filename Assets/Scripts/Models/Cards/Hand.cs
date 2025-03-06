using System;
using System.Collections.Generic;
using Kardx.Models.Match;

namespace Kardx.Models.Cards
{
    /// <summary>
    /// Represents a player's hand of cards.
    /// </summary>
    public class Hand : CardCollection
    {
        private const int MAX_HAND_SIZE = 5;

        public Hand(Player owner) : base(owner) { }
        
        public override void AddCard(Card card)
        {
            if (cards.Count >= MAX_HAND_SIZE)
            {
                throw new InvalidOperationException("Hand is full");
            }
            
            base.AddCard(card);
        }
        
        public bool IsFull => cards.Count >= MAX_HAND_SIZE;
        
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
