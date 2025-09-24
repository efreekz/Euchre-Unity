using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using GamePlay.Cards;
using GamePlay.Interfaces;
using GamePlay.Player;
using Managers;
using Newtonsoft.Json;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Controllers
{
    public class GamePlayControllerNetworked : NetworkBehaviour, IGameController
    {
        [SerializeField] private float timeToPlayTrun = 10f;
        public PlayerManagerNetworked playerManager;
        public CardsController cardsController;
        public float cardDistributionAnimationTime = 0.2f;
        
        private GamePlayScreen _gamePlayScreen;
        public List<Card> kitty = new();
        private CardData _topKittyCardData;
        private TrumpSelectionData _trumpSelectionData;
        public Dictionary<int, Card> currentTrickCards = new Dictionary<int, Card>();
        public Dictionary<CardData, int> allPlayedCards = new Dictionary<CardData, int>();
        
        public List<CardData> GetCardsPlayedByPlayer(int index) => allPlayedCards.Where(kv => kv.Value == index).Select(kv => kv.Key).ToList();

        public Card TopKittyCard { get; private set; }
        public Suit TrumpSuit
        {
            get
            {
                if (_trumpSelectionData is not null)
                    return _trumpSelectionData.Suit;
                
                GameLogger.ShowLog($"No trump selection data available.", GameLogger.LogType.Error);
                return Suit.None;
            }
            set
            {
                if (_trumpSelectionData == null)
                    return;
                
                _trumpSelectionData.Suit = value;
            }
        }
        public UniTaskCompletionSource<TrumpSelectionData> TrumpAcceptedTcs { get; private set; }= new();
        public UniTaskCompletionSource<CardData> CardPlayedTcs { get; private set; }= new();
        public UniTaskCompletionSource CardExchangedTcs { get; private set; }= new();
        private UniTaskCompletionSource ScoreUpdate { get; set; }= new();
        
        [Networked] public Suit CurrentTrickSuit { get; set; }
        [Networked, Capacity(4)] private NetworkArray<NetworkBool> PlayersInitialized => default;

        public static CancellationTokenSource CancellationTokenSource;
        public static GamePlayControllerNetworked Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            CancellationTokenSource = new CancellationTokenSource();
        }

        public async UniTask Initialize()
        {

            _gamePlayScreen = UiManager.Instance.GetUiView(UiScreenName.GamePlayScreens) as GamePlayScreen;
            cardsController.Initialize(_gamePlayScreen);
            await playerManager.Initialize(_gamePlayScreen);
            
            var localPlayer = playerManager.GetLocalPlayerBase() as OnlinePlayer;
            
            if (localPlayer != null)
            {
                await UniTask.WaitUntil(() =>
                {
                    if (localPlayer.HasInputAuthority)
                    {
                        localPlayer.RPC_NotifyInitialized();
                        GameLogger.LogNetwork($"PLAYER {localPlayer.PlayerIndex} INITIALIZED");
                        return true;
                    }

                    GameLogger.LogNetwork($"PLAYER {localPlayer.PlayerIndex} NOT INITIALIZED");
                    return false;
                });
            }
            else
            {
                GameLogger.LogNetwork("No local player found", GameLogger.LogType.Error);
            }

            // Server will Initialize all the bot players
            if (HasStateAuthority)
            {
                var allPlayers = playerManager.GetPlayers();

                foreach (var botPlayer in allPlayers.OfType<OnlineBot>())
                {
                    await UniTask.WaitUntil(() =>
                    {
                        if (botPlayer.HasInputAuthority)
                        {
                            botPlayer.RPC_NotifyInitialized();
                            GameLogger.LogNetwork($"BOT {botPlayer.PlayerIndex} INITIALIZED");
                            return true;
                        }

                        GameLogger.LogNetwork($"BOT {botPlayer.PlayerIndex} NOT INITIALIZED");
                        return false;
                    });
                }
            }

            await UniTask.WaitUntil(() =>
            {
                GameLogger.LogNetwork("Waiting to Sync Players");
                for (var i = 0; i < 4; i++)
                {
                    if (!PlayersInitialized.Get(i)) return false;
                }
                return true;
            });
        }

        public async UniTask StartGame()
        {
            await StartGameSequence();
            
            GameManager.LoadScene(SceneName.MainMenu);
            MultiplayerManager.Instance.ShutDown().Forget();
            
        }

        private void OnDestroy()
        {
            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();
        }
        private async UniTask StartGameSequence()
        {
            while (playerManager.TeamAScore() < 10 && playerManager.TeamBScore() < 10)
            {
                await SetupDealer();
                await DistributeCardsAnimated();
                await SetupTrumpCard();
                await StartNormalGame();
                await ShowResults();

                await ClearBoard();
            }
            
            GameManager.EndGameResult(playerManager.TeamA, playerManager.TeamB, playerManager.GetLocalPlayerBase().PlayerIndex);
        }

        #region Setup Dealer

        private async UniTask SetupDealer()
        {
            if (Runner.IsServer || HasStateAuthority)
            {
                // Rotate dealer clockwise each hand (or random for first hand)
                if (playerManager.DealerIndex == -1) // First hand
                {
                    playerManager.DealerIndex = Random.Range(0, 4);
                }
                else // Subsequent hands - rotate clockwise
                {
                    playerManager.DealerIndex = (playerManager.DealerIndex + 1) % 4;
                }
                RPC_AssignDealerNotification(playerManager.DealerIndex);
            }

            await UniTask.CompletedTask;
        }

        #endregion

        #region Distribute Cards

        private async UniTask DistributeCardsAnimated()
        {
            var localPLayer = playerManager.GetLocalPlayerBase();
            
            var players = playerManager.GetPlayers();
            var shuffledDeck = cardsController.GetShuffledDeck();
            
            if (Runner.IsServer || HasStateAuthority)
            {
                const int cardsPerPlayer = 5;

                // Deal cards starting from player to left of dealer (proper Euchre rotation)
                int startPlayerIndex = (playerManager.DealerIndex + 1) % 4;

                for (int i = 0; i < players.Count; i++)
                {
                    int playerIndex = (startPlayerIndex + i) % 4; // Rotate properly from left of dealer
                    var player = players[playerIndex];
                    var playerCards = new List<CardData>();

                    for (int j = 0; j < cardsPerPlayer; j++)
                    {
                        int cardIndex = i + j * players.Count; // Card distribution index
                        if (shuffledDeck[cardIndex].cardData is null)
                        {
                            GameLogger.ShowLog($"Card at index {cardIndex} is null");
                        }
                        playerCards.Add(shuffledDeck[cardIndex].cardData);
                    }

                    if (player.IsBot)
                    {
                        player.handData = playerCards;
                    }
                    else
                    {
                        var serialized = JsonConvert.SerializeObject(playerCards.Select(c => 
                            new CardDataDto()
                            {
                                suit = c.suit,
                                rank = c.rank,
                            }).ToList());
                        GameLogger.ShowLog($"{player.PlayerIndex} : \n {serialized}");
                        RPC_AssignCardsToPlayer(playerManager.GetPlayerRef(player), serialized);
                    }

                }
            }

            await DistributeEmptyCardsAnimation(players, shuffledDeck);

            await UniTask.WaitUntil(() => localPLayer.handData != null);
            
            localPLayer.ReassignHand(localPLayer.handData);
            localPLayer.RevealHand(true);
        }

        #region Helper Methods

        private async UniTask DistributeEmptyCardsAnimation(List<PlayerBase> players, List<Card> deck)
        {
            const int cardsPerPlayer = 5;
            var playerCount = playerManager.GetPlayers().Count;
            
            for (var i = 0; i < cardsPerPlayer; i++)
            {
                for (var j = 0; j < playerCount; j++)
                {
                    var cardIndex = i * playerCount + j;
                    if (cardIndex >= deck.Count) continue;

                    var card = deck[cardIndex];
                    players[j].hand.Add(card);
                    await players[j].AddCardToHandUI(card, cardDistributionAnimationTime);
                }
            }
            kitty = new List<Card>();

            for (var i = cardsPerPlayer * playerCount; i < deck.Count; i++)
            {
                kitty.Add(deck[i]);
            }
        }

        #endregion

        #endregion

        #region Setup Trump
        
        private async UniTask SetupTrumpCard()
        {
            if (Runner.IsServer || HasStateAuthority)
            {
                var topKittyCard = kitty[0];
                var cardDataDto = new CardDataDto()
                {
                    rank = topKittyCard.cardData.rank,
                    suit = topKittyCard.cardData.suit,
                };
                var cardJson = JsonConvert.SerializeObject(cardDataDto);
                Rpc_SendTopKittyCard(cardJson);
            }
            
            
            HideKittyCards();
            
            await UniTask.WaitUntil(() => _topKittyCardData != null);
            
            ShowTopKittyCard(_topKittyCardData);
            
            await UniTask.Delay(3000);

            if (Runner.IsServer || HasStateAuthority)
            {
                var trumpCallerAndChoice = await AskToAcceptTrump();

                if (trumpCallerAndChoice.Choice is 0)
                {
                    trumpCallerAndChoice = await AskToAcceptTrumpFromOtherSuits();
                }
                
                if (trumpCallerAndChoice.Choice is 0)
                {
                    GameLogger.ShowLog("No One Selected Trump", GameLogger.LogType.Error);
                    return;
                }
                
                GameLogger.ShowLog($"{trumpCallerAndChoice.PlayerIndex} Selected Trump as {trumpCallerAndChoice.Suit}");
                _trumpSelectionData = trumpCallerAndChoice;
                
                var willGoAlone = trumpCallerAndChoice.Choice == 2;
                SetUpTeam(trumpCallerAndChoice.PlayerIndex, willGoAlone);

                GameLogger.ShowLog($"Setting Up Data {_trumpSelectionData.Choice}");

                RPC_ShareTrumpSuitData(_trumpSelectionData.PlayerIndex, (int)trumpCallerAndChoice.Suit, trumpCallerAndChoice.Choice);
            }

            await UniTask.WaitUntil(() => _trumpSelectionData != null);
            
        }

        #region Helper Methods
        
        private void HideKittyCards()
        {
            foreach (var kittyCard in kitty)
                kittyCard.gameObject.SetActive(false);
        }
        
        private void ShowTopKittyCard(CardData topCard)
        {
            GameLogger.ShowLog($"Top kitty card showed : {topCard.rank} of {topCard.suit}");
            TopKittyCard = kitty[0];
            TopKittyCard.SetCardData(topCard);
            TopKittyCard.SetFaceUp(true);
            TopKittyCard.gameObject.SetActive(true);
        }

        private async UniTask<TrumpSelectionData> AskToAcceptTrump()
        {
            var current = playerManager.GetLeadPlayerToPlay();
            var trumpCallerIndex = -1;
            var choice = -1;
            var suit = Suit.None;
            
            for (int i = 0; i < 4; i++)
            {
                TrumpAcceptedTcs = new UniTaskCompletionSource<TrumpSelectionData>();
                
                RPC_PlayTurnNotification(current.PlayerIndex);

                if (current.IsBot)
                {
                    GameLogger.ShowLog($"Asking Bot {current.PlayerIndex} to accept trump.");
                    current.AskToAcceptTrump(TopKittyCard).Forget();
                }
                else
                {
                    GameLogger.ShowLog($"Asking Player {current.PlayerIndex} to accept trump.");
                    var playerRef = playerManager.GetPlayerRef(current);
                    RPC_AskToAcceptTrump(playerRef);
                }
                
                var playerAndChoice = await TrumpAcceptedTcs.Task;
                
                choice = playerAndChoice.Choice;
                suit = playerAndChoice.Suit;
                if (choice == 0)
                {
                    RPC_PlayMessage(current.PlayerIndex, $"Pass");
                    current = playerManager.GetNextPlayerToPlay(current);
                }
                else if (choice is 1)
                {
                    trumpCallerIndex = playerAndChoice.PlayerIndex;
                    GameLogger.ShowLog($"Player {trumpCallerIndex} accepted trump.");
                    RPC_PlayMessage(current.PlayerIndex, $"{suit} selected as Trump");
                    await ExchangeTopKittyCardWithDealer();
                    
                    break;
                }
                else if (choice == 2)
                {
                    trumpCallerIndex = playerAndChoice.PlayerIndex;
                    GameLogger.ShowLog($"Player {trumpCallerIndex} accepted trump.");
                    RPC_PlayMessage(current.PlayerIndex, $"{suit} selected as Trump & Player {playerAndChoice.PlayerIndex} decided to play alone");
                    await ExchangeTopKittyCardWithDealer();
                    
                    break;
                }
                else
                    GameLogger.ShowLog($"Invalid Choice {choice}", GameLogger.LogType.Error);
            }

            return new TrumpSelectionData(trumpCallerIndex, suit, choice);
        }

        private async UniTask ExchangeTopKittyCardWithDealer()
        {
            CardExchangedTcs = new UniTaskCompletionSource();
            var dealer = playerManager.GetDealerPlayer();
            RPC_PlayTurnNotification(dealer.PlayerIndex);
            RPC_PlayMessage(dealer.PlayerIndex, $"Choose a Card To Discard");

            if (dealer.IsBot)
            {
                dealer.AskToExchangeTrumpCard(TopKittyCard).Forget();
            }
            else
            {
                var dealerRef = playerManager.GetPlayerRef(dealer);
                Rpc_AskDealerToExchangeCard(dealerRef);
            }
            
            await CardExchangedTcs.Task;
        }

        private async UniTask<TrumpSelectionData> AskToAcceptTrumpFromOtherSuits()
        {
            var current = playerManager.GetLeadPlayerToPlay();
            var trumpCallerIndex = -1;
            var choice = -1;
            var suit = Suit.None;
            
            for (int i = 0; i < 4; i++)
            {
                TrumpAcceptedTcs = new UniTaskCompletionSource<TrumpSelectionData>();
                var forceful = i == 3;
                RPC_PlayTurnNotification(current.PlayerIndex);

                
                if (current.IsBot)
                {
                    GameLogger.ShowLog($"Asking Bot {current.PlayerIndex} to accept trump.");
                    current.ChooseTrumpSuit(TopKittyCard, forceful).Forget();
                }
                else
                {
                    GameLogger.ShowLog($"Asking Player {current.PlayerIndex} to accept trump.");
                    var playerRef = playerManager.GetPlayerRef(current);
                    RPC_AskToAcceptTrumpFromOtherSuits(playerRef, forceful);
                }

                var playerAndChoice = await TrumpAcceptedTcs.Task;

                choice = playerAndChoice.Choice;
                suit = playerAndChoice.Suit;
                if (choice == 0)
                {
                    RPC_PlayMessage(current.PlayerIndex, $"Pass");
                    current = playerManager.GetNextPlayerToPlay(current);
                }
                else if (choice == 1)
                {
                    trumpCallerIndex = playerAndChoice.PlayerIndex;
                    GameLogger.ShowLog($"Player {trumpCallerIndex} accepted trump.");
                    RPC_PlayMessage(current.PlayerIndex, $"{suit} selected as Trump");
                    break;
                }
                else if (choice == 2)
                {
                    trumpCallerIndex = playerAndChoice.PlayerIndex;
                    GameLogger.ShowLog($"Player {trumpCallerIndex} accepted trump And decided to go alone.");
                    RPC_PlayMessage(current.PlayerIndex, $"{suit} selected as Trump & Player {playerAndChoice.PlayerIndex} decided to play alone");
                    break;
                }
                else
                    GameLogger.ShowLog($"Invalid Choice {choice}", GameLogger.LogType.Error);
            }

            return new TrumpSelectionData(trumpCallerIndex, suit, choice);
        }
        
        private void SetUpTeam(int trumpCallerIndex, bool willGoAlone)
        {
            if (playerManager.TeamA.player0Index == trumpCallerIndex || playerManager.TeamA.player1Index == trumpCallerIndex)
            {
                var makersTeam = playerManager.TeamA;
                var defendersTeam = playerManager.TeamB;
                makersTeam.teamType = TeamType.Makers;
                defendersTeam.teamType = TeamType.Defenders;
                makersTeam.willGoAlone = willGoAlone;
                playerManager.TeamA = makersTeam;
                playerManager.TeamB = defendersTeam;
            }
            else
            {
                var makersTeam = playerManager.TeamB;
                var defendersTeam = playerManager.TeamA;
                makersTeam.teamType = TeamType.Makers;
                defendersTeam.teamType = TeamType.Defenders;
                makersTeam.willGoAlone = willGoAlone;
                playerManager.TeamB = makersTeam;
                playerManager.TeamA = defendersTeam;
            }
        }
        #endregion

        #endregion

        #region Normal Game
        private async UniTask StartNormalGame()
        {
            if (Runner.IsServer)
            {
                var currentPlayer = playerManager.GetLeadPlayerToPlay();

                while (currentPlayer.hand.Count > 0)
                {
                    var playedCards = new Dictionary<int, CardData>();
                    CurrentTrickSuit = Suit.None;

                    for (int i = 0; i < 4; i++)
                    {
                        if (currentPlayer.IsDisabled)
                        {
                            currentPlayer = playerManager.GetNextPlayerToPlay(currentPlayer);
                            continue;
                        }

                        CardPlayedTcs = new UniTaskCompletionSource<CardData>();
                        
                        RPC_PlayTurnNotification(currentPlayer.PlayerIndex);

                        if (currentPlayer.IsBot)
                        {
                            currentPlayer.PlayTurn().Forget();
                        }
                        else
                        {
                            RPC_PlayTurn(playerManager.GetPlayerRef(currentPlayer));
                        }
                        
                        var cardData = await CardPlayedTcs.Task;

                        if (CurrentTrickSuit == Suit.None)
                            CurrentTrickSuit = cardData.suit;

                        playedCards[currentPlayer.PlayerIndex] = cardData;
                        if (allPlayedCards.ContainsKey(cardData))
                            GameLogger.ShowLog($"Error: Adding a Duplicate Card {cardData.rank} of {cardData.suit}", GameLogger.LogType.Error);

                        allPlayedCards.Add(cardData, currentPlayer.PlayerIndex);

                        currentPlayer = playerManager.GetNextPlayerToPlay(currentPlayer);
                    }
                    
                    var winner = GetTrickWinner(playedCards, CurrentTrickSuit, TrumpSuit);
                    await UniTask.Delay(2000, cancellationToken: CancellationTokenSource.Token);
                    
                    RPC_PlayTrickAnimation(winner.PlayerIndex);

                    currentPlayer = winner;
                    playedCards.Clear();
                }
            }
            
            await UniTask.WaitUntil(AllHandsAreEmpty);
        }

        #region Helper Funtions

        private bool AllHandsAreEmpty()
        {
            foreach (var player in playerManager.GetPlayers())
            {
                if (player.IsDisabled) continue;
                if (player.hand.Count > 0) return false;
            }

            return true;
        }
        
        private PlayerBase GetTrickWinner(Dictionary<int, CardData> playedCards, Suit leadSuit, Suit trumpSuit)
        {
            PlayerBase winner = null;
            CardData winningCard = null;

            var debugString = string.Empty;
            foreach (var (playerIndex, cardData) in playedCards)
            {
                if (cardData is null) continue;
                
                debugString += $"Card : {cardData.rank} of {cardData.suit} Power : {cardData.GetCardPower(trumpSuit, leadSuit)}\n";
                
                if (winningCard != null && cardData.GetCardPower(trumpSuit, leadSuit) <= winningCard.GetCardPower(trumpSuit, leadSuit)) 
                    continue;
                winner = playerManager.GetPlayer(playerIndex);
                winningCard = cardData;
            }
            GameLogger.ShowLog(debugString);

            return winner;
        }
        #endregion


        #endregion
        
        #region ShowResults

        private async UniTask ShowResults()
        {
            if (HasStateAuthority)
            {
                CalculateResults();
            } 

            await ScoreUpdate.Task;
            // TODO: Animate to show a board that displays the score
            _gamePlayScreen.UpdateScore(playerManager.TeamA, playerManager.TeamB);
            await UniTask.Delay(2000, cancellationToken: CancellationTokenSource.Token);
        }

        #endregion

        #region Helper Functions
        
        private void CalculateResults()
        {
            var teamATricks = playerManager.GetPlayer(playerManager.TeamA.player0Index).TricksWon +
                              playerManager.GetPlayer(playerManager.TeamA.player1Index).TricksWon;

            int teamBTricks = playerManager.GetPlayer(playerManager.TeamB.player0Index).TricksWon +
                              playerManager.GetPlayer(playerManager.TeamB.player1Index).TricksWon;

            var teamA = playerManager.TeamA;
            var teamB = playerManager.TeamB;

            bool isTeamAMakers = teamA.teamType == TeamType.Makers;

            int makersTricks = isTeamAMakers ? teamATricks : teamBTricks;

            if (makersTricks is >= 3 and < 5)
            {
                if (isTeamAMakers)
                    teamA.score += 1;
                else
                    teamB.score += 1;
            }
            else if (makersTricks == 5)
            {
                if (isTeamAMakers)
                    teamA.score += teamA.willGoAlone ? 4 : 2;
                else
                    teamB.score += teamB.willGoAlone ? 4 : 2;
            }
            else
            {
                if (isTeamAMakers)
                    teamB.score += 2;
                else
                    teamA.score += 2;
            }

            // Assign updated structs back
            playerManager.TeamA = teamA;
            playerManager.TeamB = teamB;

            RPC_SyncTeamData();
        }

        #endregion
        
        #region Clear Board

        private async UniTask ClearBoard()
        {
            foreach (var player in playerManager.GetPlayers())
            {
                player.hand.Clear();
                player.TricksWon = 0;
                playerManager.UpdateTrickCount(player);
                player.IsDisabled = false;
                player.ResetUiElement();
            }
            
            // Reset team states for next hand
            var teamA = playerManager.TeamA;
            var teamB = playerManager.TeamB;
            teamA.teamType = TeamType.Defenders; // Reset to default
            teamA.willGoAlone = false;
            teamB.teamType = TeamType.Defenders; // Reset to default  
            teamB.willGoAlone = false;
            playerManager.TeamA = teamA;
            playerManager.TeamB = teamB;
            
            _trumpSelectionData = null;
            
            kitty.Clear();
            allPlayedCards.Clear();

            cardsController.Reset();
            _gamePlayScreen.Reset();
            playerManager.Reset();
            
            _topKittyCardData = null;
            kitty = new List<Card>();
            

            ScoreUpdate = new UniTaskCompletionSource();
            await UniTask.Delay(2000, cancellationToken: CancellationTokenSource.Token);
        }

        #endregion

        #region Helper funtions

        public void SetPlayerInitialized(int index)
        {
            PlayersInitialized.Set(index, true);
        }
        
        public Card GetCardPlayedByPartner(PlayerBase player)
        {
            var otherPlayer = playerManager.GetOppositePlayerOfTeam(player);
            return currentTrickCards.GetValueOrDefault(otherPlayer.PlayerIndex);
        }

        #endregion

        #region RPCs

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_AssignDealerNotification(int dealerIndex)
        {
            GameLogger.ShowLog($"Dealer Is Assigned to player {dealerIndex}");

            if (dealerIndex != playerManager.DealerIndex)
                GameLogger.ShowLog("Game Crash At Dealer Index", GameLogger.LogType.Error);

            playerManager.SetUpDealer();

        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_AssignCardsToPlayer([RpcTarget] PlayerRef target, string json)
        {
            var localPlayer = playerManager.GetLocalPlayerBase();
            if (localPlayer == null || playerManager.GetLocalPlayerRef() != target) return;
            GameLogger.ShowLog($"Hand for this player : {json}");

            var dtoList = JsonConvert.DeserializeObject<List<CardDataDto>>(json);
            var actualCards = dtoList.Select(dto =>
            {
                var card = cardsController.ToCardData(dto);
                return card;
            }).ToList();
            
            localPlayer.handData = actualCards;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_SendTopKittyCard(string cardJson)
        {
            var cardData = JsonConvert.DeserializeObject<CardDataDto>(cardJson);
            _topKittyCardData = cardsController.ToCardData(cardData);
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_AskToAcceptTrump(PlayerRef targetPlayer)
        {
            var localPlayerRef =  playerManager.GetLocalPlayerRef();
            if (localPlayerRef == targetPlayer)
            {
                _ = playerManager.GetLocalPlayerBase().AskToAcceptTrump(TopKittyCard);
            }
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_AskDealerToExchangeCard(PlayerRef dealerRef)
        {
            var localPlayerRef = playerManager.GetLocalPlayerRef();
            if (localPlayerRef == dealerRef)
            {
                _ = playerManager.GetLocalPlayerBase().AskToExchangeTrumpCard(TopKittyCard);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_AskToAcceptTrumpFromOtherSuits(PlayerRef targetPlayer, bool forceful = false)
        {
            var localPlayerRef = playerManager.GetLocalPlayerRef();
            if (localPlayerRef == targetPlayer)
            {
                _ = playerManager.GetLocalPlayerBase().ChooseTrumpSuit(TopKittyCard, forceful);
            }
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShareTrumpSuitData(int playerIndex, int suit, int choice)
        {
            _trumpSelectionData = new TrumpSelectionData(
                playerIndex,
                (Suit)suit,
                choice);
            
            GameLogger.ShowLog($"setting up Trump Data as (player: {playerIndex} : Suit: {suit}, Choice: {choice})");
            var trumpCaller = playerManager.GetPlayer(playerIndex);
            _gamePlayScreen.ActiveTrumpSuit(_trumpSelectionData.Suit, trumpCaller.PlayerElementUi);
            _gamePlayScreen.DisableDeck();
            
            if (choice == 2)
            {
                playerManager.GetOppositePlayerOfTeam(playerManager.GetPlayer(_trumpSelectionData.PlayerIndex)).IsDisabled = true;
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayTurn(PlayerRef player)
        {
            var playerBase = playerManager.GetPlayerBase(player);

            var localPlayerRef = playerManager.GetLocalPlayerRef();
            if (localPlayerRef == player)
            {
                playerBase.PlayTurn(timeToPlayTrun);
            }
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayTurnNotification(int playerIndex)
        {
            foreach (var player in playerManager.GetPlayers())
                player.UpdateUiOnEndTurn();

            var playerBase = playerManager.GetPlayer(playerIndex);
            playerBase.UpdateUiOnPlayerTurn();
        }
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayMessage(int playerIndex, string chatMessage)
        {
            var playerBase = playerManager.GetPlayer(playerIndex);
            playerBase.SendMessageToUi(chatMessage).Forget();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayTrickAnimation(int winnerIndex)
        {
            foreach (var p in playerManager.GetPlayers())
                p.UpdateUiOnEndTurn();
            
            var cards = currentTrickCards.Values.ToArray();
            var player = playerManager.GetPlayer(winnerIndex);
            _ = player.AddCardToWinDeckUI(cards, 0.5f);
            player.TricksWon++;
            playerManager.UpdateTrickCount(player);
            currentTrickCards.Clear();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SyncTeamData()
        {
            ScoreUpdate.TrySetResult();
        }

        #endregion

    }
}