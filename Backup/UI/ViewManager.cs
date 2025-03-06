using System;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Core;

namespace Kardx.UI
{
    /// <summary>
    /// Manages the synchronization between model objects and their UI representations.
    /// Acts as a bridge between the Core and UI layers.
    /// </summary>
    public class ViewManager : MonoBehaviour
    {
        [SerializeField] private CardView playerCardPrefab;
        [SerializeField] private CardView opponentCardPrefab;
        
        private MatchManager matchManager;
        private ViewRegistry viewRegistry = new ViewRegistry();
        
        // Components that manage different areas of the UI
        private HandView playerHandView;
        private HandView opponentHandView;
        private PlayerBattlefieldView playerBattlefieldView;
        private OpponentBattlefieldView opponentBattlefieldView;
        
        // Events
        public event Action<Card, CardView> OnCardViewCreated;
        public event Action<Card> OnCardViewDestroyed;
        public event Action<Card, GameArea, GameArea> OnCardAreaChanged;
        
        /// <summary>
        /// Initialize with the match manager
        /// </summary>
        public void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;
            
            // Store references to UI views
            playerHandView = FindAnyObjectByType<HandView>();
            opponentHandView = FindObjectsByType<HandView>(FindObjectsSortMode.None)
                .FirstOrDefault(h => h != playerHandView);
            playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
            opponentBattlefieldView = FindAnyObjectByType<OpponentBattlefieldView>();
            
            // Subscribe to relevant events
            if (matchManager != null)
            {
                matchManager.OnCardDeployed += HandleCardDeployed;
                matchManager.OnCardDied += HandleCardDied;
                matchManager.OnCardDrawn += HandleCardDrawn;
                matchManager.OnCardDiscarded += HandleCardDiscarded;
                matchManager.OnCardReturned += HandleCardReturned;
            }
            
            // Subscribe to registry events
            viewRegistry.OnCardViewRegistered += (card, view) => OnCardViewCreated?.Invoke(card, view);
            viewRegistry.OnCardViewUnregistered += (card) => OnCardViewDestroyed?.Invoke(card);
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (matchManager != null)
            {
                matchManager.OnCardDeployed -= HandleCardDeployed;
                matchManager.OnCardDied -= HandleCardDied;
                matchManager.OnCardDrawn -= HandleCardDrawn;
                matchManager.OnCardDiscarded -= HandleCardDiscarded;
                matchManager.OnCardReturned -= HandleCardReturned;
            }
            
            // Clear registry
            viewRegistry.Clear();
        }
        
        /// <summary>
        /// Get the view registry
        /// </summary>
        public ViewRegistry Registry => viewRegistry;
        
        /// <summary>
        /// Handle card drawn event
        /// </summary>
        private void HandleCardDrawn(Player player, Card card)
        {
            // Card moved from deck to hand
            EnsureCardInCorrectArea(card, GameArea.Hand);
        }
        
        /// <summary>
        /// Handle card deployed event
        /// </summary>
        private void HandleCardDeployed(Card card, int slotIndex)
        {
            // Card moved from hand to battlefield
            EnsureCardInCorrectArea(card, GameArea.Battlefield, slotIndex);
        }
        
        /// <summary>
        /// Handle card died event
        /// </summary>
        private void HandleCardDied(Card card)
        {
            // Card moved from battlefield to graveyard (destroyed)
            DestroyCardView(card);
        }
        
        /// <summary>
        /// Handle card discarded event
        /// </summary>
        private void HandleCardDiscarded(Card card)
        {
            // Card moved from hand to graveyard
            DestroyCardView(card);
        }
        
        /// <summary>
        /// Handle card returned event (from battlefield to hand)
        /// </summary>
        private void HandleCardReturned(Card card)
        {
            // Card moved from battlefield to hand
            EnsureCardInCorrectArea(card, GameArea.Hand);
        }
        
