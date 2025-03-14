using System;
using System.Collections.Generic;
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
                // Add to our internal list
                if (!cards.Contains(card))
                {
                    cards.Add(card);
                }

                card.SetOwner(owner);

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
                    bool removed = cards.Remove(card);
                    return removed;
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
                cards.Remove(card);
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

        /// <summary>
        /// Gets the position (slot index) of a card in the battlefield.
        /// </summary>
        /// <param name="card">The card to find</param>
        /// <returns>The slot index where the card is located, or -1 if not found</returns>
        public int GetCardPosition(Card card)
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

            return -1;
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
