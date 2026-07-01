namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Exactly one drag mode is active per drag gesture.
    /// </summary>
    public enum CardDragMode
    {
        None,
        DeployUnit,
        PlayOrder,
        PlayCountermeasure,
        BattlefieldAction,
    }

    public enum CardDragTargetKind
    {
        None,
        PlayerBattlefieldSlot,
        OpponentBattlefieldSlot,
        OpponentHeadquarters,
        OrderZone,
        CountermeasureZone,
    }

    public struct CardDragTarget
    {
        public CardDragTargetKind Kind;
        public int SlotIndex;
        public Kardx.Models.Cards.Card DefenderCard;

        public static CardDragTarget None => new() { Kind = CardDragTargetKind.None };
    }
}
