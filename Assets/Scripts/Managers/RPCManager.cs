using System;
using System.Collections.Generic;
using Controllers;
using Fusion;
using UnityEngine;

namespace Managers
{
    public class RPCManager : NetworkBehaviour
    {
        [Networked, Capacity(4)]
        public NetworkArray<PlayerGameData> JoinedPlayers => default;

        public static RPCManager Instance;
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        #region Seat
        
        public int GetFilledCount() {
            int c = 0;
            for (int i = 0; i < JoinedPlayers.Length; i++) {
                if (JoinedPlayers[i].Occupied) c++;
            }
            return c;
        }
        
        private int FindSeatForHumanJoin() {
            for (int i = 0; i < JoinedPlayers.Length; i++) {
                if (!JoinedPlayers[i].Occupied) return i;
            }
            for (int i = 0; i < JoinedPlayers.Length; i++) {
                if (JoinedPlayers[i].Occupied && JoinedPlayers[i].IsBot) return i;
            }
            return -1;
        }
        
        public void AssignHumanToSeat(PlayerRef player) {
            int seat = FindSeatForHumanJoin();
            if (seat < 0) {
                Runner.Disconnect(player);
                return;
            }

            // If replacing a bot, despawn it here (if you actually spawned bot objects)
            if (JoinedPlayers[seat].Occupied && JoinedPlayers[seat].IsBot) {
                // Despawn bot entity here if needed
            }

            var playerData = JoinedPlayers[seat];
            playerData.Occupied = true;
            playerData.IsBot = false;
            playerData.PlayerId = seat;
            playerData.PlayerRef = player;
            JoinedPlayers.Set(seat, playerData);
            RPC_AddRealPlayer(playerData);
            Debug.Log($"Player with Id {player.PlayerId} added to list");
        }

        public void SpawnBotAtSeat(int seatIndex) 
        {
            if (seatIndex < 0 || seatIndex >= JoinedPlayers.Length) return;
            var botData = JoinedPlayers[seatIndex];
            botData.Occupied = true;
            botData.IsBot = true;
            botData.PlayerId = seatIndex;
            botData.PlayerRef = MultiplayerManager.Instance.LocalPlayerRef;
            JoinedPlayers.Set(seatIndex, botData);
            RPC_AddBotPlayer(botData);
            Debug.Log($"🤖 Bot {seatIndex + 1} joined with simulated PlayerRef {botData.PlayerId}");
        }

        public void ClearSeat(int seatIndex) {
            if (seatIndex < 0 || seatIndex >= JoinedPlayers.Length) return;
            var d = JoinedPlayers[seatIndex];
            d.Occupied = false;
            d.IsBot = false;
            d.PlayerRef = default;
            JoinedPlayers.Set(seatIndex, d);
        }

        #endregion
        
        #region Main Callbacks

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_AddBotPlayer(PlayerGameData playerData)
        {
            Debug.Log($"Add Bot Player : {playerData.PlayerRef.PlayerId} and {playerData.PlayerId}");
            MainMenuSceneController.Instance.OnPlayerJoined(JoinedPlayers);
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_AddRealPlayer(PlayerGameData playerData)
        {
            Debug.Log($"Added Real Player : {playerData.PlayerRef.PlayerId} and {playerData.PlayerId}");
            MainMenuSceneController.Instance.OnPlayerJoined(JoinedPlayers);
        }

        #endregion
    }
}