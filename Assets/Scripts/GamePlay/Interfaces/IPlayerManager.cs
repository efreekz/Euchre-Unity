using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamePlay.Player;
using Ui.GamePlayScreens;

namespace GamePlay.Interfaces
{
    public interface IPlayerManager
    {
        public UniTask Initialize(GamePlayScreen gamePlayScreen);
        public PlayerBase GetDealerPlayer();
        public PlayerBase GetLeadPlayerToPlay();
        public List<PlayerBase> GetPlayers();
        public PlayerBase GetPlayer(int index);
        public PlayerBase GetOppositePlayerOfTeam(PlayerBase trumpCaller);
        public PlayerBase GetNextPlayerToPlay(PlayerBase currentPlayer);
        public int TeamAScore();
        public int TeamBScore();
    }
}