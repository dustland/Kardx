using System.Collections.Generic;
using UnityEngine;
using Kardx.Models.Cards;
using Kardx.Views.Cards;
using Kardx.Models.Match;

namespace Kardx.Views.Hand
{
    /// <summary>
    /// Manages the display of cards in the player's hand
    /// </summary>
    public class PlayerHandView : BaseHandView
    {
        /// <summary>
        /// Handle card drawn event - only add cards owned by the player
        /// </summary>
        protected override void HandleCardDrawn(Card card)
        {
            if (card == null)
            {
                Debug.LogWarning("[PlayerHandView] Cannot handle null card");
                return;
            }

            // Only add cards owned by the player
            if (card.Owner == matchManager.Player)
            {
                AddCardToHand(card);
            }
        }

        /// <summary>
        /// Adds a card to the player's hand
        /// </summary>
        public override void AddCardToHand(Card card)
        {
            if (card == null)
            {
                Debug.LogWarning("[PlayerHandView] Cannot add null card to hand");
                return;
            }

            // Only add player cards to the player hand
            if (card.Owner != matchManager.Player)
            {
                Debug.LogWarning($"[PlayerHandView] Attempted to add opponent card {card.Title} to player hand");
                return;
            }

            // Check if this card already exists in the hand
            if (IsCardInHand(card))
            {
                Debug.Log($"[PlayerHandView] Card {card.Title} is already in hand");
                return;
            }

            // Create UI for the card - player cards are face up
            CardView cardView = CreateCardUI(card, false);

            if (cardView != null)
            {
                cardView.UpdateInteractivity();

                Debug.Log($"[PlayerHandView] Added card {card.Title} to player hand");

                // Re-arrange the cards in the hand
                ArrangeCards();
            }
        }
    }
}