        /// <summary>
        /// Ensures a card UI is in the correct area of the game
        /// This maintains consistency between the data model tree and UI tree
        /// </summary>
        public void EnsureCardInCorrectArea(Card card, GameArea targetArea)
        {
            if (card == null) return;
            
            // Determine the current area of the card in the UI
            GameArea currentArea = DetermineCardUIArea(card);
            
            // If card is already in the correct area, no need to move it
            if (currentArea == targetArea) return;
            
            // If card has changed areas, we need to update the UI
            bool isPlayerCard = card.Owner == matchManager.Player;
            
            // Fire event that card is changing areas
            OnCardAreaChanged?.Invoke(card, currentArea, targetArea);
            
            // Get the existing card view if it exists
            CardView existingView = viewRegistry.GetCardView(card);
            
            // Handle UI tree updates based on the target area
            switch (targetArea)
            {
                case GameArea.Hand:
                    // Card should be in hand
                    if (existingView != null)
                    {
                        // Reparent the existing view instead of destroying and recreating
                        Transform handTransform = isPlayerCard ? 
                            playerHandView.GetHandTransform() : 
                            opponentHandView.GetHandTransform();
                        
                        if (handTransform != null)
                        {
                            // Update the card's parent and position in the hand
                            existingView.transform.SetParent(handTransform);
                            
                            // Reset transform values
                            existingView.transform.localPosition = Vector3.zero;
                            existingView.transform.localRotation = Quaternion.identity;
                            existingView.transform.localScale = Vector3.one;
                            
                            // Configure card for hand interaction
                            if (isPlayerCard)
                            {
                                existingView.SwitchToDeploymentDragHandler();
                            }
                            else
                            {
                                existingView.SetDraggable(false);
                            }
                            
                            Debug.Log($"[ViewManager] Moved card {card.Title} to {targetArea}");
                        }
                    }
                    else
                    {
                        // No existing view, create a new one in the hand
                        if (isPlayerCard && playerHandView != null)
                        {
                            playerHandView.AddCardToHand(card);
                        }
                        else if (!isPlayerCard && opponentHandView != null)
                        {
                            opponentHandView.AddCardToHand(card);
                        }
                    }
                    break;
                    
                case GameArea.Battlefield:
                    // Get the target position on the battlefield
                    int slotIndex = -1;
                    if (isPlayerCard && matchManager.Player.Battlefield.Contains(card))
                    {
                        slotIndex = matchManager.Player.Battlefield.GetCardPosition(card);
                    }
                    else if (!isPlayerCard && matchManager.Opponent.Battlefield.Contains(card))
                    {
                        slotIndex = matchManager.Opponent.Battlefield.GetCardPosition(card);
                    }
                    
                    if (slotIndex >= 0)
                    {
                        // Get the target slot's transform
                        Transform slotTransform = null;
                        if (isPlayerCard && playerBattlefieldView != null)
                        {
                            var slots = playerBattlefieldView.GetSlots();
                            if (slotIndex < slots.Length)
                            {
                                slotTransform = slots[slotIndex].CardContainer;
                            }
                        }
                        else if (!isPlayerCard && opponentBattlefieldView != null)
                        {
                            var slots = opponentBattlefieldView.GetSlots();
                            if (slotIndex < slots.Length)
                            {
                                slotTransform = slots[slotIndex].CardContainer;
                            }
                        }
                        
                        if (existingView != null && slotTransform != null)
                        {
                            // Reparent the existing view
                            existingView.transform.SetParent(slotTransform);
                            
                            // Reset transform values
                            existingView.transform.localPosition = Vector3.zero;
                            existingView.transform.localRotation = Quaternion.identity;
                            existingView.transform.localScale = Vector3.one;
                            
                            // Configure card for battlefield interaction
                            existingView.SetDraggable(false);
                            
                            Debug.Log($"[ViewManager] Moved card {card.Title} to {targetArea} at slot {slotIndex}");
                        }
                        else
                        {
                            // No existing view or no valid slot, create a new card in the battlefield
                            if (isPlayerCard && playerBattlefieldView != null)
                            {
                                playerBattlefieldView.CreateCardUI(card, slotIndex);
                            }
                            else if (!isPlayerCard && opponentBattlefieldView != null)
                            {
                                opponentBattlefieldView.CreateCardUI(card, slotIndex);
                            }
                        }
                    }
                    break;
                    
                case GameArea.Graveyard:
                    // Cards in graveyard don't have UI representation
                    DestroyCardView(card);
                    break;
            }
        }
        
