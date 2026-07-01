using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Utils;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Determines which drag mode applies to a card in the current game state.
    /// </summary>
    public static class CardDragCapability
    {
        public static CardDragMode ResolveMode(Card card, MatchManager matchManager)
        {
            if (card == null || matchManager == null)
                return CardDragMode.None;

            if (card.Owner == null || card.Owner.IsOpponent)
                return CardDragMode.None;

            if (!matchManager.IsPlayerTurn())
                return CardDragMode.None;

            bool inHand = card.Owner.Hand.Contains(card);
            bool onBattlefield = card.Owner.Battlefield.Contains(card);

            if (inHand)
            {
                if (card.IsUnitCard && matchManager.CanDeployUnitCard(card))
                    return CardDragMode.DeployUnit;

                if (card.IsOrderCard && matchManager.CanDeployOrderCard(card))
                    return CardDragMode.PlayOrder;

                if (
                    card.CardType.Category == CardCategory.Countermeasure
                    && matchManager.HasPendingEnemyOrder
                    && matchManager.CanPlayCountermeasure(card)
                )
                    return CardDragMode.PlayCountermeasure;

                return CardDragMode.None;
            }

            if (onBattlefield && (CanAttack(card, matchManager) || CanMove(card, matchManager)))
                return CardDragMode.BattlefieldAction;

            return CardDragMode.None;
        }

        public static bool CanAttack(Card card, MatchManager matchManager)
        {
            if (card == null || matchManager == null)
                return false;

            if (!matchManager.Player.Battlefield.Contains(card))
                return false;

            if (!CombatRules.CanUnitAttack(card))
                return false;

            return card.OperationCost <= matchManager.Player.Credits;
        }

        public static bool CanMove(Card card, MatchManager matchManager)
        {
            if (card == null || matchManager == null)
                return false;

            if (!matchManager.Player.Battlefield.Contains(card))
                return false;

            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                if (matchManager.CanMoveUnit(card, i))
                    return true;
            }

            return false;
        }
    }
}
