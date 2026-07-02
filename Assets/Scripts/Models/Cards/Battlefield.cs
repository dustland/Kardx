using System;
using System.Collections.Generic;
using Kardx.Models;
using Kardx.Models.Match;

namespace Kardx.Models.Cards
{
    /// <summary>
    /// Represents a battlefield with fixed slots for cards.
    /// </summary>
    public class Battlefield : CardCollection
    {
        public const int SLOT_COUNT = 5;
        private BattlefieldSlot[] slots;

        protected override ZoneType Zone => ZoneType.Battlefield;

        public Battlefield(Player owner) : base(owner)
        {
            slots = new BattlefieldSlot[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                slots[i] = new BattlefieldSlot(i);
            }
        }

        public IReadOnlyList<BattlefieldSlot> Slots => Array.AsReadOnly(slots);
        public int SlotCount => slots.Length;

        public Card GetCardAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            return slots[slotIndex].Card;
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            return !slots[slotIndex].IsOccupied;
        }

        public bool DeployCard(Card card, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            if (!IsSlotEmpty(slotIndex))
            {
                return false;
            }

            // Set the card in the slot
            if (slots[slotIndex].SetCard(card))
            {
                // CardCollection.AddCard handles: ownership/zone + event signaling.
                // (Avoid invoking OnCardAdded directly here; events can only be raised
                // within CardCollection.)
                if (!cards.Contains(card))
                    AddCard(card);

                return true;
            }

            return false;
        }

        public override bool RemoveCard(Card card)
        {
            if (card == null) return false;

            // Find the slot containing this card
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                if (slots[i].Card == card)
                {
                    slots[i].RemoveCard();
                    // CardCollection.RemoveCard handles: removal + zone update + event signaling.
                    return base.RemoveCard(card);
                }
            }

            return false;
        }

        public Card RemoveCardAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            var card = slots[slotIndex].RemoveCard();
            if (card != null)
            {
                base.RemoveCard(card);
            }

            return card;
        }

        public int GetSlotIndex(Card card)
        {
            if (card == null) return -1;

            // Find the slot containing this card
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                if (slots[i].Card == card)
                {
                    return i;
                }
            }

            return -1; // Card not found
        }

        public bool MoveCard(Card card, int toSlotIndex)
        {
            if (card == null || toSlotIndex < 0 || toSlotIndex >= SLOT_COUNT)
                return false;

            if (!IsSlotEmpty(toSlotIndex))
                return false;

            int fromSlot = GetSlotIndex(card);
            if (fromSlot < 0)
                return false;

            slots[fromSlot].RemoveCard();
            if (!slots[toSlotIndex].SetCard(card))
            {
                slots[fromSlot].SetCard(card);
                return false;
            }

            return true;
        }
    }

    public class BattlefieldSlot
    {
        public int Position { get; private set; }
        public Card Card { get; private set; }
        public bool IsOccupied => Card != null;

        public BattlefieldSlot(int position)
        {
            Position = position;
        }

        public bool SetCard(Card card)
        {
            if (IsOccupied) return false;
            Card = card;
            return true;
        }

        public Card RemoveCard()
        {
            var card = Card;
            Card = null;
            return card;
        }
    }
}