        /// <summary>
        /// Determines which area of the game a card UI is currently in
        /// </summary>
        private GameArea DetermineCardUIArea(Card card)
        {
            // Check if the card has a view
            CardView view = viewRegistry.GetCardView(card);
            if (view == null) return GameArea.Unknown;
            
            // Check which area the card is in based on parent transforms
            Transform viewTransform = view.transform;
            Transform viewParent = viewTransform.parent;
            
            // Check if it's in player hand
            if (playerHandView != null && 
                viewParent != null && 
                viewParent == playerHandView.GetHandTransform())
            {
                return GameArea.PlayerHand;
            }
            
            // Check if it's in opponent hand
            if (opponentHandView != null && 
                viewParent != null && 
                viewParent == opponentHandView.GetHandTransform())
            {
                return GameArea.OpponentHand;
            }
            
            // Check if it's on player battlefield
            if (playerBattlefieldView != null)
            {
                foreach (var slot in playerBattlefieldView.GetSlots())
                {
                    if (viewParent != null && viewParent == slot.CardContainer)
                    {
                        return GameArea.PlayerBattlefield;
                    }
                }
            }
            
            // Check if it's on opponent battlefield
            if (opponentBattlefieldView != null)
            {
                foreach (var slot in opponentBattlefieldView.GetSlots())
                {
                    if (viewParent != null && viewParent == slot.CardContainer)
                    {
                        return GameArea.OpponentBattlefield;
                    }
                }
            }
            
            // If view exists but not in a known container, it might be detached 
            // (e.g., during a drag operation)
            if (viewParent == view.transform.root)
            {
                return GameArea.Detached;
            }
            
            return GameArea.Unknown;
        }
        
        /// <summary>
        /// Creates a card view for a card model
        /// </summary>
        public CardView CreateCardView(Card card, Transform parent, bool isPlayerCard = true)
        {
            if (card == null)
            {
                Debug.LogError("[ViewManager] Cannot create card view: Card is null");
                return null;
            }
            
            // Check if a view already exists for this card
            CardView existingView = viewRegistry.GetCardView(card);
            if (existingView != null)
            {
                Debug.LogWarning($"[ViewManager] View already exists for card {card.Title} ({card.InstanceId})");
                return existingView;
            }
            
            // Determine which prefab to use
            CardView prefab = isPlayerCard ? playerCardPrefab : opponentCardPrefab;
            if (prefab == null)
            {
                Debug.LogError("[ViewManager] Card prefab is null");
                return null;
            }
            
            // Instantiate the card view
            CardView view = Instantiate(prefab, parent);
            view.Initialize(card);
            
            // Register in the registry
            viewRegistry.RegisterCardView(card, view);
            
            return view;
        }
        
        /// <summary>
        /// Destroys a card view
        /// </summary>
        public void DestroyCardView(Card card)
        {
            CardView view = viewRegistry.GetCardView(card);
            if (view != null)
            {
                // Unregister first
                viewRegistry.UnregisterCardView(card);
                
                // Then destroy the GameObject
                Destroy(view.gameObject);
                
                Debug.Log($"[ViewManager] Destroyed view for card {card.Title}");
            }
        }
        
        /// <summary>
        /// Creates card views for cards in a list
        /// </summary>
        public List<CardView> CreateCardViews(IEnumerable<Card> cards, Transform parent, bool isPlayerCards = true)
        {
            List<CardView> views = new List<CardView>();
            foreach (var card in cards)
            {
                CardView view = CreateCardView(card, parent, isPlayerCards);
                if (view != null)
                {
                    views.Add(view);
                }
            }
            return views;
        }
        
