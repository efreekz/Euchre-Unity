using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using MainMenu;
using Managers;
using TMPro;
using UIArchitecture;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.MainMenuScreens
{
    public class MatchMakingPanel : FullScreenView
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private Button copyCode;

        [SerializeField] private MatchMakingPlayerUi[] playersUI;

        [SerializeField] private MultiplayerManager multiplayerManagerPrefab;
        
        private CancellationTokenSource _timeoutCts;
        private MatchMakingPanelData _matchMakingPanelData;

        protected async override void Initialize(Object obj)
        {
            if (obj is MatchMakingPanelData matchMakingPanelData)
            {
                copyCode.gameObject.SetActive(false);
                _matchMakingPanelData = matchMakingPanelData;

                if (_matchMakingPanelData.IsPrivate)
                {
                    await StartPrivateGame();
                }
                else
                {
                    await StartPublicGame(_matchMakingPanelData.Fee);
                }
                
            }
            else
            {
                GameLogger.ShowLog("Invalid Data");
            }
        }

        protected override void Cleanup()
        {
            statusText.text = "";
            roomNameText.text = "";
            copyCode.onClick.RemoveAllListeners();

            _timeoutCts?.Cancel();
            _timeoutCts?.Dispose();
            _timeoutCts = null;
        }

        private async UniTask StartPrivateGame()
        {
            var roomName = _matchMakingPanelData.RoomName;
            var shouldBeHost = string.IsNullOrEmpty(roomName);
            statusText.text = shouldBeHost ? $"Creating Room {roomName}" : $"Joining Room : {roomName}";
            
            Instantiate(multiplayerManagerPrefab);

            await UniTask.WaitForEndOfFrame();

            var joined = await MultiplayerManager.Instance.StartPrivateGame(roomName);

            if (!joined.Item1)
            {
                statusText.text = $"Failed to join {roomName}";
                await MultiplayerManager.Instance.ShutDown();
                UiManager.Instance.HidePanel(this);
                return;
            }

            statusText.text = "Waiting For Players";
            roomNameText.text = $"Room Code : {joined.Item2}";
            copyCode.gameObject.SetActive(true);
            copyCode.onClick.AddListener(() => OnCopyCodeClick(joined.Item2));
            for (var index = 0; index < playersUI.Length; index++)
            {
                var playerUi = playersUI[index];
                playerUi.Initialize(shouldBeHost, index);
            }
        }

        public void OnCopyCodeClick(string roomCode)
        {
            GUIUtility.systemCopyBuffer = roomCode; // Works for standalone/editor
        }

        private async UniTask StartPublicGame(int fee)
        {
            statusText.text = "Finding Room";
            
            Instantiate(multiplayerManagerPrefab);

            await UniTask.WaitForEndOfFrame();

            var joined = await MultiplayerManager.Instance.StartPublicGame(fee);

            if (!joined)
            {
                statusText.text = "Failed to join any session";
                if (GameManager.Fee != null)
                {
                    UiManager.Instance.ShowToast("Failed to join any session, Collecting Refund");
                    await CurrencyManager.AddFreekz(GameManager.Fee.Value, "Game Joining Failed", "You have got a refund");
                    GameManager.Fee = null;
                }
                else
                {
                    UiManager.Instance.ShowToast("Failed to join any session");
                }
                await MultiplayerManager.Instance.ShutDown();
                UiManager.Instance.HidePanel(this);
                return;
            }

            statusText.text = "Waiting For Players";
            for (var index = 0; index < playersUI.Length; index++)
            {
                var playerUi = playersUI[index];
                playerUi.Initialize(false, index);
            }
        }

        public async void OnPlayerJoined(NetworkArray<PlayerGameData> players)
        {
            await UniTask.WaitUntil(() => MultiplayerManager.Instance.LocalPlayerIndex >= 0);

            for (int i = 0; i < players.Length; i++)
            {
                if (!players[i].Occupied) continue;
                
                GameLogger.LogNetwork($"playersList[i].PlayerId : {players[i].PlayerId} \n MultiplayerManager.Instance.LocalPlayerIndex : {MultiplayerManager.Instance.LocalPlayerIndex}");
                var playerName = players[i].IsBot
                    ? $"Bot {players[i].PlayerId + 1}"
                    : $"Player {players[i].PlayerId + 1}";
                var displayIndex = (players[i].PlayerId - MultiplayerManager.Instance.LocalPlayerIndex + 4) % 4;
                playersUI[displayIndex].SetData(playerName);
            }
        }
    }

    public class MatchMakingPanelData
    {
        // Private room Data
        public string RoomName = "";
        public bool IsPrivate = false;
        
        // public room Data
        public int Fee;
    }

}