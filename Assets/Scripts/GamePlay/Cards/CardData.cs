using System;
using System.Collections.Generic;
using Fusion;
using GamePlay.Player;
using UnityEngine;

namespace GamePlay.Cards
{
    public enum Suit : byte { Hearts, Diamonds, Clubs, Spades, None }
    public enum Rank : byte { Nine = 0, Ten = 1, Jack = 2, Queen = 3, King = 4, Ace = 5 }
    public enum CardColor : byte { Red, Black }
    public enum CardState : byte { InDeck, InHand, }
    public enum TeamType : byte {Makers, Defenders}

    [Serializable]
    public class CardData
    {
        public Suit suit;
        public Rank rank;
        public Sprite sprite;
        public CardColor color;
        public bool isFaceUp;

        public CardData(Suit suit, Rank rank)
        {
            this.suit = suit;
            this.rank = rank;
            color = this.suit is Suit.Hearts or Suit.Diamonds ? CardColor.Red : CardColor.Black;
        }
        
        public int GetCardPower(Suit trumpSuit, Suit leadSuit)
        {
            IsTrump(trumpSuit);
            var isLeftBower = IsLeftBower(trumpSuit);
            var isRightBower = IsRightBower(trumpSuit);

            if (isRightBower) return 100; // Highest
            if (isLeftBower) return 99;   // Second highest

            if (suit == trumpSuit) return 90 + (int)rank;
            if (suit == leadSuit) return 50 + (int)rank;

            return 10 + (int)rank; // Non-trump, non-lead suit
        }

        private bool IsRightBower(Suit trumpSuit)
        {
            return rank == Rank.Jack && suit == trumpSuit;
        }

        private bool IsLeftBower(Suit trumpSuit)
        {
            if (rank != Rank.Jack) return false;

            return (trumpSuit == Suit.Spades && suit == Suit.Clubs) ||
                   (trumpSuit == Suit.Clubs && suit == Suit.Spades) ||
                   (trumpSuit == Suit.Hearts && suit == Suit.Diamonds) ||
                   (trumpSuit == Suit.Diamonds && suit == Suit.Hearts);
        }

        public bool IsTrump(Suit trumpSuit)
        {
            return suit == trumpSuit || IsLeftBower(trumpSuit);
        }
        
        // ✅ Operator overloads
        public static bool operator ==(CardData a, CardData b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            return a.suit == b.suit && a.rank == b.rank;
        }

        public static bool operator !=(CardData a, CardData b) => !(a == b);

        // Also override Equals and GetHashCode for best practice
        public override bool Equals(object obj)
        {
            if (obj is not CardData other) return false;
            return this == other;
        }

        public override int GetHashCode() => (int)suit * 31 + (int)rank;
        
        public override string ToString()
        {
            var rankStr = rank.ToString();
            var suitStr = suit.ToString();

            return suit == Suit.None ? $"<color=red>{rankStr} of No Suit</color>" : $"{rankStr} of {suitStr}";
        }

    }
    
    [Serializable]
    public class CardDataDto
    {
        public Suit suit;
        public Rank rank;
    }

    
    [Serializable]
    public class SuitData
    {
        public Suit suit;
        public Sprite sprite;
    }
    
    [Serializable]
    public class TeamData
    {
        public string teamName;
        public List<PlayerBase> players;
        public int score;
        public TeamType teamType;
        public bool willGoAlone;
    }
    
    [Serializable]
    public struct NetworkTeamData : INetworkStruct
    {
        public NetworkString<_16> teamName;
        public int player0Index;
        public int player1Index;
        public int score;
        public TeamType teamType;
        public NetworkBool willGoAlone;
    }
    
    
    public class TrumpSelectionData
    {
        public int PlayerIndex;
        public Suit Suit;
        public int Choice;

        public TrumpSelectionData(int playerindex, Suit suit, int choice)
        {
            PlayerIndex = playerindex;
            Suit = suit;
            Choice = choice;
        }
    }


    public struct TrumpSelectionDataNetworked : INetworkStruct
    {
        public PlayerRef Player;
        public Suit Suit;
        public int Choice;
    }
    
}