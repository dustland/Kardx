using Kardx.Models;
using Kardx.Models.Match;

namespace Kardx.Models.Cards
{
    /// <summary>
    /// Represents a player's discard pile.
    /// </summary>
    public class DiscardPile : CardCollection
    {
        public DiscardPile(Player owner) : base(owner) { }

        protected override ZoneType Zone => ZoneType.DiscardPile;
    }
}