        /// <summary>
        /// Synchronizes all card views to ensure the UI hierarchy matches the data model
        /// This is the primary method for ensuring model-view consistency
        /// </summary>
        public void SynchronizeAllCardViews()
        {
            if (matchManager == null)
            {
                Debug.LogError("[ViewManager] Cannot synchronize - MatchManager is null");
                return;
            }

            // Track which cards we've already processed to avoid duplicates
            HashSet<Card> processedCards = new HashSet<Card>();
            
            // Process player cards
            SynchronizePlayerHand(processedCards);
            SynchronizePlayerBattlefield(processedCards);
            
            // Process opponent cards
            SynchronizeOpponentHand(processedCards);
            SynchronizeOpponentBattlefield(processedCards);
            
            // Clean up orphaned views (views for cards that no longer exist in any valid game area)
            CleanupOrphanedViews(processedCards);
            
            Debug.Log("[ViewManager] Card view synchronization complete");
        }
        
        private void SynchronizePlayerHand(HashSet<Card> processedCards)
        {
            if (matchManager.Player == null || matchManager.Player.Hand == null) return;
            
            // Process player hand
            foreach (Card card in matchManager.Player.Hand.GetCards())
            {
                if (!processedCards.Contains(card))
                {
                    EnsureCardInCorrectArea(card, GameArea.PlayerHand);
                    processedCards.Add(card);
                }
            }
        }
        
        private void SynchronizePlayerBattlefield(HashSet<Card> processedCards)
        {
            if (matchManager.Player == null || matchManager.Player.Battlefield == null) return;
            
            // Process player battlefield
            var battlefield = matchManager.Player.Battlefield;
            for (int i = 0; i < battlefield.Size; i++)
            {
                Card card = battlefield.GetCardAt(i);
                if (card != null && !processedCards.Contains(card))
                {
                    EnsureCardInCorrectArea(card, GameArea.PlayerBattlefield, i);
                    processedCards.Add(card);
                }
            }
        }
        
        private void SynchronizeOpponentHand(HashSet<Card> processedCards)
        {
            if (matchManager.Opponent == null || matchManager.Opponent.Hand == null) return;
            
            // Process opponent hand
            foreach (Card card in matchManager.Opponent.Hand.GetCards())
            {
                if (!processedCards.Contains(card))
                {
                    EnsureCardInCorrectArea(card, GameArea.OpponentHand);
                    processedCards.Add(card);
                }
            }
        }
        
        private void SynchronizeOpponentBattlefield(HashSet<Card> processedCards)
        {
            if (matchManager.Opponent == null || matchManager.Opponent.Battlefield == null) return;
            
            // Process opponent battlefield
            var battlefield = matchManager.Opponent.Battlefield;
            for (int i = 0; i < battlefield.Size; i++)
            {
                Card card = battlefield.GetCardAt(i);
                if (card != null && !processedCards.Contains(card))
                {
                    EnsureCardInCorrectArea(card, GameArea.OpponentBattlefield, i);
                    processedCards.Add(card);
                }
            }
        }
        
        private void CleanupOrphanedViews(HashSet<Card> validCards)
        {
            // Get all card views currently registered
            List<Card> cardsToRemove = new List<Card>();
            
            foreach (KeyValuePair<Card, CardView> entry in viewRegistry.CardViews)
            {
                // If the card isn't in our processed set, it's not in any valid game area
                if (!validCards.Contains(entry.Key))
                {
                    // Mark for removal from registry
                    cardsToRemove.Add(entry.Key);
                    
                    // Destroy the orphaned view if it still exists
                    if (entry.Value != null)
                    {
                        Debug.Log($"[ViewManager] Destroying orphaned view for card {entry.Key.Title}");
                        GameObject.Destroy(entry.Value.gameObject);
                    }
                }
            }
            
            // Remove all orphaned cards from the registry
            foreach (Card card in cardsToRemove)
            {
                viewRegistry.UnregisterCard(card);
            }
        }

