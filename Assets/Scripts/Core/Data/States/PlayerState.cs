using UnityEngine;
using System;
using System.Collections.Generic;
using Kardx.Core.Data.Cards;

namespace Kardx.Core.Data.States
{
  [CreateAssetMenu(fileName = "PlayerState", menuName = "Kardx/Player State")]
  public class PlayerState : ScriptableObject
  {
    [Header("Card Collections")]
    [SerializeField] private List<Card> deckCards = new();
    [SerializeField] private List<Card> handCards = new();
    [SerializeField] private SerializableDictionary<Vector2Int, Card> battlefieldCards = new();
    [SerializeField] private List<Card> discardCards = new();
    [SerializeField] private Card headquarter;

    [Header("Resources")]
    [SerializeField] private int credits;

    [Header("Limits")]
    [SerializeField] private const int MAX_HAND_SIZE = 10;
    [SerializeField] private const int MAX_CREDITS = 9;
    [SerializeField] private const int CREDITS_PER_TURN = 5;

    // Runtime collections
    private Stack<Card> deck;
    private Queue<Card> discardPile;

    private void OnEnable()
    {
      // Initialize runtime collections
      InitializeCollections();
    }

    private void InitializeCollections()
    {
      // Initialize deck
      deck = new Stack<Card>();
      foreach (var card in deckCards)
      {
        deck.Push(card);
      }

      // Initialize discard pile
      discardPile = new Queue<Card>();
      foreach (var card in discardCards)
      {
        discardPile.Enqueue(card);
      }
    }

    // Public properties
    public IReadOnlyCollection<Card> Deck => deck;
    public IReadOnlyList<Card> Hand => handCards;
    public IReadOnlyDictionary<Vector2Int, Card> Battlefield => battlefieldCards;
    public IReadOnlyCollection<Card> DiscardPile => discardPile;
    public int Credits => credits;
    public Card Headquarter => headquarter;

    // Deck management
    public void SetDeck(IEnumerable<Card> cards)
    {
      deckCards.Clear();
      deck.Clear();
      foreach (var card in cards)
      {
        deckCards.Add(card);
        deck.Push(card);
      }
    }

    public void ShuffleDeck()
    {
      var tempList = new List<Card>(deck);
      deck.Clear();

      // Fisher-Yates shuffle
      for (int i = tempList.Count - 1; i > 0; i--)
      {
        int j = UnityEngine.Random.Range(0, i + 1);
        (tempList[i], tempList[j]) = (tempList[j], tempList[i]);
      }

      foreach (var card in tempList)
      {
        deck.Push(card);
      }

      // Update serialized list
      deckCards = tempList;
    }

    // Hand management
    public Card DrawCard()
    {
      if (deck.Count > 0 && handCards.Count < MAX_HAND_SIZE)
      {
        var card = deck.Pop();
        handCards.Add(card);
        // Update serialized deck list
        deckCards.Remove(card);
        return card;
      }
      return null;
    }

    public void DrawCards(int count)
    {
      for (int i = 0; i < count && handCards.Count < MAX_HAND_SIZE && deck.Count > 0; i++)
      {
        DrawCard();
      }
    }

    public bool DiscardCard(Card card)
    {
      if (handCards.Remove(card))
      {
        discardPile.Enqueue(card);
        discardCards.Add(card);
        return true;
      }
      return false;
    }

    // Battlefield management
    public bool DeployCard(Card card, Vector2Int position)
    {
      if (handCards.Contains(card) && !battlefieldCards.ContainsKey(position))
      {
        handCards.Remove(card);
        battlefieldCards[position] = card;
        return true;
      }
      return false;
    }

    public bool RemoveFromBattlefield(Vector2Int position)
    {
      if (battlefieldCards.TryGetValue(position, out Card card))
      {
        battlefieldCards.Remove(position);
        discardPile.Enqueue(card);
        discardCards.Add(card);
        return true;
      }
      return false;
    }

    // Resource management
    public void RefreshCredits()
    {
      credits = Mathf.Min(credits + CREDITS_PER_TURN, MAX_CREDITS);
    }

    public bool SpendCredits(int amount)
    {
      if (amount <= credits)
      {
        credits -= amount;
        return true;
      }
      return false;
    }

    public void AddCredits(int amount)
    {
      credits = Mathf.Min(credits + amount, MAX_CREDITS);
    }

    // Headquarter management
    public void SetHeadquarter(Card card)
    {
      if (card.CardType.Category == CardCategory.Headquarter)
      {
        headquarter = card;
      }
    }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure credits are within bounds
            credits = Mathf.Clamp(credits, 0, MAX_CREDITS);
        }
#endif
  }
}