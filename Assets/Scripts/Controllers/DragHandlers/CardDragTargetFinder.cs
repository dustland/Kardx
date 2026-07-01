using System.Collections.Generic;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Match;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Resolves the topmost valid drop target under the pointer for the active drag mode.
    /// </summary>
    public static class CardDragTargetFinder
    {
        public static CardDragTarget Find(
            Vector2 screenPosition,
            CardDragMode mode,
            MatchManager matchManager,
            Card sourceCard
        )
        {
            if (mode == CardDragMode.None || matchManager == null || sourceCard == null)
                return CardDragTarget.None;

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(
                new PointerEventData(EventSystem.current) { position = screenPosition },
                results
            );

            foreach (var hit in results)
            {
                var target = ParseHit(hit.gameObject, mode, matchManager, sourceCard);
                if (target.Kind != CardDragTargetKind.None)
                    return target;
            }

            return CardDragTarget.None;
        }

        private static CardDragTarget ParseHit(
            GameObject hit,
            CardDragMode mode,
            MatchManager matchManager,
            Card sourceCard
        )
        {
            foreach (var zone in hit.GetComponents<CardDragZone>())
            {
                if (mode == CardDragMode.PlayOrder && zone.ZoneKind == CardDragTargetKind.OrderZone)
                    return new CardDragTarget { Kind = CardDragTargetKind.OrderZone };

                if (
                    mode == CardDragMode.PlayCountermeasure
                    && zone.ZoneKind == CardDragTargetKind.CountermeasureZone
                )
                    return new CardDragTarget { Kind = CardDragTargetKind.CountermeasureZone };
            }

            if (mode == CardDragMode.BattlefieldAction)
            {
                var hq = hit.GetComponent<HeadquarterView>();
                if (hq != null && hq.IsOpponentHq && matchManager.CanAttackHQ(sourceCard, matchManager.Opponent))
                    return new CardDragTarget { Kind = CardDragTargetKind.OpponentHeadquarters };

                var opponentSlot = hit.GetComponent<OpponentCardSlot>();
                if (opponentSlot != null)
                {
                    var defender = matchManager.Opponent.Battlefield.GetCardAt(opponentSlot.SlotIndex);
                    if (
                        defender != null
                        && matchManager.CanAttack(sourceCard, defender)
                    )
                    {
                        return new CardDragTarget
                        {
                            Kind = CardDragTargetKind.OpponentBattlefieldSlot,
                            SlotIndex = opponentSlot.SlotIndex,
                            DefenderCard = defender,
                        };
                    }
                }

                var playerSlot = hit.GetComponent<PlayerCardSlot>();
                if (
                    playerSlot != null
                    && matchManager.CanMoveUnit(sourceCard, playerSlot.SlotIndex)
                )
                {
                    return new CardDragTarget
                    {
                        Kind = CardDragTargetKind.PlayerBattlefieldSlot,
                        SlotIndex = playerSlot.SlotIndex,
                    };
                }
            }

            if (mode == CardDragMode.DeployUnit)
            {
                var playerSlot = hit.GetComponent<PlayerCardSlot>();
                if (
                    playerSlot != null
                    && matchManager.CanDeployUnitCard(sourceCard)
                    && matchManager.Player.Battlefield.IsSlotEmpty(playerSlot.SlotIndex)
                )
                {
                    return new CardDragTarget
                    {
                        Kind = CardDragTargetKind.PlayerBattlefieldSlot,
                        SlotIndex = playerSlot.SlotIndex,
                    };
                }
            }

            return CardDragTarget.None;
        }
    }
}
