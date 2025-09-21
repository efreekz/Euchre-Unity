using System.Collections.Generic;
using System.Linq;
using Controllers;
using Cysharp.Threading.Tasks;
using Fusion;
using GamePlay.Cards;
using GamePlay.Interfaces;
using Managers;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GamePlay.Player
{
    public class PlayerManagerNetworked : NetworkBehaviour, IPlayerManager
    {
        [Header("References")]
        public NetworkPrefabRef networkPlayerPrefab;
        [Header("References")]
        public NetworkPrefabRef networkBotPrefab;
        [Networked] public bool Initialized { get; private set; }
        [Networked] public int DealerIndex { get; set; }
        [Networked] public NetworkTeamData TeamA { get; set; }
        [Networked] public NetworkTeamData TeamB { get; set; }
        private List<PlayerBase> Players { get; set; } = new List<PlayerBase>();
        private List<PlayerRef> _playerRefList = new List<PlayerRef>();
        private GamePlayScreen _gamePlayScreen;
        
        public async UniTask Initialize(GamePlayScreen gamePlayScreen)
        {
            _gamePlayScreen = gamePlayScreen;
            if (_gamePlayScreen is null)
            {
                Debug.LogError($"{nameof(Initialize)}: gamePlayScreen is null");
                return;
            }

            if (MultiplayerManager.Instance.Runner.IsServer)
            {
                foreach (var playerGameData in MultiplayerManager.Instance.RPCManager.JoinedPlayers)
                {
                    var selectedPrefab = playerGameData.IsBot ? networkBotPrefab : networkPlayerPrefab;
                    var playerObject = MultiplayerManager.Instance.Runner.Spawn(selectedPrefab, Vector3.zero, Quaternion.identity, playerGameData.PlayerRef);
                    var playerBase = playerObject.GetComponent<PlayerBase>();
                    playerBase.Initialize(playerGameData.PlayerId, _gamePlayScreen.GetPlayerElement(playerGameData.PlayerId));
                }
            }
            
            // Wait until all PlayerBase objects are spawned
            await UniTask.WaitUntil(() =>
            {
                var allPlayers = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);
                
                if (allPlayers.Length != 4) return false;

                Players = new List<PlayerBase>(new PlayerBase[4]);
                _playerRefList = new List<PlayerRef>(new PlayerRef[4]);

                var localPlayerIndex = MultiplayerManager.Instance.LocalPlayerIndex;

                foreach (var playerBase in allPlayers)
                {
                    var index = playerBase.PlayerIndex;
                    Players[index] = playerBase;
                    _playerRefList[index] = playerBase.Object.InputAuthority;

                    var assignedUIIndex = (index - localPlayerIndex + 4) % 4;

                    playerBase.Initialize(index, _gamePlayScreen.GetPlayerElement(assignedUIIndex));
                }

                return true;
            });
            
            if (Runner.IsServer)
            {
                AssignTeams();
                
                DealerIndex = Random.Range(0, 4);
                
                Initialized = true;
            }
            
            _gamePlayScreen.UpdateScore(TeamA, TeamB);

        }
        private void AssignTeams()
        {
            if (MultiplayerManager.Instance.RPCManager.GetFilledCount() != 4)
            {
                Debug.LogError("Invalid player count for team assignment");
                return;
            }

            // Assign players 0 and 2 to Team A
            TeamA = new NetworkTeamData()
            {
                player0Index = 0,
                player1Index = 2,
                score = 0,
                teamName = "Team A",
            };

            // Assign players 1 and 3 to Team B
            TeamB = new NetworkTeamData()
            {
                player0Index = 1,
                player1Index = 3,
                score = 0,
                teamName = "Team B",
            };
        }
        
        
        public void SetUpDealer()
        {
            foreach (var playerBase in Players)
            {
                playerBase.SetHimDealer(false);
            }
            GetDealerPlayer().SetHimDealer(true);
        }
        
        public List<PlayerBase> GetPlayers() => Players;
        public PlayerBase GetPlayer(int index) => Players[index];
        public PlayerBase GetDealerPlayer() => Players[DealerIndex];
        public PlayerBase GetLeadPlayerToPlay() => Players[(DealerIndex + 1) % 4];
        public PlayerBase GetNextPlayerToPlay(PlayerBase currentPlayer)
        {
            int currentIndex = Players.IndexOf(currentPlayer);
            if (currentIndex == -1)
            {
                Debug.LogError("Current player not found in player list.");
                return null;
            }

            int nextIndex = (currentIndex + 1) % Players.Count;
            return Players[nextIndex];
        }
        public PlayerBase GetOppositePlayerOfTeam(PlayerBase trumpCaller)
        {
            var index = Players.IndexOf(trumpCaller);
            if (index == -1)
            {
                Debug.LogError("Trump caller not found in player list.");
                return null;
            }

            var oppositeIndex = (index + 2) % 4;
            return Players[oppositeIndex];
        }
        public int TeamAScore() => TeamA.score;
        public int TeamBScore() => TeamB.score;

        public void Reset()
        {
            foreach (var player in Players)
            {
                player.handData = null;
            }
        }
        public PlayerBase GetLocalPlayerBase()
        {
            var index = MultiplayerManager.Instance.LocalPlayerIndex;
            if (index == -1)
            {
                Debug.LogError($"local player not found in player list. {index}");
            }
            return Players[index];
        }
        public PlayerRef GetLocalPlayerRef()
        {
            return Runner.LocalPlayer;
        }

        public PlayerRef GetPlayerRef(PlayerBase player)
        {
            return _playerRefList[Players.IndexOf(player)];
        }

        public PlayerBase GetPlayerBase(PlayerRef player)
        {
            return Players[_playerRefList.IndexOf(player)];
        }

        public void UpdateTrickCount(PlayerBase player)
        {
            if (player.PlayerIndex == TeamA.player0Index || player.PlayerIndex == TeamA.player1Index)
            {
                var teamATricks = GetPlayer(TeamA.player0Index).TricksWon +
                                  GetPlayer(TeamA.player1Index).TricksWon;
                _gamePlayScreen.UpdateCurrentTricksCountForTeamA(teamATricks);
            }
            else if (player.PlayerIndex == TeamB.player0Index || player.PlayerIndex == TeamB.player1Index)
            {
                var teamBTricks = GetPlayer(TeamB.player0Index).TricksWon +
                                  GetPlayer(TeamB.player1Index).TricksWon;
                _gamePlayScreen.UpdateCurrentTricksCountForTeamB(teamBTricks);
            }
        }
    }

}
