using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core.Data.Cards;

namespace Kardx.Core.Data.States
{
  public class BoardState : MonoBehaviour
  {
    [Header("State")]
    [SerializeField] private SerializableDictionary<string, PlayerState> players = new();
    [SerializeField] private SharedState global;
    [SerializeField] private List<GameEffect> activeEffects = new();
    [SerializeField] private int turnNumber;
    [SerializeField] private string currentPlayerId;

    // Public properties
    public IReadOnlyDictionary<string, PlayerState> Players => players;
    public SharedState Global => global;
    public IReadOnlyList<GameEffect> ActiveEffects => activeEffects;
    public int TurnNumber => turnNumber;
    public string CurrentPlayerId => currentPlayerId;

    private void Awake()
    {
      // Initialize board state
      turnNumber = 1;
    }

    // State management
    public void AddPlayer(string playerId, PlayerState state)
    {
      if (!players.ContainsKey(playerId))
      {
        players[playerId] = state;
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
        effect.OnTurnEnd(this, currentPlayerId);
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
        effect.OnTurnStart(this, currentPlayerId);
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

    // Serialization helper
    public string Serialize()
    {
      return JsonUtility.ToJson(this);
    }

    // Deserialization helper
    public static BoardState Deserialize(string json)
    {
      return JsonUtility.FromJson<BoardState>(json);
    }
  }

  [Serializable]
  public class GameEffect
  {
    [SerializeField] private string id;
    [SerializeField] private string description;
    [SerializeField] private int duration; // -1 for permanent effects

    public string Id => id;
    public string Description => description;
    public int Duration => duration;

    public GameEffect(string id, string description, int duration)
    {
      this.id = id;
      this.description = description;
      this.duration = duration;
    }

    public bool IsActive() => duration > 0 || duration == -1;

    public virtual void OnTurnStart(BoardState board, string activePlayerId)
    {
      // Override in derived classes
    }

    public virtual void OnTurnEnd(BoardState board, string activePlayerId)
    {
      if (duration > 0)
      {
        duration--;
      }
    }
  }
}