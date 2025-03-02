namespace Kardx.Core
{
    /// <summary>
    /// Represents a player's graveyard for destroyed cards.
    /// </summary>
    public class Graveyard : CardCollection
    {
        public Graveyard(Player owner) : base(owner) { }
    }
}
