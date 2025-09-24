using System.Collections.Generic;
using GamePlay.Cards;

namespace GamePlay.Bot
{
    public class GameState
    {
        public List<CardData> Hand;
        public List<CardData> PlayedCards;
        public Dictionary<int, CardData> CurrentTrickCards;
        public Suit TrumpSuit;
        public Suit TrickSuit;
        public int BotPlayerIndex;
        public List<PlayerSummary> AllPlayers;
        public int TeamNumber;
        public List<CardData> Kitty;
        public int TeamScore;
    }
}