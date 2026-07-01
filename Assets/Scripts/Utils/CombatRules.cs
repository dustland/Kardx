using System.Collections.Generic;
using System.Linq;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Models.Match;

namespace Kardx.Utils
{
    /// <summary>
    /// Combat targeting rules including Guard, Smokescreen, and frontline checks.
    /// </summary>
    public static class CombatRules
    {
        public static bool CanUnitAttack(Card attacker)
        {
            if (attacker == null || !attacker.IsUnitCard)
                return false;

            if (attacker.HasAttackedThisTurn)
                return false;

            if (attacker.DeployedThisTurn && !attacker.HasKeyword(UnitKeyword.Blitz))
                return false;

            return true;
        }

        public static bool IsValidAttackTarget(Card attacker, Card defender, Player defendingPlayer)
        {
            if (attacker == null || defender == null || defendingPlayer == null)
                return false;

            if (attacker.Owner == defendingPlayer)
                return false;

            if (attacker.Owner == null || !attacker.Owner.Battlefield.Contains(attacker))
                return false;

            if (defender.Owner != defendingPlayer)
                return false;

            if (defender.IsHeadquarters)
                return CanAttackHQ(attacker, defendingPlayer);

            if (!defendingPlayer.Battlefield.Contains(defender))
                return false;

            if (defender.HasKeyword(UnitKeyword.Smokescreen) && !defender.HasAttackedThisTurn)
                return false;

            if (!PassesGuardRestriction(attacker, defender, defendingPlayer))
                return false;

            return true;
        }

        public static bool CanAttackHQ(Card attacker, Player defendingPlayer)
        {
            if (attacker == null || defendingPlayer == null)
                return false;

            if (attacker.Owner == defendingPlayer)
                return false;

            if (attacker.Owner == null || !attacker.Owner.Battlefield.Contains(attacker))
                return false;

            if (!CanUnitAttack(attacker))
                return false;

            if (defendingPlayer.HasFrontlineUnits())
                return false;

            var hq = defendingPlayer.Headquarter;
            return hq != null && hq.IsAlive;
        }

        public static bool PassesGuardRestriction(Card attacker, Card defender, Player defendingPlayer)
        {
            var guardUnits = GetGuardUnitsOnFrontline(defendingPlayer);
            if (guardUnits.Count == 0)
                return true;

            return guardUnits.Contains(defender);
        }

        public static List<Card> GetGuardUnitsOnFrontline(Player defendingPlayer)
        {
            var guards = new List<Card>();
            if (defendingPlayer == null)
                return guards;

            for (int i = 0; i < GameConstants.FrontlineSlotCount; i++)
            {
                var card = defendingPlayer.Battlefield.GetCardAt(i);
                if (card != null && card.HasKeyword(UnitKeyword.Guard))
                    guards.Add(card);
            }

            return guards;
        }

        public static List<Card> GetValidAttackTargets(Card attacker, Player defendingPlayer)
        {
            var targets = new List<Card>();
            if (attacker == null || defendingPlayer == null)
                return targets;

            var guardUnits = GetGuardUnitsOnFrontline(defendingPlayer);
            if (guardUnits.Count > 0)
            {
                targets.AddRange(guardUnits.Where(g =>
                    !g.HasKeyword(UnitKeyword.Smokescreen) || g.HasAttackedThisTurn));
                return targets;
            }

            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                var card = defendingPlayer.Battlefield.GetCardAt(i);
                if (card != null && IsValidAttackTarget(attacker, card, defendingPlayer))
                    targets.Add(card);
            }

            if (CanAttackHQ(attacker, defendingPlayer))
            {
                targets.Add(defendingPlayer.Headquarter);
            }

            return targets;
        }
    }
}
