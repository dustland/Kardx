using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kardx.Core
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
        
        // Events
        public event Action<Player> OnTurnStarted;
        public event Action<Player> OnTurnEnded;
        
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
            
            // Start with player's turn
            currentTurnPlayer = player;
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
        /// Starts a new turn for the next player.
        /// </summary>
        public void StartNextTurn()
        {
            // End current turn
            OnTurnEnded?.Invoke(currentTurnPlayer);
            
            // Switch current player
            currentTurnPlayer = (currentTurnPlayer == player) ? opponent : player;
            
            // Increment turn number if it's the player's turn
            if (currentTurnPlayer == player)
            {
                turnNumber++;
            }
            
            // Add credits for the new turn
            currentTurnPlayer.AddCredits(Player.CREDITS_PER_TURN);
            
            // Draw a card for the new turn
            currentTurnPlayer.DrawCard();
            
            // Notify turn started
            OnTurnStarted?.Invoke(currentTurnPlayer);
        }
        
        /// <summary>
        /// Gets the other player.
        /// </summary>
        /// <param name="player">The current player.</param>
        /// <returns>The other player.</returns>
        public Player GetOtherPlayer(Player player)
        {
            return player == this.player ? opponent : this.player;
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
        public void SwitchCurrentPlayer()
        {
            currentTurnPlayer = GetOtherPlayer(currentTurnPlayer);
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
