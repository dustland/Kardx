using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kardx.Models.Match
{
    /// <summary>
    /// Represents the game board that manages players and game state.
    /// </summary>
    public class Board
    {
        private Player player;
        private Player opponent;
        private Player currentTurnPlayer;
        private int turnNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="Board"/> class.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="opponent">The opponent.</param>
        public Board(Player player, Player opponent)
        {
            this.player = player;
            this.opponent = opponent;

            // Set board reference in players
            player.SetBoard(this);
            opponent.SetBoard(this);

            // Start with opponent's turn
            currentTurnPlayer = opponent;
            turnNumber = 1;
        }

        /// <summary>
        /// Gets the player.
        /// </summary>
        public Player Player => player;

        /// <summary>
        /// Gets the opponent.
        /// </summary>
        public Player Opponent => opponent;

        /// <summary>
        /// Gets the current turn player.
        /// </summary>
        public Player CurrentTurnPlayer => currentTurnPlayer;

        /// <summary>
        /// Gets the current turn number.
        /// </summary>
        public int TurnNumber => turnNumber;

        /// <summary>
        /// Checks if a player is the opponent.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player is the opponent, false otherwise.</returns>
        public bool IsOpponent(Player player)
        {
            return player == opponent;
        }

        /// <summary>
        /// Checks if it's the player's turn.
        /// </summary>
        /// <returns>True if it's the player's turn, false otherwise.</returns>
        public bool IsPlayerTurn()
        {
            return currentTurnPlayer == player;
        }

        /// <summary>
        /// Ends the current turn.
        /// </summary>
        public void EndTurn()
        {
            // Switch the current player
            currentTurnPlayer = currentTurnPlayer == player ? opponent : player;

            // Increment turn number when player's turn starts
            if (currentTurnPlayer == player)
            {
                turnNumber++;
            }
        }

        /// <summary>
        /// Starts a new turn for the current player.
        /// </summary>
        public void StartTurn()
        {
            // Refresh the current player's resources
            currentTurnPlayer.AddCredits(Player.CREDITS_PER_TURN);

            // Reset attack counts for all cards would be implemented here
            // This functionality would need to be added to Battlefield class
        }

        /// <summary>
        /// Processes end of turn effects for all cards in play.
        /// </summary>
        public void ProcessEndOfTurnEffects()
        {
            // Process all cards in play to check for end of turn effects
            foreach (var card in player.GetCardsInPlay())
            {
                card.ProcessEndOfTurnEffects();
            }

            foreach (var card in opponent.GetCardsInPlay())
            {
                card.ProcessEndOfTurnEffects();
            }
        }

        /// <summary>
        /// Processes start of turn effects for all cards in play.
        /// </summary>
        public void ProcessStartOfTurnEffects()
        {
            // Process all cards in play to check for start of turn effects
            foreach (var card in player.GetCardsInPlay())
            {
                card.ProcessStartOfTurnEffects();
            }

            foreach (var card in opponent.GetCardsInPlay())
            {
                card.ProcessStartOfTurnEffects();
            }
        }

        /// <summary>
        /// Switches the current player to the other player.
        /// </summary>
        /// <returns>The new current player.</returns>
        public Player SwitchCurrentPlayer()
        {
            currentTurnPlayer = currentTurnPlayer == player ? opponent : player;
            return currentTurnPlayer;
        }

        /// <summary>
        /// Increments the turn number.
        /// </summary>
        public void IncrementTurnNumber()
        {
            turnNumber++;
        }
    }
}
