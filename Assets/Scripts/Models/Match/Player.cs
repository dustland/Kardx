using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Utils;

namespace Kardx.Models.Match
{
    /// <summary>
    /// Represents the state of a player in the game.
    /// </summary>
    public class Player
    {
        public const int BATTLEFIELD_SLOT_COUNT = GameConstants.BattlefieldSlotCount;
        public const int CREDITS_PER_TURN = 1;

        private string playerId;
        private ILogger logger;
        private int credits;
        private int turnsPlayed;
        private Faction faction;

        private Hand hand;
        private Battlefield battlefield;
        private Deck deck;
        private DiscardPile discardPile;
        private Card headquartersCard;
        private Board board;

        public Player(
            string playerId,
            List<Card> initialDeck,
            Faction faction = Faction.Neutral,
            ILogger logger = null,
            Board board = null
        )
        {
            Initialize(playerId, initialDeck, faction, logger, board);
        }

        public void Initialize(
            string playerId,
            List<Card> initialDeck,
            Faction faction = Faction.Neutral,
            ILogger logger = null,
            Board board = null
        )
        {
            this.playerId = playerId;
            this.logger = logger;
            this.credits = 0;
            this.turnsPlayed = 0;
            this.faction = faction;
            this.board = board;

            hand = new Hand(this);
            battlefield = new Battlefield(this);
            deck = new Deck(this);
            discardPile = new DiscardPile(this);

            foreach (var card in initialDeck)
            {
                card.SetOwner(this);
                deck.AddCard(card);
            }

            deck.Shuffle();
        }

        public string Id => playerId;
        public Faction Faction => faction;
        public Hand Hand => hand;
        public Battlefield Battlefield => battlefield;
        public Deck Deck => deck;
        public DiscardPile DiscardPile => discardPile;
        public Card Headquarter => headquartersCard;
        public int Credits => credits;
        public int TurnsPlayed => turnsPlayed;
        public bool IsOpponent => board != null && board.IsOpponent(this);

        public Card DrawCard(bool faceDown = false)
        {
            if (hand.IsFull)
            {
                logger?.Log(
                    $"[{playerId}] Cannot draw card - hand is full ({GameConstants.MaxHandSize} cards)"
                );
                return null;
            }

            var card = deck.DrawCard();
            if (card == null)
            {
                logger?.Log($"[{playerId}] Cannot draw card - deck is empty");
                return null;
            }

            card.SetFaceDown(faceDown);
            hand.AddCard(card);
            return card;
        }

        public bool DiscardFromHand(Card card)
        {
            if (card == null || !hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot discard card - card not in hand");
                return false;
            }

            hand.RemoveCard(card);
            discardPile.AddCard(card);
            card.SetFaceDown(false);
            return true;
        }

        public bool DeployUnitCard(Card card, int slotIndex)
        {
            if (card == null)
                return false;

            if (!hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot deploy card - card not in hand");
                return false;
            }

            if (card.CardType.Category != CardCategory.Unit)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not a unit card");
                return false;
            }

            if (credits < card.CardType.Cost)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not enough credits");
                return false;
            }

            if (!battlefield.IsSlotEmpty(slotIndex))
            {
                logger?.Log($"[{playerId}] Cannot deploy card - slot {slotIndex} is not empty");
                return false;
            }

            hand.RemoveCard(card);
            SpendCredits(card.CardType.Cost);
            card.SetFaceDown(false);
            card.MarkDeployed();
            return battlefield.DeployCard(card, slotIndex);
        }

        public bool DeployOrderCard(Card card)
        {
            if (card == null)
                return false;

            if (!hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot deploy card - card not in hand");
                return false;
            }

            if (card.CardType.Category != CardCategory.Order)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not an order card");
                return false;
            }

            if (credits < card.CardType.Cost)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not enough credits");
                return false;
            }

            hand.RemoveCard(card);
            SpendCredits(card.CardType.Cost);
            card.SetFaceDown(false);
            discardPile.AddCard(card);
            return true;
        }

        public bool MoveToDiscard(Card card)
        {
            if (card == null)
                return false;

            if (battlefield.Contains(card))
            {
                battlefield.RemoveCard(card);
            }
            else if (hand.Contains(card))
            {
                hand.RemoveCard(card);
            }
            else
            {
                return false;
            }

            discardPile.AddCard(card);
            return true;
        }

        public bool DestroyCard(Card card)
        {
            return MoveToDiscard(card);
        }

        public bool RemoveFromBattlefield(Card card)
        {
            if (card == null)
                return false;

            if (!battlefield.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot remove card from battlefield - card not on battlefield");
                return false;
            }

            battlefield.RemoveCard(card);
            discardPile.AddCard(card);
            logger?.Log($"[{playerId}] Card {card.Title} removed from battlefield to discard pile");
            return true;
        }

        public bool SpendCredits(int amount)
        {
            if (amount <= 0)
                return false;

            if (credits < amount)
            {
                logger?.Log($"[{playerId}] Cannot spend {amount} credits - only have {credits}");
                return false;
            }

            credits -= amount;
            return true;
        }

        public void RefreshCreditsForTurn()
        {
            turnsPlayed++;
            credits = Math.Min(turnsPlayed, GameConstants.MaxCredits);
            logger?.Log($"[{playerId}] Credits refreshed to {credits} (turn {turnsPlayed})");
        }

        public void SetBoard(Board board)
        {
            this.board = board;
        }

        public List<Card> GetCardsInPlay()
        {
            var cardsInPlay = new List<Card>(battlefield.Cards);
            if (headquartersCard != null && headquartersCard.IsAlive)
            {
                cardsInPlay.Add(headquartersCard);
            }
            return cardsInPlay;
        }

        public bool HasFrontlineUnits()
        {
            for (int i = 0; i < GameConstants.FrontlineSlotCount; i++)
            {
                if (!battlefield.IsSlotEmpty(i))
                    return true;
            }
            return false;
        }

        public void SetHeadquarters(Card card)
        {
            if (card == null || card.CardType.Category != CardCategory.Headquarters)
                return;

            headquartersCard = card;
            card.SetOwner(this);
            card.SetZone(ZoneType.Battlefield);
            card.SetFaceDown(false);
        }

        public void ResetCardAttackStatus()
        {
            foreach (var card in battlefield.Cards)
            {
                card.ProcessStartOfTurnEffects();
            }

            logger?.Log($"[{playerId}] Reset attack status for all cards on the battlefield");
        }

        public int GetTotalCardCount()
        {
            return deck.Count + hand.Count + battlefield.Count + discardPile.Count
                + (headquartersCard != null ? 1 : 0);
        }

        public bool MoveUnitOnBattlefield(Card card, int toSlotIndex)
        {
            if (card == null || card.CardType.Category != CardCategory.Unit)
                return false;

            if (!battlefield.Contains(card))
                return false;

            if (!battlefield.IsSlotEmpty(toSlotIndex))
                return false;

            return battlefield.MoveCard(card, toSlotIndex);
        }

        public bool PlayCountermeasureCard(Card card)
        {
            if (card == null)
                return false;

            if (!hand.Contains(card))
                return false;

            if (card.CardType.Category != CardCategory.Countermeasure)
                return false;

            if (credits < card.CardType.Cost)
                return false;

            hand.RemoveCard(card);
            SpendCredits(card.CardType.Cost);
            card.SetFaceDown(false);
            discardPile.AddCard(card);
            return true;
        }
    }
}
