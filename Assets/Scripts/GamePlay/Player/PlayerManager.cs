using System;
using System.Collections.Generic;
using Controllers;
using Cysharp.Threading.Tasks;
using GamePlay.Cards;
using GamePlay.Interfaces;
using Managers;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace GamePlay.Player
{
    public class PlayerManager : MonoBehaviour, IPlayerManager
    {
        public LocalPlayer localPlayerPrefab;
        public BotPlayer botPlayerPrefab;

        public int dealerIndex;
        public int LeadPlayerIndex => (dealerIndex + 1) % 4;
        public TeamData TeamA { get; set; }
        public TeamData TeamB { get; set; }
        
        private List<PlayerBase> _players = new List<PlayerBase>();
        private GamePlayScreen _gamePlayScreen;
        private GamePlayScreen GamePlayScreen {
            get
            {
                if (_gamePlayScreen == null)
                    _gamePlayScreen = UiManager.Instance.GetUiView(UiScreenName.GamePlayScreens) as GamePlayScreen;
                return _gamePlayScreen;
            }
            set => _gamePlayScreen = value;
        }
        
        public UniTask Initialize(GamePlayScreen gamePlayScreen)
        {
            GamePlayScreen = gamePlayScreen;
            _players.Clear();
            
            SpawnPlayers();
            
            GamePlayScreen.UpdateScore(TeamA, TeamB);
            
            return UniTask.CompletedTask;
        }

        private void SpawnPlayers()
        {
            var localPlayer = Instantiate(localPlayerPrefab, Vector3.zero, Quaternion.identity);
            localPlayer.Initialize(0, GamePlayScreen.GetPlayerElement(0));
            _players.Add(localPlayer);

            for (var i = 1; i < 4; i++)
            {
                var botPlayer = Instantiate(botPlayerPrefab, Vector3.zero, Quaternion.identity);
                botPlayer.Initialize(i, GamePlayScreen.GetPlayerElement(i));
                _players.Add(botPlayer);
            }
                    
            TeamA = new TeamData()
            {
                players = new List<PlayerBase>()
                {
                    _players[0],
                    _players[2]
                },
                score = 0,
                teamName = "Team A"
            };
            TeamB = new TeamData()
            {
                players = new List<PlayerBase>()
                {
                    _players[1],
                    _players[3]
                },
                score = 0,
                teamName = "Team B"
            };

            dealerIndex = Random.Range(0, 4);
        }

        public List<PlayerBase> GetPlayers() => _players;
        public PlayerBase GetPlayer(int index) => _players[index];
        public PlayerBase GetDealerPlayer() => _players[dealerIndex];
        public PlayerBase GetLeadPlayerToPlay() => _players[(dealerIndex + 1) % 4];
        public PlayerBase GetNextPlayerToPlay(PlayerBase currentPlayer)
        {
            int currentIndex = _players.IndexOf(currentPlayer);
            if (currentIndex == -1)
            {
                Debug.LogError("Current player not found in player list.");
                return null;
            }

            int nextIndex = (currentIndex + 1) % _players.Count;
            return _players[nextIndex];
        }
        public PlayerBase GetOppositePlayerOfTeam(PlayerBase trumpCaller)
        {
            var index = _players.IndexOf(trumpCaller);
            if (index == -1)
            {
                Debug.LogError("Trump caller not found in player list.");
                return null;
            }

            var oppositeIndex = (index + 2) % 4;
            return _players[oppositeIndex];
        }
        public int TeamAScore() => TeamA.score;
        public int TeamBScore() => TeamB.score;
        
        
    }
    
}