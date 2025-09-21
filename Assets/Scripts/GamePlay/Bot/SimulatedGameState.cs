using System.Collections.Generic;
using GamePlay.Cards;

namespace GamePlay.Bot
{
    public class SimulatedGameState
    {
        public Dictionary<int, List<CardData>> Hands;
        public Dictionary<int, CardData> Trick;
        public Suit LeadSuit;
        public int CurrentPlayer;
    }
}