        /// <summary>
        /// Ensure a card is visually represented in the correct area based on the data model
        /// This will reparent existing views instead of destroying and recreating them
        /// </summary>
        /// <param name="card">The card to ensure is in the correct location</param>
        /// <param name="area">The area where the card should be located</param>
        /// <param name="slot">The slot within the area (for battlefields)</param>
        /// <returns>The CardView for the card, either existing or newly created</returns>
        public CardView EnsureCardInCorrectArea(Card card, GameArea area, int slot = -1)
        {
            if (card == null)
            {
                Debug.LogError("[ViewManager] Cannot ensure position of null card");
                return null;
            }
            
            // Check if a view for this card already exists
            CardView cardView = viewRegistry.GetCardView(card);
            
            switch (area)
            {
                case GameArea.PlayerHand:
                    if (playerHandView == null) 
                    {
                        Debug.LogError("[ViewManager] Player hand view is null");
                        return null;
                    }
                    
                    if (cardView != null)
                    {
                        // Card already has a view - check if it's already in the player's hand
                        if (cardView.transform.parent != playerHandView.GetHandTransform())
                        {
                            // Reparent existing view rather than destroying and recreating
                            playerHandView.AddCardToHand(card);
                        }
                    }
                    else
                    {
                        // No existing view, create a new one
                        playerHandView.AddCardToHand(card);
                        // Get the newly created view from the registry
                        cardView = viewRegistry.GetCardView(card);
                    }
                    break;
                    
                case GameArea.OpponentHand:
                    if (opponentHandView == null)
                    {
                        Debug.LogError("[ViewManager] Opponent hand view is null");
                        return null;
                    }
                    
                    if (cardView != null)
                    {
                        // Card already has a view - check if it's already in the opponent's hand
                        if (cardView.transform.parent != opponentHandView.GetHandTransform())
                        {
                            // Reparent existing view rather than destroying and recreating
                            opponentHandView.AddCardToHand(card);
                        }
                    }
                    else
                    {
                        // No existing view, create a new one
                        opponentHandView.AddCardToHand(card);
                        // Get the newly created view from the registry
                        cardView = viewRegistry.GetCardView(card);
                    }
                    break;
                    
                case GameArea.PlayerBattlefield:
                    if (playerBattlefieldView == null)
                    {
                        Debug.LogError("[ViewManager] Player battlefield view is null");
                        return null;
                    }
                    
                    if (slot < 0)
                    {
                        Debug.LogError("[ViewManager] Invalid battlefield slot index");
                        return null;
                    }
                    
                    if (cardView != null)
                    {
                        // Card already has a view, check if it's in the correct slot
                        // For battlefield cards, we compare the parent's index to determine slot
                        var currentSlot = GetCardSlotIndex(cardView);
                        if (currentSlot != slot || !IsInPlayerBattlefield(cardView))
                        {
                            // Card is not in the correct slot or not in the player battlefield
                            // Create a new view in the correct slot
                            cardView = playerBattlefieldView.CreateCardUI(card, slot);
                        }
                    }
                    else
                    {
                        // No existing view, create a new one
                        cardView = playerBattlefieldView.CreateCardUI(card, slot);
                    }
                    break;
                    
                case GameArea.OpponentBattlefield:
                    if (opponentBattlefieldView == null)
                    {
                        Debug.LogError("[ViewManager] Opponent battlefield view is null");
                        return null;
                    }
                    
                    if (slot < 0)
                    {
                        Debug.LogError("[ViewManager] Invalid battlefield slot index");
                        return null;
                    }
                    
                    if (cardView != null)
                    {
                        // Card already has a view, check if it's in the correct slot
                        var currentSlot = GetCardSlotIndex(cardView);
                        if (currentSlot != slot || !IsInOpponentBattlefield(cardView))
                        {
                            // Card is not in the correct slot or not in the opponent battlefield
                            // Create a new view in the correct slot
                            cardView = opponentBattlefieldView.CreateCardUI(card, slot);
                        }
                    }
                    else
                    {
                        // No existing view, create a new one
                        cardView = opponentBattlefieldView.CreateCardUI(card, slot);
                    }
                    break;
                    
                default:
                    Debug.LogError($"[ViewManager] Unknown card area: {area}");
                    return null;
            }
            
            return cardView;
        }
        
