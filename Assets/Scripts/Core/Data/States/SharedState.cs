using UnityEngine;
using System;
using System.Collections.Generic;
using Kardx.Core.Data.Cards;

namespace Kardx.Core.Data.States
{
  [CreateAssetMenu(fileName = "SharedState", menuName = "Kardx/Shared State")]
  public class SharedState : ScriptableObject
  {
    [Header("Card Collections")]
    [SerializeField] private List<Card> neutralDeckCards = new();
    [SerializeField] private List<Card> exileCards = new();
    [SerializeField] private List<Card> limboCards = new();

    // Runtime collections
    private Stack<Card> neutralDeck;

    private void OnEnable()
    {
      InitializeCollections();
    }

    private void InitializeCollections()
    {
      neutralDeck = new Stack<Card>();
      foreach (var card in neutralDeckCards)
      {
        neutralDeck.Push(card);
      }
    }

    // Public properties
    public IReadOnlyCollection<Card> NeutralDeck => neutralDeck;
    public IReadOnlyList<Card> Exile => exileCards;
    public IReadOnlyList<Card> Limbo => limboCards;

    // Neutral deck management
    public void SetNeutralDeck(IEnumerable<Card> cards)
    {
      neutralDeckCards.Clear();
      neutralDeck.Clear();
      foreach (var card in cards)
      {
        neutralDeckCards.Add(card);
        neutralDeck.Push(card);
      }
    }

    public void ShuffleNeutralDeck()
    {
      var tempList = new List<Card>(neutralDeck);
      neutralDeck.Clear();

      // Fisher-Yates shuffle
      for (int i = tempList.Count - 1; i > 0; i--)
      {
        int j = UnityEngine.Random.Range(0, i + 1);
        (tempList[i], tempList[j]) = (tempList[j], tempList[i]);
      }

      foreach (var card in tempList)
      {
        neutralDeck.Push(card);
      }

      // Update serialized list
      neutralDeckCards = tempList;
    }

    public Card DrawNeutralCard()
    {
      if (neutralDeck.Count > 0)
      {
        var card = neutralDeck.Pop();
        neutralDeckCards.Remove(card);
        return card;
      }
      return null;
    }

    // Exile zone management
    public void ExileCard(Card card)
    {
      if (card != null && !exileCards.Contains(card))
      {
        exileCards.Add(card);
      }
    }

    public bool ReturnFromExile(Card card)
    {
      return exileCards.Remove(card);
    }

    // Limbo zone management (for cards in transition)
    public void MoveToLimbo(Card card)
    {
      if (card != null && !limboCards.Contains(card))
      {
        limboCards.Add(card);
      }
    }

    public bool RemoveFromLimbo(Card card)
    {
      return limboCards.Remove(card);
    }

    public void ClearLimbo()
    {
      limboCards.Clear();
    }

    // Zone transfer methods
    public bool TransferToExile(Card card, ZoneType sourceZone, PlayerState sourcePlayer = null)
    {
      bool removed = false;

      switch (sourceZone)
      {
        case ZoneType.Limbo:
          removed = RemoveFromLimbo(card);
          break;
        case ZoneType.Hand:
        case ZoneType.Battlefield:
        case ZoneType.DiscardPile:
          if (sourcePlayer != null)
          {
            // Implementation depends on PlayerState methods
            // This is a placeholder for the actual implementation
            removed = true;
          }
          break;
      }

      if (removed)
      {
        ExileCard(card);
        return true;
      }

      return false;
    }

    public bool TransferToLimbo(Card card, ZoneType sourceZone, PlayerState sourcePlayer = null)
    {
      bool removed = false;

      switch (sourceZone)
      {
        case ZoneType.Exile:
          removed = ReturnFromExile(card);
          break;
        case ZoneType.Hand:
        case ZoneType.Battlefield:
        case ZoneType.DiscardPile:
          if (sourcePlayer != null)
          {
            // Implementation depends on PlayerState methods
            // This is a placeholder for the actual implementation
            removed = true;
          }
          break;
      }

      if (removed)
      {
        MoveToLimbo(card);
        return true;
      }

      return false;
    }
  }
}