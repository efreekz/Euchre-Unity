using System;
using System.Collections.Generic;
using UnityEngine;

public class EuchreBot
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Nine = 0, Ten = 1, Jack = 2, Queen = 3, King = 4, Ace = 5 } // Match main game system

    public class Card
    {
        public Suit Suit;
        public Rank Rank;
        public Card(Suit suit, Rank rank) { Suit = suit; Rank = rank; }
    }

    public List<Card> Hand = new List<Card>();
    public Suit? TrumpSuit;
    public bool IsDealer;
    public bool IsMaker;
    public EuchreBot Partner;
    public List<Card> PlayedCards = new List<Card>();

    // Basic MCTS-like card selection (randomized for now)
    public Card SelectCard(Suit? suitLed)
    {
        List<Card> validCards = GetValidCards(suitLed);
        // Advanced etiquette: Don't trump partner's Ace unless necessary
        if (suitLed.HasValue && validCards.Count > 1)
        {
            Card partnerLed = Partner != null && Partner.PlayedCards.Count > 0 ? Partner.PlayedCards[Partner.PlayedCards.Count - 1] : null;
            if (partnerLed != null && partnerLed.Rank == Rank.Ace && partnerLed.Suit == suitLed.Value)
            {
                List<Card> trumpCards = validCards.FindAll(c => c.Suit == TrumpSuit);
                if (trumpCards.Count > 0)
                {
                    bool opponentTrumped = PlayedCards.Exists(c => c.Suit == TrumpSuit && c.Rank > Rank.Ace);
                    if (!opponentTrumped)
                    {
                        List<Card> nonTrump = validCards.FindAll(c => c.Suit != TrumpSuit);
                        if (nonTrump.Count > 0)
                            return nonTrump[UnityEngine.Random.Range(0, nonTrump.Count)];
                    }
                }
            }
        }
        // Otherwise, pick highest value card
        Card best = validCards[0];
        foreach (var card in validCards)
        {
            if ((int)card.Rank > (int)best.Rank) best = card;
        }
        return best;
    }

    public List<Card> GetValidCards(Suit? suitLed)
    {
        if (!suitLed.HasValue) return new List<Card>(Hand);
        
        // Need to check for trump-aware suit matching including bowers
        if (TrumpSuit.HasValue)
        {
            List<Card> matchingCards = Hand.FindAll(c => GetEffectiveSuit(c, TrumpSuit.Value) == suitLed.Value);
            return matchingCards.Count > 0 ? matchingCards : new List<Card>(Hand);
        }
        else
        {
            // Fallback when trump suit not set yet
            List<Card> inSuit = Hand.FindAll(c => c.Suit == suitLed.Value);
            return inSuit.Count > 0 ? inSuit : new List<Card>(Hand);
        }
    }
    
    // Helper method to get effective suit for a card (handles bower trump conversion)
    private Suit GetEffectiveSuit(Card card, Suit trumpSuit)
    {
        // Left bower is treated as trump suit
        if (card.Rank == Rank.Jack)
        {
            if ((trumpSuit == Suit.Spades && card.Suit == Suit.Clubs) ||
                (trumpSuit == Suit.Clubs && card.Suit == Suit.Spades) ||
                (trumpSuit == Suit.Hearts && card.Suit == Suit.Diamonds) ||
                (trumpSuit == Suit.Diamonds && card.Suit == Suit.Hearts))
            {
                return trumpSuit;
            }
        }
        return card.Suit;
    }

    // Basic bid logic (advanced conventions)
    public bool ShouldBid(Suit flippedSuit, int bidRound, int tablePosition)
    {
        // Lone call logic: 4+ trump or 2 bowers + ace
        int trumpCount = CountTrumpCards(flippedSuit);
        int bowerCount = CountBowers(flippedSuit);
        bool hasAce = Hand.Exists(c => GetEffectiveSuit(c, flippedSuit) == flippedSuit && c.Rank == Rank.Ace);
        if (trumpCount >= 4 || (bowerCount == 2 && hasAce))
            return true;
        // 'Next' logic for first seat
        if (bidRound == 1 && tablePosition == 0)
        {
            Suit nextSuit = GetNextSuit(flippedSuit);
            int nextTrumpCount = CountTrumpCards(nextSuit);
            int highTrump = CountHighTrumpCards(nextSuit);
            bool hasOffAce = Hand.Exists(c => c.Rank == Rank.Ace && GetEffectiveSuit(c, nextSuit) != nextSuit);
            if (highTrump >= 2 && nextTrumpCount >= 2 && hasOffAce)
                return true;
        }
        return false;
    }
    
    // Helper method to count trump cards including left bower
    private int CountTrumpCards(Suit trumpSuit)
    {
        return Hand.Count(c => GetEffectiveSuit(c, trumpSuit) == trumpSuit);
    }
    
    // Helper method to count bowers (right + left)
    private int CountBowers(Suit trumpSuit)
    {
        int count = 0;
        foreach (var card in Hand)
        {
            if (card.Rank == Rank.Jack)
            {
                // Right bower
                if (card.Suit == trumpSuit) count++;
                // Left bower
                else if (GetEffectiveSuit(card, trumpSuit) == trumpSuit) count++;
            }
        }
        return count;
    }
    
    // Helper method to count high trump cards (Jack, Ace, King)
    private int CountHighTrumpCards(Suit trumpSuit)
    {
        return Hand.Count(c => GetEffectiveSuit(c, trumpSuit) == trumpSuit && 
                              (c.Rank == Rank.Jack || c.Rank == Rank.Ace || c.Rank == Rank.King));
    }

    public Suit GetNextSuit(Suit suit)
    {
        switch (suit)
        {
            case Suit.Hearts: return Suit.Diamonds;
            case Suit.Diamonds: return Suit.Hearts;
            case Suit.Clubs: return Suit.Spades;
            case Suit.Spades: return Suit.Clubs;
            default: return suit;
        }
    }

    // Call this after each hand to reset
    public void ResetForNewHand()
    {
        Hand.Clear();
        PlayedCards.Clear();
        TrumpSuit = null;
        IsDealer = false;
        IsMaker = false;
    }
}
