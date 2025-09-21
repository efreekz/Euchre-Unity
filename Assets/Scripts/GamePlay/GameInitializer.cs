using System;
using Controllers;
using Cysharp.Threading.Tasks;
using Managers;
using UIArchitecture;
using UnityEngine;
using GameMode = Managers.GameMode;

namespace GamePlay
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GamePlayController gameplayControllerPrefab;
        [SerializeField] private GamePlayControllerNetworked gameplayControllerNetworkedPrefab;

        private GamePlayControllerNetworked _gameplayControllerNetworkedInstance;

        private async void Awake()
        {
            UiManager.Instance.LoadSceneUi(SceneName.GamePlay);

            switch (GameManager.CurrentGameMode)
            {
                case GameMode.SinglePlayerVsBots:
                    var gamePlayController = Instantiate(gameplayControllerPrefab);
                    await gamePlayController.Initialize();
                    UiManager.Instance.ShowPanel(UiScreenName.GamePlayScreens, null);
                    break;

                case GameMode.OnlineMultiplayer:
                {
                    var runner = MultiplayerManager.Instance.Runner;

                    if (runner == null)
                    {
                        Debug.LogError("Runner not available for spawning!");
                        return;
                    }

                    if (runner.IsServer)
                    {
                        _gameplayControllerNetworkedInstance = runner.Spawn(
                            gameplayControllerNetworkedPrefab,
                            Vector3.zero,
                            Quaternion.identity
                        );
                    }
                    // Wait until the gameplay controller is replicated to this client
                    await UniTask.WaitUntil(() =>
                    {
                        if (_gameplayControllerNetworkedInstance != null) 
                            return true;

                        var found = FindFirstObjectByType<GamePlayControllerNetworked>();
                        
                        if (found == null) return false;
                        
                        _gameplayControllerNetworkedInstance = found;
                        return true;

                    });

                    await _gameplayControllerNetworkedInstance.Initialize();
                    
                    _gameplayControllerNetworkedInstance.StartGame().Forget();

                    // Only show the panel once controller is confirmed available
                    UiManager.Instance.ShowPanel(UiScreenName.GamePlayScreens, null);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}