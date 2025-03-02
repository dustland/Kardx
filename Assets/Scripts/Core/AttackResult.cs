using System;

namespace Kardx.Core
{
    /// <summary>
    /// Represents the result of an attack between two cards.
    /// </summary>
    public class AttackResult : EventArgs
    {
        /// <summary>
        /// The attacking card
        /// </summary>
        public Card Attacker { get; private set; }
        
        /// <summary>
        /// The defending card
        /// </summary>
        public Card Defender { get; private set; }
        
        /// <summary>
        /// The damage dealt to the defender
        /// </summary>
        public int DamageDealt { get; private set; }
        
        /// <summary>
        /// Whether the defender died as a result of the attack
        /// </summary>
        public bool DefenderDied { get; private set; }
        
        /// <summary>
        /// Creates a new AttackResult instance
        /// </summary>
        /// <param name="attacker">The attacking card</param>
        /// <param name="defender">The defending card</param>
        /// <param name="damageDealt">The damage dealt to the defender</param>
        /// <param name="defenderDied">Whether the defender died as a result of the attack</param>
        public AttackResult(Card attacker, Card defender, int damageDealt, bool defenderDied)
        {
            Attacker = attacker;
            Defender = defender;
            DamageDealt = damageDealt;
            DefenderDied = defenderDied;
        }
    }
}
