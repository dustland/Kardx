using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kardx.Core
{
    /// <summary>
    /// Base class for all card collections in the game.
    /// </summary>
    public abstract class CardCollection
    {
        protected List<Card> cards = new List<Card>();
        protected Player owner;
        
        // Events
        public event Action<Card, CardCollection> OnCardAdded;
        public event Action<Card, CardCollection> OnCardRemoved;
        
        public CardCollection(Player owner)
        {
            this.owner = owner;
        }
        
        public IReadOnlyList<Card> Cards => cards.AsReadOnly();
        
        public Player Owner => owner;
        
        public virtual void AddCard(Card card)
        {
            if (card == null) return;
            
            cards.Add(card);
            card.SetOwner(owner);
            OnCardAdded?.Invoke(card, this);
        }
        
        public virtual bool RemoveCard(Card card)
        {
            if (card == null) return false;
            
            bool removed = cards.Remove(card);
            if (removed)
            {
                OnCardRemoved?.Invoke(card, this);
            }
            return removed;
        }
        
        public virtual bool TransferCardTo(Card card, CardCollection destination)
        {
            if (!Contains(card) || destination == null)
                return false;
                
            if (RemoveCard(card))
            {
                destination.AddCard(card);
                return true;
            }
            
            return false;
        }
        
        public virtual void Clear()
        {
            var oldCards = new List<Card>(cards);
            cards.Clear();
            
            foreach (var card in oldCards)
            {
                OnCardRemoved?.Invoke(card, this);
            }
        }
        
        public virtual bool Contains(Card card)
        {
            return cards.Contains(card);
        }
        
        public virtual int Count => cards.Count;
    }
}
