using System;
using System.Collections.Generic;

namespace Kardx.Core
{
    /// <summary>
    /// Represents a battlefield with fixed slots for cards.
    /// </summary>
    public class Battlefield : CardCollection
    {
        public const int SLOT_COUNT = 5;
        private BattlefieldSlot[] slots;
        
        public event Action<Card, int> OnCardDeployed;
        public new event Action<Card, CardCollection> OnCardAdded;
        public new event Action<Card, CardCollection> OnCardRemoved;
        
        public Battlefield(Player owner) : base(owner)
        {
            slots = new BattlefieldSlot[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                slots[i] = new BattlefieldSlot(i);
            }
        }
        
        public IReadOnlyList<BattlefieldSlot> Slots => Array.AsReadOnly(slots);
        
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
                
                OnCardDeployed?.Invoke(card, slotIndex);
                OnCardAdded?.Invoke(card, this);
                
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
                    if (removed)
                    {
                        OnCardRemoved?.Invoke(card, this);
                    }
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
                OnCardRemoved?.Invoke(card, this);
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
