using Cysharp.Threading.Tasks;
using GamePlay.Cards;

namespace GamePlay.Interfaces
{
    public interface IGameController
    {
        public UniTask Initialize();
        public UniTask StartGame();
        
        public Suit CurrentTrickSuit { get; set; }
        public Suit TrumpSuit { get; set; }
    }
}