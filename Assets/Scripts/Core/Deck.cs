using System;
using System.Collections.Generic;

namespace Kardx.Core
{
    /// <summary>
    /// Represents a player's deck of cards.
    /// </summary>
    public class Deck : CardCollection
    {
        public Deck(Player owner) : base(owner) { }
        
        public Card DrawCard()
        {
            if (cards.Count == 0)
            {
                return null;
            }
            
            var card = cards[cards.Count - 1];
            RemoveCard(card);
            return card;
        }
        
        public void Shuffle()
        {
            // Fisher-Yates shuffle
            Random rng = new Random();
            int n = cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card value = cards[k];
                cards[k] = cards[n];
                cards[n] = value;
            }
        }
    }
}
