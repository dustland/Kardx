using System;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Models.Cards;  // Import for Card class
using Kardx.Views.Cards;   // Import for CardView class

namespace Kardx.Managers
{
    /// <summary>
    /// Central registry that maps model objects to their UI representations
    /// to maintain synchronization between data and UI.
    /// </summary>
    public class ViewRegistry
    {
        // Maps card instance IDs to their UI CardView components
        private Dictionary<string, CardView> cardViewRegistry = new Dictionary<string, CardView>();
        
        // Event fired when a card view is registered
        public event Action<Card, CardView> OnCardViewRegistered;
        
        // Event fired when a card view is unregistered (removed)
        public event Action<Card> OnCardViewUnregistered;

        /// <summary>
        /// Gets all card-view pairs currently registered
        /// </summary>
        public IReadOnlyDictionary<Card, CardView> CardViews
        {
            get
            {
                Dictionary<Card, CardView> result = new Dictionary<Card, CardView>();
                foreach (var pair in cardViewRegistry)
                {
                    if (pair.Value != null && pair.Value.Card != null)
                    {
                        result[pair.Value.Card] = pair.Value;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Register a card view in the registry
        /// </summary>
        public void RegisterCard(Card card, CardView view)
        {
            if (card == null || view == null) return;
            
            // Remove any existing view for this card
            if (cardViewRegistry.TryGetValue(card.InstanceId, out var existingView) && existingView != view)
            {
                Debug.LogWarning($"[ViewRegistry] Replacing existing view for card {card.Title} ({card.InstanceId})");
                UnregisterView(card);
            }
            
            cardViewRegistry[card.InstanceId] = view;
            OnCardViewRegistered?.Invoke(card, view);
            
            Debug.Log($"[ViewRegistry] Registered view for card {card.Title} ({card.InstanceId})");
        }
        
        private void UnregisterView(Card card)
        {
            if (card == null) return;
            
            if (cardViewRegistry.ContainsKey(card.InstanceId))
            {
                cardViewRegistry.Remove(card.InstanceId);
                OnCardViewUnregistered?.Invoke(card);
                
                Debug.Log($"[ViewRegistry] Unregistered view for card {card.Title} ({card.InstanceId})");
            }
        }
        
        /// <summary>
        /// Unregisters a Card from the registry
        /// </summary>
        /// <param name="card">The card to unregister</param>
        /// <returns>True if the card was unregistered, false if not found</returns>
        public bool UnregisterCard(Card card)
        {
            if (card == null) return false;
            
            if (cardViewRegistry.ContainsKey(card.InstanceId))
            {
                cardViewRegistry.Remove(card.InstanceId);
                OnCardViewUnregistered?.Invoke(card);
                
                Debug.Log($"[ViewRegistry] Unregistered view for card {card.Title} ({card.InstanceId})");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the view associated with a card model
        /// </summary>
        public CardView GetCardView(Card card)
        {
            if (card == null) return null;
            
            if (cardViewRegistry.TryGetValue(card.InstanceId, out var view))
            {
                return view;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get a card view by its instance ID
        /// </summary>
        public CardView GetCardViewById(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            
            if (cardViewRegistry.TryGetValue(cardId, out var view))
            {
                return view;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all card IDs in the registry
        /// </summary>
        public IEnumerable<string> GetAllCardIds()
        {
            return cardViewRegistry.Keys;
        }
        
        /// <summary>
        /// Check if a card has a registered view
        /// </summary>
        public bool HasCardView(Card card)
        {
            if (card == null) return false;
            return cardViewRegistry.ContainsKey(card.InstanceId);
        }
        
        /// <summary>
        /// Clear all registered views
        /// </summary>
        public void Clear()
        {
            cardViewRegistry.Clear();
            Debug.Log("[ViewRegistry] Cleared all registered views");
        }
        
        /// <summary>
        /// Get all registered card views
        /// </summary>
        public List<CardView> GetAllCardViews()
        {
            return new List<CardView>(cardViewRegistry.Values);
        }
        
        /// <summary>
        /// Get all registered cards
        /// </summary>
        public List<Card> GetAllCards()
        {
            List<Card> cards = new List<Card>();
            foreach (var kvp in cardViewRegistry)
            {
                var cardView = kvp.Value;
                if (cardView != null && cardView.Card != null)
                {
                    cards.Add(cardView.Card);
                }
            }
            return cards;
        }
    }
}
