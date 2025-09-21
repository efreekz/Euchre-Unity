using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Controllers;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Test;
using UIArchitecture;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using static System.String;
using GameMode = Fusion.GameMode;
using Random = UnityEngine.Random;

namespace Managers
{
    public struct PlayerGameData : INetworkStruct, IEquatable<PlayerGameData>
    {
        public int PlayerId;
        public PlayerRef PlayerRef;
        public NetworkBool IsBot;
        public NetworkBool Occupied;

        public bool Equals(PlayerGameData other)
        {
            return PlayerId == other.PlayerId && PlayerRef.Equals(other.PlayerRef) && IsBot.Equals(other.IsBot);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerGameData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlayerId, PlayerRef, IsBot);
        }
    }
    
    public class MultiplayerManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static MultiplayerManager Instance;

        public NetworkRunner Runner { get; private set; }
        private NetworkSceneManagerDefault SceneManager { get; set; }

        
        private List<SessionInfo> _sessionList = new();
        private bool _sessionListReceived = false;
        
        private int _localPlayerIndex = -1;
        private PlayerRef _localPlayerRef;
        public int LocalPlayerIndex => _localPlayerIndex;
        public PlayerRef LocalPlayerRef => _localPlayerRef;
        
        private CancellationTokenSource _autoStartCts;
        private CancellationTokenSource _botSpawningCts;
        private CancellationTokenSource _genericCts;
        public float timeToAutomaticStartGame = 10f;
        public float timeToWaitForPlayersInQueue = 5f;
        private const int MaxPlayerCount = 4;

        public RPCManager rpcManagerPrefab;
        private bool _isPrivateGame;
        
        [Header("Testing")]
        [SerializeField] private bool immediateBotSpawning;
        public RPCManager RPCManager { get; set; }

        // public TestPlayer PlayerPrefab;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _genericCts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _autoStartCts?.Cancel();
            _autoStartCts?.Dispose();
            _botSpawningCts?.Cancel();
            _botSpawningCts?.Dispose();
            _genericCts?.Cancel();
            _genericCts?.Dispose();
        }

        public async UniTask<bool> StartPublicGame(int fee)
        {
            if (Runner != null)
            {
                Debug.LogError("Runner already exists.");
                return false;
            }

            Runner = gameObject.AddComponent<NetworkRunner>();
            Runner.ProvideInput = true;
            Runner.AddCallbacks(this);

            _isPrivateGame = false;

            var scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

            // Join default lobby to receive session list
            var joinLobbyResult = await Runner.JoinSessionLobby(SessionLobby.ClientServer);
            if (!joinLobbyResult.Ok)
            {
                Debug.LogError($"Failed to join session lobby. Reason: {joinLobbyResult.ErrorMessage}");
                return false;
            }

            Debug.Log("Updating session list");

            // Wait until OnSessionListUpdated is called
            float timeout = 5f;
            while (!_sessionListReceived && timeout > 0)
            {
                await UniTask.Delay(100);
                timeout -= 0.1f;
            }

            Debug.Log("Session list Updated");

            // Reset flag for future use
            _sessionListReceived = false;

            // Try to find a session with same fee and < 4 players
            SessionInfo availableSession = null;
            foreach (var session in _sessionList.Where(session => session.PlayerCount < 4 && session.IsOpen))
            {
                if (!session.Properties.TryGetValue("fee", out var feeObj) || !feeObj.IsInt ||
                    (int)feeObj.PropertyValue != fee) continue;
                availableSession = session;
                break;
            }

            StartGameResult result;

            if (availableSession != null)
            {
                Debug.Log($"Joining session: {availableSession.Name} with fee {fee}");
                result = await Runner.StartGame(new StartGameArgs
                {
                    GameMode = Fusion.GameMode.Client,
                    SessionName = availableSession.Name,
                    SceneManager = SceneManager
                });
            }
            else
            {
                string newSessionName = $"{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                Debug.Log($"Creating new session: {newSessionName} with fee {fee}");

                var properties = new Dictionary<string, SessionProperty>()
                {
                    { "fee", fee }
                };

                result = await Runner.StartGame(new StartGameArgs
                {
                    GameMode = Fusion.GameMode.Host,
                    SessionName = newSessionName,
                    Scene = scene,
                    SceneManager = SceneManager,
                    SessionProperties = properties
                });
            }

            if (result.Ok)
            {
                Debug.Log("Game started successfully.");
                ActuallyStartGame().Forget();
                return true;
            }
            else
            {
                Debug.LogError($"StartGame failed. Reason: {result.ShutdownReason}, Error: {result.ErrorMessage}");
                UiManager.Instance.ShowToast(result.ErrorMessage);
                return false;
            }
        }


        public async UniTask<(bool, string)> StartPrivateGame(string sessionName = "")
        {
            if (Runner != null)
            {
                Debug.LogError("Runner already exists.");
                return (false, null);
            }
            
            _isPrivateGame = true; // ✅ Mark as private game
            
            Runner = gameObject.AddComponent<NetworkRunner>();
            Runner.ProvideInput = true;
            Runner.AddCallbacks(this);
            
            var scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            
            var isHost = string.IsNullOrEmpty(sessionName);
            if (isHost)
                sessionName = Guid.NewGuid().ToString("N").Substring(0, 6);

            var startGameArgs = new StartGameArgs()
            {
                GameMode = isHost ? Fusion.GameMode.Host : Fusion.GameMode.Client,
                SessionName = sessionName,
                Scene = scene,
                SceneManager = SceneManager,
                // ✅ Important: don't advertise in public lobby
                IsVisible = false,  

                // ✅ (Optional) Tag it as private for extra safety
                CustomLobbyName = null,
                PlayerCount = 4
            };

            var result = await Runner.StartGame(startGameArgs);

            if (result.Ok)
            {
                Debug.Log(isHost ? $"Created room: {sessionName}" : $"Joined room: {sessionName}");
                ActuallyStartGame().Forget();
                return (true, sessionName);
            }
            else
            {
                Debug.LogError($"Failed to start/join room: {result.ShutdownReason}");
                UiManager.Instance.ShowToast(result.ErrorMessage);
                return (false, null);
            }
        }

        public async UniTask ShutDown()
        {
            Debug.Log("🔻 Shutting down MultiplayerManager...");

            try
            {
                // --- Runner cleanup ---
                if (Runner != null)
                {
                    // Remove callbacks first to avoid dangling events
                    Runner.RemoveCallbacks(this);

                    // Shut down the Fusion runner safely
                    try
                    {
                        if (!Runner.IsShutdown)
                        {
                            await Runner.Shutdown();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Error shutting down Runner: {ex.Message}");
                    }

                    // Destroy the runner GameObject
                    Destroy(Runner);
                    Runner = null;
                }

                // --- Scene Manager cleanup ---
                if (SceneManager != null)
                {
                    Destroy(SceneManager);
                    SceneManager = null;
                }

                // --- (Optional) RPC Manager cleanup ---
                // If you keep a persistent RPCManager, despawn & clear players here.
                if (RPCManager != null && RPCManager.TryGetComponent(out NetworkObject netObj))
                {
                    if (Runner != null && Runner.IsServer)
                    {
                        Runner.Despawn(netObj);
                    }
                    Destroy(RPCManager.gameObject);
                    RPCManager = null;
                }

                // --- Session cleanup ---
                _sessionList.Clear();
                _sessionListReceived = false;

                if (gameObject == null)
                {
                    Destroy(gameObject);
                }
                
                Debug.Log("✅ MultiplayerManager shutdown completed.");
            }
            catch (Exception e)
            {
                Debug.LogError($"⚠️ MultiplayerManager shutdown encountered an error: {e.Message}");
            }
        }


        private async UniTask StartGameAutomatically()
        {
            // Cancel and reset any previous timer
            _autoStartCts?.Cancel();
            _autoStartCts?.Dispose();
            _autoStartCts = new CancellationTokenSource();

            if (Runner.SessionInfo.IsVisible)
            {
                try
                {
                    Debug.Log($"⏳ Waiting {timeToAutomaticStartGame} seconds for more players to join...");
                    await UniTask.Delay(TimeSpan.FromSeconds(timeToAutomaticStartGame), cancellationToken: _autoStartCts.Token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("🔁 New player joined — auto-start timer reset.");
                    return;
                }

                Debug.Log("⏱️ Initial wait over. Locking session and checking for pending joins...");
                
                Runner.SessionInfo.IsVisible = false; // Optional: hide room from list
            }

            // ⚠️ Wait a few more seconds to allow pending players to fully connect (e.g., WebGL/slow connections)
            try
            {
                Debug.Log($"⏳ Waiting {timeToWaitForPlayersInQueue} seconds for queue players to join...");
                await UniTask.Delay(TimeSpan.FromSeconds(timeToWaitForPlayersInQueue), cancellationToken: _autoStartCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("🔁 New player joined — auto-start timer reset.");
                return;
            }
            
            Runner.SessionInfo.IsOpen = false;  // Disallow new join attempts

            var totalPlayers = RPCManager.GetFilledCount();

            if (totalPlayers < 4)
            {
                var botsToSpawn = 4 - totalPlayers;
                await SpawnBots(botsToSpawn);
            }
        }

        private async UniTask SpawnBots(int botCount)
        {
            Debug.Log($"[Server] Spawning {botCount} bots...");
            _botSpawningCts?.Cancel();
            _botSpawningCts?.Dispose();
            _botSpawningCts = new CancellationTokenSource();

            int timeSpan;
            for (var i = 0; i < botCount; i++)
            {
                
                if (!immediateBotSpawning)
                {
                    timeSpan = Random.Range(2000, 4000);
                    await UniTask.Delay(timeSpan,  cancellationToken: _botSpawningCts.Token);
                }
                
                var currentIndex = RPCManager.GetFilledCount();
                RPCManager.SpawnBotAtSeat(currentIndex);

            }
            
            timeSpan = Random.Range(2000, 4000);
            await UniTask.Delay(timeSpan,  cancellationToken: _botSpawningCts.Token);
        }

        private async UniTask ActuallyStartGame()
        {
            await UniTask.WaitUntil(() =>
            {
                RPCManager = FindFirstObjectByType<RPCManager>();
                return RPCManager;
            });
            
            Debug.Log("RpcManager has been Spawned");
            
            await UniTask.WaitUntil(() => RPCManager.GetFilledCount() == MaxPlayerCount);
            
            if (RPCManager.GetFilledCount() == MaxPlayerCount)
            {
                _autoStartCts?.Cancel();
                _autoStartCts?.Dispose();
                _autoStartCts = null;
                await UniTask.Delay(3000,  cancellationToken: _genericCts.Token);
                GameManager.LoadScene(SceneName.GamePlay);
            }
            else
            {
                Debug.LogError($"Invalid Player Count {RPCManager.GetFilledCount()}");
            }
        }

        public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player joined : {player.PlayerId}");
            if (runner.IsServer)
            {
                RPCManager ??= runner.Spawn(rpcManagerPrefab, Vector3.zero, Quaternion.identity, player);
                
                RPCManager.AssignHumanToSeat(player);

                if (!_isPrivateGame)
                {
                    _ = StartGameAutomatically();
                }
            }
            
            if (player == runner.LocalPlayer)
            {
                await UniTask.WaitUntil(() => FindAnyObjectByType<RPCManager>() != null);
                RPCManager = FindAnyObjectByType<RPCManager>();
                Debug.Log("RPCManager found and initialized!");
                
                await UniTask.WaitUntil(() => RPCManager.JoinedPlayers.Any(p => p.PlayerRef == player));
                var myData = RPCManager.JoinedPlayers.First(p => p.PlayerRef == player);
            
                Debug.Log($"Setting Up Local Player at index {player.PlayerId - 1}");
                _localPlayerRef = player;
                _localPlayerIndex = myData.PlayerId;
            }
        }


        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player Left : {player.PlayerId}");
        }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.LogWarning($"Runner shutdown. Reason: {shutdownReason}");
            _ = ShutDown();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"Disconnected from server. Reason: {reason}");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _sessionList = sessionList;
            _sessionListReceived = true;
        }
        
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){ }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){ }
    }
}
