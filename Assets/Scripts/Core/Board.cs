using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Utils;

namespace Kardx.Core
{
    public class Board
    {
        private Player[] players = new Player[2];
        private List<GameEffect> activeEffects = new();
        private int turnNumber;
        private string currentPlayerId;
        private ILogger logger;

        // Public properties
        public Player Player => players[0];
        public Player Opponent => players[1];
        public int TurnNumber => turnNumber;
        public string CurrentPlayerId => currentPlayerId;
        public Player CurrentPlayer => currentPlayerId == players[0].Id ? players[0] : players[1];
        public Player Player1 => players[0];
        public Player Player2 => players[1];

        public Board(Player player, Player opponent, ILogger logger = null)
        {
            players[0] = player;
            players[1] = opponent;
            turnNumber = 1;
            currentPlayerId = player.Id;
            this.logger = logger;
        }

        // State management methods

        /// <summary>
        /// Sets the current player ID.
        /// </summary>
        /// <param name="playerId">The ID of the player to set as current.</param>
        public void SetCurrentPlayer(string playerId)
        {
            if (playerId == Player.Id || playerId == Opponent.Id)
            {
                currentPlayerId = playerId;
            }
            else
            {
                throw new ArgumentException($"Invalid player ID: {playerId}");
            }
        }

        /// <summary>
        /// Increments the turn number.
        /// </summary>
        public void IncrementTurnNumber()
        {
            turnNumber++;
        }

        /// <summary>
        /// Switches the current player to the other player.
        /// </summary>
        public void SwitchCurrentPlayer()
        {
            currentPlayerId = currentPlayerId == Player.Id ? Opponent.Id : Player.Id;
        }

        /// <summary>
        /// Gets the ID of the next player (the player who is not the current player).
        /// </summary>
        /// <returns>The ID of the next player.</returns>
        public string GetNextPlayerId()
        {
            // Simple two-player implementation
            return currentPlayerId == Player.Id ? Opponent.Id : Player.Id;
        }

        // Effect management
        public void AddGameEffect(GameEffect effect)
        {
            if (effect != null)
            {
                activeEffects.Add(effect);
            }
        }

        public void RemoveGameEffect(GameEffect effect)
        {
            activeEffects.Remove(effect);
        }

        public void ClearExpiredEffects()
        {
            activeEffects.RemoveAll(e => !e.IsActive());
        }

        /// <summary>
        /// Processes effects that occur at the end of a turn.
        /// </summary>
        public void ProcessEndOfTurnEffects()
        {
            // Process game effects
            foreach (var effect in activeEffects.ToList())
            {
                effect.OnTurnEnd(turnNumber);
                if (!effect.IsActive())
                {
                    activeEffects.Remove(effect);
                }
            }
        }

        /// <summary>
        /// Processes effects that occur at the start of a turn.
        /// </summary>
        public void ProcessStartOfTurnEffects()
        {
            // Process game effects
            foreach (var effect in activeEffects)
            {
                effect.OnTurnStart(turnNumber);
            }
        }

        // Reset state
        public void Reset()
        {
            // Reset players
            players[0] = null;
            players[1] = null;

            // Clear effects
            activeEffects.Clear();

            // Reset turn state
            turnNumber = 1;
            currentPlayerId = players[0].Id;
        }
    }

    public class GameEffect
    {
        private string id;
        private string description;
        private int duration; // -1 for permanent effects

        public string Id => id;
        public string Description => description;
        public int Duration => duration;

        public GameEffect(string id, string description, int duration)
        {
            this.id = id;
            this.description = description;
            this.duration = duration;
        }

        public bool IsActive()
        {
            return duration == -1 || duration > 0;
        }

        public virtual void OnTurnStart(int turnNumber)
        {
            // Override in derived classes
        }

        public virtual void OnTurnEnd(int turnNumber)
        {
            if (duration > 0)
            {
                duration--;
            }
        }
    }
}
