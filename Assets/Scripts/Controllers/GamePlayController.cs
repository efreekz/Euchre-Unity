using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion;
using GamePlay;
using GamePlay.Cards;
using GamePlay.Interfaces;
using GamePlay.Player;
using Managers;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;
using GameMode = Managers.GameMode;

namespace Controllers
{
    
    
    public class GamePlayController : MonoBehaviour, IGameController
    {
        public static GamePlayController Instance;

        public CardsController cardsController;
        public PlayerManager playerManager;
        
        public float cardDistributionAnimationTime = 0.2f;
        
        public Suit TrumpSuit {get; set;}
        public Suit CurrentTrickSuit { get; set; }

        private List<Card> _kitty = new();
        
        private GamePlayScreen _gamePlayScreen;
        private PlayerBase _trumpCaller;
        private Dictionary<PlayerBase, Card> _currentTrickCards = new Dictionary<PlayerBase, Card>();
        public static CancellationTokenSource CancellationTokenSource;
        
        
        public List<Card> GetCurrentTrickCards()
        {
            return _currentTrickCards.Values.ToList();
        }

        public Card GetCardPlayedByPartner(PlayerBase player)
        {
            var otherPlayer = playerManager.GetOppositePlayerOfTeam(player);
            return _currentTrickCards.GetValueOrDefault(otherPlayer);
        }
        
        private void Awake()
        {
            Instance = this;
            CancellationTokenSource = new CancellationTokenSource();
        }
        
        public UniTask Initialize()
        {
            _gamePlayScreen = UiManager.Instance.GetUiView(UiScreenName.GamePlayScreens) as GamePlayScreen;

            cardsController.Initialize(_gamePlayScreen);
            playerManager.Initialize(_gamePlayScreen);
            
            return UniTask.CompletedTask;
        }

        public async UniTask StartGame()
        {
            await StartGameSequence();
            
            GameManager.LoadScene(SceneName.MainMenu);
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
                await DistributeCardsAnimated();
                await SetupTrumpCard();
                await StartNormalGame();

                await ClearBoard();
            }
        }
        
        private async UniTask ClearBoard()
        {
            foreach (var player in playerManager.GetPlayers())
            {
                player.hand.Clear();
                player.TricksWon = 0;
                player.IsDisabled = false;
                player.ResetUiElement();
            }

            // Reset team states for next hand
            playerManager.TeamA.teamType = TeamType.Defenders; // Reset to default
            playerManager.TeamA.willGoAlone = false;
            playerManager.TeamB.teamType = TeamType.Defenders; // Reset to default
            playerManager.TeamB.willGoAlone = false;

            // Advance dealer to next player clockwise
            playerManager.AdvanceDealer();

            foreach (var card in _kitty)
            {
                Destroy(card.gameObject);
            }
            _kitty.Clear();

            cardsController.Reset();
            _gamePlayScreen.Reset();

            await UniTask.Delay(2000, cancellationToken: CancellationTokenSource.Token);
        }


        
        private async UniTask DistributeCardsAnimated()
        {
            var players = playerManager.GetPlayers();
            var shuffledDeck = cardsController.GetShuffledDeck();

            var playerCount = players.Count;
            const int cardsPerPlayer = 5;

            // Deal cards starting from player to left of dealer (proper Euchre rotation)
            int startPlayerIndex = (playerManager.dealerIndex + 1) % 4;

            for (var i = 0; i < cardsPerPlayer; i++)
            {
                for (var j = 0; j < playerCount; j++)
                {
                    int playerIndex = (startPlayerIndex + j) % 4; // Rotate properly from left of dealer
                    var cardIndex = i * playerCount + j;
                    if (cardIndex >= shuffledDeck.Count) continue;

                    var card = shuffledDeck[cardIndex];
                    players[playerIndex].hand.Add(card);
                    await players[playerIndex].AddCardToHandUI(card, cardDistributionAnimationTime);
                }
            }

            _kitty = new List<Card>();

            for (var i = cardsPerPlayer * playerCount; i < shuffledDeck.Count; i++)
            {
                _kitty.Add(shuffledDeck[i]);
            }

            for (int i = 0; i < players.Count; i++)
            {
                players[i].RevealHand(i == 0);
            }
        }
        
        private async UniTask SetupTrumpCard()
        {
            if (_kitty.Count == 0) return;

            _trumpCaller = null;

            HideKittyCards();

            var topKittyCard = _kitty[0];
            ShowTopKittyCard(topKittyCard);

            var trumpSuit = topKittyCard.cardData.suit;

            _trumpCaller = await TryAcceptTrump(topKittyCard);

            if (_trumpCaller == null)
            {
                trumpSuit = await LetPlayersChooseTrump(topKittyCard);
            }

            if (_trumpCaller == null)
            {
                trumpSuit = await ForceDealerToChooseTrump(topKittyCard);
            }

            var willGoAlone = await _trumpCaller.AskToGoAlone();

            TrumpSuit = trumpSuit;
            _gamePlayScreen.ActiveTrumpSuit(TrumpSuit, _trumpCaller.PlayerElementUi);
            _gamePlayScreen.DisableDeck();

            SetUpTeam(_trumpCaller, willGoAlone);

            if (willGoAlone)
            {
                playerManager.GetOppositePlayerOfTeam(_trumpCaller).IsDisabled = true;
            }
        }
        
        #region Helper Methods

        private void HideKittyCards()
        {
            foreach (var kittyCard in _kitty)
                kittyCard.gameObject.SetActive(false);
        }

        private void ShowTopKittyCard(Card topCard)
        {
            topCard.SetFaceUp(true);
            topCard.gameObject.SetActive(true);
        }