        /// <summary>
        /// Get the index of the card slot containing a card view
        /// </summary>
        private int GetCardSlotIndex(CardView cardView)
        {
            if (cardView == null || cardView.transform == null || cardView.transform.parent == null)
                return -1;
            
            // Get the parent's sibling index
            return cardView.transform.parent.GetSiblingIndex();
        }
        
        /// <summary>
        /// Check if a card view is currently in the player's battlefield
        /// </summary>
        private bool IsInPlayerBattlefield(CardView cardView)
        {
            if (cardView == null || playerBattlefieldView == null)
                return false;
            
            // Check parent hierarchy - is this card in a PlayerCardSlot?
            Transform parent = cardView.transform.parent;
            while (parent != null)
            {
                if (parent == playerBattlefieldView.transform)
                    return true;
                
                parent = parent.parent;
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a card view is currently in the opponent's battlefield
        /// </summary>
        private bool IsInOpponentBattlefield(CardView cardView)
        {
            if (cardView == null || opponentBattlefieldView == null)
                return false;
            
            // Check parent hierarchy - is this card in an OpponentCardSlot?
            Transform parent = cardView.transform.parent;
            while (parent != null)
            {
                if (parent == opponentBattlefieldView.transform)
                    return true;
                
                parent = parent.parent;
            }
            
            return false;
        }
        
        /// <summary>
        /// Validates that the UI matches the model state
        /// </summary>
        public List<string> ValidateViewState()
        {
            List<string> inconsistencies = new List<string>();
            
            // Get all cards in the model
            var allModelCards = new List<Card>();
            if (matchManager != null)
            {
                if (matchManager.Player != null)
                {
                    allModelCards.AddRange(matchManager.Player.GetCardsInPlay());
                    allModelCards.AddRange(matchManager.Player.Hand.Cards);
                }
                
                if (matchManager.Opponent != null)
                {
                    allModelCards.AddRange(matchManager.Opponent.GetCardsInPlay());
                    allModelCards.AddRange(matchManager.Opponent.Hand.Cards);
                }
            }
            
            // Check for inconsistencies in UI placement
            foreach (var card in allModelCards)
            {
                // Determine where the card should be
                GameArea expectedArea = GameArea.Unknown;
                if (card.Owner == matchManager.Player && matchManager.Player.Hand.Contains(card))
                {
                    expectedArea = GameArea.PlayerHand;
                }
                else if (card.Owner == matchManager.Opponent && matchManager.Opponent.Hand.Contains(card))
                {
                    expectedArea = GameArea.OpponentHand;
                }
                else if (card.Owner == matchManager.Player && matchManager.Player.Battlefield.Contains(card))
                {
                    expectedArea = GameArea.PlayerBattlefield;
                }
                else if (card.Owner == matchManager.Opponent && matchManager.Opponent.Battlefield.Contains(card))
                {
                    expectedArea = GameArea.OpponentBattlefield;
                }
                
                // Check if the UI matches this expectation
                GameArea actualArea = DetermineCardUIArea(card);
                if (actualArea != expectedArea && expectedArea != GameArea.Unknown)
                {
                    string owner = card.Owner == matchManager.Player ? "Player" : "Opponent";
                    inconsistencies.Add($"{owner} card '{card.Title}' (ID: {card.InstanceId}) should be in {expectedArea} but is in {actualArea}");
                }
            }
            
            // Check if all views have valid models
            foreach (var cardId in viewRegistry.GetAllCardIds())
            {
                var view = viewRegistry.GetCardViewById(cardId);
                if (view != null && view.Card != null)
                {
                    if (!allModelCards.Contains(view.Card))
                    {
                        inconsistencies.Add($"Card '{view.Card.Title}' (ID: {cardId}) exists in UI but not in model");
                    }
                }
            }
            
            return inconsistencies;
        }
    }
    
    /// <summary>
    /// Represents different areas of the game where a card can be
    /// </summary>
    public enum GameArea
    {
        Unknown,
        PlayerHand,
        OpponentHand,
        PlayerBattlefield,
        OpponentBattlefield,
        Graveyard,
        Detached // For cards being dragged
    }
}
