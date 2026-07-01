using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Executes model actions for a resolved drag target. UI reparenting is handled by the caller when needed.
    /// </summary>
    public static class CardDropResolver
    {
        public static bool TryExecute(
            CardDragMode mode,
            Card card,
            CardDragTarget target,
            MatchManager matchManager,
            CardView cardView,
            out bool keepFloatingTransform
        )
        {
            keepFloatingTransform = false;

            if (card == null || matchManager == null || target.Kind == CardDragTargetKind.None)
                return false;

            switch (mode)
            {
                case CardDragMode.DeployUnit:
                    if (
                        target.Kind == CardDragTargetKind.PlayerBattlefieldSlot
                        && matchManager.DeployCard(card, target.SlotIndex)
                    )
                    {
                        keepFloatingTransform = true;
                        return true;
                    }
                    break;

                case CardDragMode.PlayOrder:
                    if (
                        target.Kind == CardDragTargetKind.OrderZone
                        && matchManager.DeployCard(card, -1)
                    )
                    {
                        keepFloatingTransform = true;
                        return true;
                    }
                    break;

                case CardDragMode.PlayCountermeasure:
                    if (
                        target.Kind == CardDragTargetKind.CountermeasureZone
                        && matchManager.PlayCountermeasure(card)
                    )
                    {
                        keepFloatingTransform = true;
                        return true;
                    }
                    break;

                case CardDragMode.BattlefieldAction:
                    switch (target.Kind)
                    {
                        case CardDragTargetKind.OpponentBattlefieldSlot:
                            return matchManager.InitiateAttack(card, target.DefenderCard);

                        case CardDragTargetKind.OpponentHeadquarters:
                            return matchManager.InitiateAttackOnHQ(card, matchManager.Opponent);

                        case CardDragTargetKind.PlayerBattlefieldSlot:
                            if (matchManager.MoveUnit(card, target.SlotIndex))
                            {
                                keepFloatingTransform = true;
                                return true;
                            }
                            break;
                    }
                    break;
            }

            return false;
        }
    }
}
