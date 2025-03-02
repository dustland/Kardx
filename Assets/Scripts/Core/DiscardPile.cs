namespace Kardx.Core
{
    /// <summary>
    /// Represents a player's discard pile.
    /// </summary>
    public class DiscardPile : CardCollection
    {
        public DiscardPile(Player owner) : base(owner) { }
        
        public Card DrawTopCard()
        {
            if (cards.Count == 0)
            {
                return null;
            }
            
            var card = cards[cards.Count - 1];
            RemoveCard(card);
            return card;
        }
    }
}
