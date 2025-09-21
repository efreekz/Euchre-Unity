using System.Collections.Generic;
using GamePlay.Cards;

namespace GamePlay.Bot
{
    public class PlayerSummary
    {
        public int PlayerIndex { get; set; }        // Index 0-3
        public bool IsBotPartner { get; set; }      // True if this is the bot's teammate
        public List<CardData> CardsPlayed { get; set; } // Cards this player has played so far
        public bool HasPlayedInCurrentTrick { get; set; } // Helpful for trick order tracking

        // Optional (Advanced Inference)
        public int? EstimatedTrumpCount { get; set; }    // If you want to track suspicion of trump possession
        public bool IsLikelyVoidInSuit(Suit suit) =>     // Inference based on inability to follow suit
            SuitVoidFlags.Contains(suit);

        private HashSet<Suit> SuitVoidFlags { get; set; } = new();

        public void MarkVoidInSuit(Suit suit)
        {
            SuitVoidFlags.Add(suit);
        }
    }

}