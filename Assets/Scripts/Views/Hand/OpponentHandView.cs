using System.Collections.Generic;
using UnityEngine;
using Kardx.Models.Cards;
using Kardx.Views.Cards;
using Kardx.Models.Match;

namespace Kardx.Views.Hand
{
    /// <summary>
    /// Manages the display of cards in the opponent's hand
    /// </summary>
    public class OpponentHandView : BaseHandView
    {
        /// <summary>
        /// Handle card drawn event - only add cards owned by the opponent
        /// </summary>
        protected override void HandleCardDrawn(Card card)
        {
            if (card == null)
            {
                Debug.LogWarning("[OpponentHandView] Cannot handle null card");
                return;
            }

            // Only add cards owned by the opponent
            if (card.Owner != matchManager.Player)
            {
                AddCardToHand(card);
            }
        }

        /// <summary>
        /// Adds a card to the opponent's hand
        /// </summary>
        public override void AddCardToHand(Card card)
        {
            if (card == null)
            {
                Debug.LogWarning("[OpponentHandView] Cannot add null card to hand");
                return;
            }

            // Only add opponent cards to the opponent hand
            if (card.Owner == matchManager.Player)
            {
                Debug.LogWarning($"[OpponentHandView] Attempted to add player card {card.Title} to opponent hand");
                return;
            }

            // Check if this card already exists in the hand
            if (IsCardInHand(card))
            {
                Debug.Log($"[OpponentHandView] Card {card.Title} is already in hand");
                return;
            }

            // Create UI for the card - opponent cards are face down
            CardView cardView = CreateCardUI(card, true);

            if (cardView != null)
            {
                // Opponent cards aren't draggable - CardView will determine this automatically
                // based on the card owner in its UpdateInteractivity method

                // Update the card's interactivity based on its state and location
                cardView.UpdateInteractivity();

                Debug.Log($"[OpponentHandView] Added card {card.Title} to opponent hand");

                // Re-arrange the cards in the hand
                ArrangeCards();
            }
        }
    }
}