        private async UniTask<PlayerBase> TryAcceptTrump(Card topCard)
        {
            var players = playerManager.GetPlayers();
            var startIndex = playerManager.LeadPlayerIndex;

            for (var i = 0; i < 4; i++)
            {
                var playerIndex = (startIndex + i) % 4;
                var player = players[playerIndex];

                var accepted = await player.AskToAcceptTrump(topCard);
                await UniTask.Delay(1000, cancellationToken: CancellationTokenSource.Token);

                if (!accepted) continue;

                Debug.Log($"{player.name} accepted {topCard.cardData.suit} as trump.");
                var dealer = playerManager.GetDealerPlayer();
                await dealer.AskToExchangeTrumpCard(topCard);
                return player;
            }

            return null;
        }
        private async UniTask<Suit> LetPlayersChooseTrump(Card topCard)
        {
            Debug.Log("No one accepted the top card. Let players choose trump manually.");

            var trumpSuit = Suit.None;
            foreach (var player in playerManager.GetPlayers())
            {
                var chosenSuit = await player.ChooseTrumpSuit(topCard);

                if (chosenSuit == Suit.None || chosenSuit == topCard.cardData.suit) continue;

                trumpSuit = chosenSuit;
                Debug.Log($"{player.name} chose {trumpSuit} as trump.");
                _trumpCaller = player;
                break;
            }

            return trumpSuit;
        }
        
        private async UniTask<Suit> ForceDealerToChooseTrump(Card topCard)
        {
            Debug.Log("No one accepted the Trump. Let Dealer forcefully choose Trump.");
            var dealer = playerManager.GetDealerPlayer();
            var trumpSuit = await dealer.ChooseTrumpSuit(topCard, true);
            await UniTask.Delay(1000, cancellationToken: CancellationTokenSource.Token);
            _trumpCaller = dealer;
            return trumpSuit;
        }

        private void SetUpTeam(PlayerBase trumpCaller, bool willGoAlone)
        {
            var makersTeam = playerManager.TeamA.players.Contains(trumpCaller) ? playerManager.TeamA : playerManager.TeamB;
            var defendersTeam = makersTeam == playerManager.TeamA ? playerManager.TeamB : playerManager.TeamA;

            makersTeam.teamType = TeamType.Makers;
            defendersTeam.teamType = TeamType.Defenders;
            
            makersTeam.willGoAlone = willGoAlone;
        }

        #endregion
        
        private async UniTask StartNormalGame()
        {
            var currentPlayer = playerManager.GetLeadPlayerToPlay();

            while (currentPlayer.hand.Count > 0)
            {
                CurrentTrickSuit = Suit.None;

                for (int i = 0; i < 4; i++)
                {
                    if (currentPlayer.IsDisabled)
                    {
                        currentPlayer = playerManager.GetNextPlayerToPlay(currentPlayer);
                        continue;
                    }

                    var playedCard = await currentPlayer.PlayTurn();

                    if (CurrentTrickSuit == Suit.None)
                        CurrentTrickSuit = playedCard.cardData.suit;

                    _currentTrickCards[currentPlayer] = playedCard;

                    await UniTask.Delay(1000, cancellationToken: CancellationTokenSource.Token);

                    currentPlayer = playerManager.GetNextPlayerToPlay(currentPlayer);
                }

                var winner = GetTrickWinner(_currentTrickCards, CurrentTrickSuit, TrumpSuit);

                await winner.AddCardToWinDeckUI(_currentTrickCards.Values.ToArray(), 0.3f);
                winner.TricksWon++;

                currentPlayer = winner;
                _currentTrickCards.Clear();
            }

            CalculateResults();
        }


        #region Helper Methods
        
        private void CalculateResults()
        {
            // Count tricks per team
            var teamATricks = playerManager.TeamA.players.Sum(player => player.TricksWon);
            var teamBTricks = playerManager.TeamB.players.Sum(player => player.TricksWon);

            TeamData makersTeam;
            TeamData defendersTeam;
            if (playerManager.TeamA.teamType == TeamType.Makers)
            {
                makersTeam = playerManager.TeamA;
                defendersTeam = playerManager.TeamB;
            }
            else
            {
                makersTeam = playerManager.TeamB;
                defendersTeam = playerManager.TeamA;
            }

            var makersTricks = makersTeam == playerManager.TeamA ? teamATricks : teamBTricks;

            // Score calculation
            if (makersTricks is >= 3 and < 5)
            {
                makersTeam.score += 1;
            }
            else if (makersTricks == 5)
            {
                makersTeam.score += makersTeam.willGoAlone ? 4 : 2;
            }
            else
            {
                defendersTeam.score += 2;
            }

            _gamePlayScreen.UpdateScore(playerManager.TeamA, playerManager.TeamB);
        }
        private PlayerBase GetTrickWinner(Dictionary<PlayerBase, Card> playedCards, Suit leadSuit, Suit trumpSuit)
        {
            PlayerBase winner = null;
            Card winningCard = null;

            var debugString = string.Empty;
            foreach (var (player, card) in playedCards)
            {
                if (card is null) continue;
                debugString += $"Card : {card.cardData.rank} of {card.cardData.suit} Power : {card.GetCardPower(trumpSuit, leadSuit)}\n";
                
                if (winningCard != null && card.GetCardPower(trumpSuit, leadSuit) <= winningCard.GetCardPower(trumpSuit, leadSuit)) 
                    continue;
                winner = player;
                winningCard = card;
            }
            Debug.Log(debugString);

            return winner;
        }

        #endregion
        
    }
}
