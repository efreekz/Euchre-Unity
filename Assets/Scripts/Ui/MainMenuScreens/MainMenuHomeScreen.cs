using System;
using Helper;
using Managers;
using Network;
using TMPro;
using UIArchitecture;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.MainMenuScreens
{
    public class MainMenuHomeScreen : FullScreenView
    {
        public Button accountsButton;
        public Button settingsButton;
        
        public Button playOnlineButton10;
        public Button playOnlineButton20;
        public Button playOnlineButton40;
        // public Button playWithBots;
        public TMP_Text freekzText;
        public Button transcetionsPanelButton;
        
        public Button creatPrivateRoomButton;
        public Button joinPrivateRoomButton;
        public TMP_InputField privateRoomCodeInput;
        
        protected override void Initialize(Object obj)
        {
            playOnlineButton10.onClick.AddListener(() => OnClickPlayOnlineButton(10));
            playOnlineButton20.onClick.AddListener(() => OnClickPlayOnlineButton(20));
            playOnlineButton40.onClick.AddListener(() => OnClickPlayOnlineButton(40));
            accountsButton.onClick.AddListener(OnClickAccountsButton);
            creatPrivateRoomButton.onClick.AddListener(OnClickCreatRoomButton);
            joinPrivateRoomButton.onClick.AddListener(OnClickJoinRoomButton);
            transcetionsPanelButton.onClick.AddListener(OnClickTranscetionsPanelButton);
            // playWithBots?.onClick.AddListener(OnClickPlayWithBotsButton);
            
            CurrencyManager.UpdateFreekz += OnUpdateFreekzText;
            
            OnUpdateFreekzText(CurrencyManager.Freekz);
        }

        protected override void Cleanup()
        {
            playOnlineButton10.onClick.RemoveAllListeners();
            playOnlineButton20.onClick.RemoveAllListeners();
            playOnlineButton40.onClick.RemoveAllListeners();
            accountsButton.onClick.RemoveListener(OnClickAccountsButton);
            creatPrivateRoomButton.onClick.RemoveListener(OnClickCreatRoomButton);
            joinPrivateRoomButton.onClick.RemoveListener(OnClickJoinRoomButton);
            transcetionsPanelButton.onClick.RemoveListener(OnClickTranscetionsPanelButton);
            
            CurrencyManager.UpdateFreekz -= OnUpdateFreekzText;

        }
        
        private void OnClickTranscetionsPanelButton()
        {
            UiManager.Instance.ShowPanel(UiScreenName.TranscetionsScreen, null);
        }
        
        private void OnClickAccountsButton()
        {
            UiManager.Instance.ShowPanel(UiScreenName.AccountDetails, null);
        }
        
        private async void OnClickPlayOnlineButton(int freekz)
        {
            var result = await CurrencyManager.SubtractFreekz(freekz, "Game Played", "You played a game");
            if (result)
            {
                GameManager.CurrentGameMode = GameMode.OnlineMultiplayer;
                GameManager.Fee = freekz;
                UiManager.Instance.ShowPanel(UiScreenName.MatchMakingPanel, new MatchMakingPanelData() { IsPrivate = false, Fee = freekz});
            }
            else
            {
                UiManager.Instance.ShowToast("You Can not start Game");
            }
        }

        private void OnUpdateFreekzText(float freekz)
        {
            freekzText.text = freekz.CurrencyFormat();
        }

        private void OnClickJoinRoomButton()
        {
            GameManager.CurrentGameMode = GameMode.OnlineMultiplayer;
            if (string.IsNullOrEmpty(privateRoomCodeInput.text))
            {
                UiManager.Instance.ShowToast("Please enter room code.");
                return;
            }
            
            UiManager.Instance.ShowPanel(UiScreenName.MatchMakingPanel,
                new MatchMakingPanelData() { IsPrivate = true, RoomName = privateRoomCodeInput.text.Trim() });
        }

        private void OnClickCreatRoomButton()
        {
            GameManager.CurrentGameMode = GameMode.OnlineMultiplayer;
            UiManager.Instance.ShowPanel(UiScreenName.MatchMakingPanel,
                new MatchMakingPanelData() { IsPrivate = true, RoomName = "" });
        }

    }
}
