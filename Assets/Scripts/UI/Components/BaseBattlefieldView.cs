using UnityEngine;
using Kardx.Core;

namespace Kardx.UI.Components
{
    /// <summary>
    /// Base class for battlefield views.
    /// </summary>
    public abstract class BaseBattlefieldView : MonoBehaviour
    {
        /// <summary>
        /// Updates the battlefield display based on the battlefield model.
        /// </summary>
        /// <param name="battlefield">The battlefield data to display</param>
        public abstract void UpdateBattlefield(Battlefield battlefield);
        
        /// <summary>
        /// Clears all highlights from the battlefield.
        /// </summary>
        public abstract void ClearCardHighlights();
    }
}
