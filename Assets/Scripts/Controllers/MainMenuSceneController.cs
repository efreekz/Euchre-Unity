using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using Fusion;
using Managers;
using Network;
using Ui.MainMenuScreens;
using UIArchitecture;
using UnityEngine;

namespace Controllers
{
    public class MainMenuSceneController : MonoBehaviour
    {
        public static MainMenuSceneController Instance;
        
        private Views _waitingPanel;
        
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            UiManager.Instance.LoadSceneUi(SceneName.MainMenu);

            GameManager.CheckForWinScreen();
        }
        
        public void OnPlayerJoined(NetworkArray<PlayerGameData> player)
        {
            var waitingPanel = UiManager.Instance.GetUiView(UiScreenName.MatchMakingPanel) as MatchMakingPanel;
            waitingPanel?.OnPlayerJoined(player);
        }
    }
}