using System.Collections.Generic;
using UnityEngine;
using Kardx.Models.Abilities;
using Kardx.Models.Cards;

namespace Kardx.Acting
{
    /// <summary>
    /// Interface for handling special ability effects that require custom logic
    /// </summary>
    public interface ISpecialEffectHandler
    {
        /// <summary>
        /// Unique identifier for this special effect handler
        /// </summary>
        string EffectId { get; }

        /// <summary>
        /// Execute the special effect
        /// </summary>
        /// <param name="ability">The ability being executed</param>
        /// <param name="source">The card that is the source of the ability</param>
        /// <param name="targets">The targets of the ability</param>
        /// <param name="parameters">Custom parameters for the effect</param>
        /// <returns>True if the effect was successfully executed, false otherwise</returns>
        bool ExecuteEffect(
            Ability ability,
            Card source,
            List<Card> targets,
            Dictionary<string, object> parameters
        );
    }
}
