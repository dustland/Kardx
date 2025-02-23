using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core.Data.Cards;

namespace Kardx.Core.Data.States
{
    public class BoardState
    {
        private Dictionary<string, PlayerState> players = new();
        private SharedState global;
        private List<GameEffect> activeEffects = new();
        private int turnNumber;
        private string currentPlayerId;
        private string player1Id;
        private string player2Id;

        // Public properties
        public IReadOnlyDictionary<string, PlayerState> Players => players;
        public SharedState Global => global;
        public int TurnNumber => turnNumber;
        public string CurrentPlayerId => currentPlayerId;

        public BoardState()
        {
            turnNumber = 1;
        }

        // State management
        public void AddPlayer(string playerId, PlayerState state)
        {
            if (!players.ContainsKey(playerId))
            {
                players[playerId] = state;
                if (player1Id == null)
                {
                    player1Id = playerId;
                }
                else if (player2Id == null)
                {
                    player2Id = playerId;
                }
            }
        }

        public bool RemovePlayer(string playerId)
        {
            return players.Remove(playerId);
        }

        public void SetCurrentPlayer(string playerId)
        {
            if (players.ContainsKey(playerId))
            {
                currentPlayerId = playerId;
            }
        }

        // Turn management
        public void StartNextTurn()
        {
            // Process end of turn effects
            ProcessEndOfTurn();

            // Switch current player
            currentPlayerId = GetNextPlayerId();
            turnNumber++;

            // Process start of turn effects
            ProcessStartOfTurn();
        }

        public void IncrementTurn()
        {
            turnNumber++;
        }

        public void SwitchCurrentPlayer()
        {
            currentPlayerId = currentPlayerId == player1Id ? player2Id : player1Id;
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

        // Private helper methods
        private string GetNextPlayerId()
        {
            // Simple two-player implementation
            return players.Keys.First(id => id != currentPlayerId);
        }

        private void ProcessEndOfTurn()
        {
            var currentPlayer = players[currentPlayerId];

            // Process card modifiers
            foreach (var card in currentPlayer.Battlefield.Values)
            {
                card.ClearExpiredModifiers();
            }

            // Process game effects
            foreach (var effect in activeEffects.ToList())
            {
                effect.OnTurnEnd();
                if (!effect.IsActive())
                {
                    activeEffects.Remove(effect);
                }
            }
        }

        private void ProcessStartOfTurn()
        {
            var currentPlayer = players[currentPlayerId];

            // Refresh player resources
            currentPlayer.RefreshCredits();

            // Process game effects
            foreach (var effect in activeEffects)
            {
                effect.OnTurnStart();
            }
        }

        // Reset state
        public void Reset()
        {
            players.Clear();
            activeEffects.Clear();
            turnNumber = 1;
            currentPlayerId = null;
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

        public virtual void OnTurnStart()
        {
            // Override in derived classes
        }

        public virtual void OnTurnEnd()
        {
            if (duration > 0)
            {
                duration--;
            }
        }
    }
}
