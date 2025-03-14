using UnityEngine;
using Kardx.Models.Cards;
using Kardx.Views.Cards;
using Kardx.Models.Match;

namespace Kardx.Views.Match
{
    /// <summary>
    /// Base class for battlefield views.
    /// </summary>
    public abstract class BaseBattlefieldView : MonoBehaviour
    {
        protected MatchManager matchManager;

        /// <summary>
        /// Initialize the battlefield view with a reference to the match manager.
        /// </summary>
        /// <param name="matchManager">The match manager that controls the game state</param>
        public virtual void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;
        }

        /// <summary>
        /// Updates the battlefield display based on the battlefield model.
        /// </summary>
        /// <param name="battlefield">The battlefield data to display</param>
        public abstract void UpdateBattlefield();


        /// <summary>
        /// Removes the given card from the battlefield.
        /// </summary>
        /// <param name="card">The card to remove</param>
        public abstract void RemoveCard(Card card);

        /// <summary>
        /// Clears all highlights from the battlefield.
        /// </summary>
        public abstract void ClearCardHighlights();

        /// <summary>
        /// Gets the match manager instance.
        /// </summary>
        public MatchManager GetMatchManager()
        {
            return matchManager;
        }

        /// <summary>
        /// Gets all card views currently on this battlefield
        /// </summary>
        /// <returns>Array of CardView components on this battlefield</returns>
        public abstract CardView[] GetCardViews();

        /// <summary>
        /// Gets the card view at the specified position
        /// </summary>
        /// <param name="position">Position on the battlefield (0-based index)</param>
        /// <returns>CardView at the specified position, or null if no card exists there</returns>
        public abstract CardView GetCardViewAt(int position);
    }
}
