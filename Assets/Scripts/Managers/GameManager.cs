using System;
using System.Threading;
using Controllers;
using Cysharp.Threading.Tasks;
using Data;
using GamePlay.Cards;
using JetBrains.Annotations;
using Network;
using UIArchitecture;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Managers
{
    public enum GameMode
    {
        SinglePlayerVsBots,
        OnlineMultiplayer
    }
    
    public static class GameManager
    {
        public static GameMode CurrentGameMode {get; set;}

        public static int? Fee;
        [CanBeNull] public static GameResult GamesResult;

        public static UserData UserData { get; set; }
        public static string PromoCode { get; set; }

        public static void LoadScene(SceneName sceneName)
        {
            if (Enum.TryParse<SceneName>(SceneManager.GetActiveScene().name, out var scene))
            {
                if (scene == sceneName)
                    return;
            }
            SceneManager.LoadScene(sceneName.ToString());
        }
        
        public static void EndGameResult(NetworkTeamData playerManagerTeamA, NetworkTeamData playerManagerTeamB, int playerIndex)
        {
            NetworkTeamData winningTeam;
            var isDraw = false;

            if (playerManagerTeamA.score > playerManagerTeamB.score)
            {
                winningTeam = playerManagerTeamA;
            }
            else if (playerManagerTeamB.score > playerManagerTeamA.score)
            {
                winningTeam = playerManagerTeamB;
            }
            else // Draw
            {
                isDraw = true;
                winningTeam = playerManagerTeamA;
            }

            var isLocalWinner = false;
            if (!isDraw)
            {
                if (winningTeam.player0Index == playerIndex || winningTeam.player1Index == playerIndex)
                {
                    isLocalWinner = true;
                }
            }
            var fee = Fee ?? 0;

            var reward = 0;
            if (isDraw)
            {
                reward = fee;
            }
            else if (isLocalWinner)
            {
                reward = fee * 2;
            }

            // Save result
            GamesResult = new GameResult
            {
                IsLocalPlayerWinner = isLocalWinner,
                Reward = reward,
                IsDraw = isDraw
            };
        }

        public static void CheckForWinScreen()
        {
            if (GamesResult == null)
                return;

            UiManager.Instance.ShowPanel(UiScreenName.ResultPanel, GamesResult);

            GamesResult = null;
        }

        public static async UniTask RefreshPlayerData()
        {
            if (AuthManager.Instance && AuthManager.Instance.IsLoggedIn())
            {
                var waitingPanel = UiManager.Instance.ShowPanel(UiScreenName.WaitingPanel, null);
                GameLogger.LogNetwork("Trying to login");
                await AuthManager.Instance.FetchUserData(GameManager.OnSucessfullLogin, GameManager.OnLoginFailed);
                
                UiManager.Instance.HidePanel(waitingPanel);
            }
        }
        
        public static async UniTask CheckForAutomaticLogin()
        {
            if (AuthManager.Instance && AuthManager.Instance.IsLoggedIn() && UserData == null)
            {
                var waitingPanel = UiManager.Instance.ShowPanel(UiScreenName.WaitingPanel, null);
                GameLogger.LogNetwork("Trying to login");
                await AuthManager.Instance.FetchUserData(OnSucessfullLogin, OnLoginFailed);
                
                UiManager.Instance.HidePanel(waitingPanel);
            }
        }

        
        public static void OnSucessfullLogin(LoginResponse response)
        {
            GameLogger.LogNetwork("Login successful!");
            
            UserData = response.user;
            PromoCode = response.promo_code;
            CurrencyManager.Freekz = response.balance;
            
            LoadScene(SceneName.MainMenu);
        }

        public static void OnLoginFailed(string error)
        {
            LoadScene(SceneName.Login);
            UiManager.Instance.ShowToast(error);
        }
    }
}