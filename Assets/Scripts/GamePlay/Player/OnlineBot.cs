using System;
using Controllers;
using Cysharp.Threading.Tasks;
using Fusion;
using GamePlay.Cards;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GamePlay.Bot;
using Newtonsoft.Json;
using CardDataDto = GamePlay.Cards.CardDataDto;
using Random = UnityEngine.Random;
using Suit = GamePlay.Cards.Suit;

namespace GamePlay.Player
{
    public class OnlineBot : PlayerBase
    {
        [Header("Testing")] 
        [SerializeField] private bool handIsFaceUp;

        [SerializeField] private int simulationCount = 100;
        [SerializeField] private float uctC = 1.41f;
        
        public override async UniTask<Card> PlayTurn(float time = 10f)
        {
            if (IsDisabled) return null;
        
            RevealHand(handIsFaceUp);
        
            var botGameState = BuildBotGameState(); // Helper method to extract state
            var decisionEngine = new EuchreBotDecisionEngine(simulationCount, uctC);
        
            // Run the heavy decision-making logic in a background thread
            var selectedCardData = decisionEngine.SelectCardToPlay(botGameState);
            var card = hand.FirstOrDefault(c => c.cardData == selectedCardData);
        
            // Async simulate thinking
            await UniTask.Delay(Random.Range(1200, 2400), cancellationToken: GamePlayControllerNetworked.CancellationTokenSource.Token);
            
            hand.Remove(card);
            GamePlayControllerNetworked.Instance.currentTrickCards[PlayerIndex] = card;
        
            var cardDataDto = JsonConvert.SerializeObject(new CardDataDto()
            {
                rank = selectedCardData.rank,
                suit = selectedCardData.suit
            });
        
            RPC_PlayCard(PlayerIndex, cardDataDto);
            await AnimateCardPlay(card);
        
            return card;
        }

        #region Helper Funtrions

        private GameState BuildBotGameState()
        {
            var gameController = GamePlayControllerNetworked.Instance;

            // Get current trick cards (player index -> card)
            var currentTrick = gameController.currentTrickCards.ToDictionary(pair => pair.Key, pair => pair.Value.cardData);

            // Build Player Summaries
            var playerSummaries = new List<PlayerSummary>();
            for (int i = 0; i < 4; i++)
            {
                var isPartner = (i + 2) % 4 == PlayerIndex;
                var cardsPlayedByPlayer = gameController.GetCardsPlayedByPlayer(i);

                playerSummaries.Add(new PlayerSummary
                {
                    PlayerIndex = i,
                    IsBotPartner = isPartner,
                    CardsPlayed = cardsPlayedByPlayer,
                    HasPlayedInCurrentTrick = currentTrick.ContainsKey(i)
                });
            }

            return new GameState
            {
                Hand = hand.Select(card => card.cardData).ToList(),
                PlayedCards = gameController.allPlayedCards.Keys.ToList(),
                TrumpSuit = gameController.TrumpSuit,
                TrickSuit = gameController.CurrentTrickSuit,
                CurrentTrickCards = currentTrick,
                BotPlayerIndex = PlayerIndex,
                AllPlayers = playerSummaries,
                TeamNumber = PlayerIndex % 2,
                Kitty = gameController.kitty.Select(card => card.cardData).ToList()
            };
        }

        
        private bool IsCardWinning(Card contender, List<Card> trickCards, Suit trump, Suit lead)
        {
            return trickCards.All(other =>
                contender.GetCardPower(trump, lead) >= other.GetCardPower(trump, lead));
        }

        #endregion

        public override async UniTask<bool> AskToAcceptTrump(Card topCard)
        {
            if (handIsFaceUp) RevealHand(true);
            var trumpSuit = topCard.cardData.suit;
            var trumpCount = hand.Count(card => card.IsTrump(trumpSuit));

            var acceptanceChance = trumpCount switch
            {
                >= 4 => 0.9f, // 90% chance to accept
                3 => 0.6f,    // 60% chance to accept
                2 => 0.25f,   // 25% chance to accept
                1 => 0.05f,   // 5% chance to accept
                _ => 0.01f    // 1% chance to accept
            };

            var chance = Random.value;
            var acceptTrump = chance < acceptanceChance;

            ShowLog($"{chance} < {acceptanceChance} = {acceptTrump} :: {trumpCount}");

            await UniTask.Delay(Random.Range(1000, 2000), cancellationToken: GamePlayControllerNetworked.CancellationTokenSource.Token);

            Rpc_RespondToTrump(PlayerIndex, (int)topCard.cardData.suit, acceptTrump ? 1 : 0);
            return true;

        }

        public override async UniTask<Suit> ChooseTrumpSuit(Card topCard, bool forceFullSuit = false)
        {
            if (handIsFaceUp) RevealHand(true);

            var availableSuits = Enum.GetValues(typeof(Suit))
                .Cast<Suit>()
                .Where(suit => suit != topCard.cardData.suit && suit != Suit.None)
                .ToList();

            Dictionary<Suit, float> suitScores = new();

            foreach (var suit in availableSuits)
            {
                var score = EvaluateTrumpSuit(suit);
                suitScores[suit] = score;
                ShowLog($"[Bot] Potential Trump: {suit}, Hand Strength Score: {score}");
            }

            var bestSuit = suitScores.OrderByDescending(pair => pair.Value).First();

            await UniTask.Delay(Random.Range(1000, 2000), cancellationToken: GamePlayControllerNetworked.CancellationTokenSource.Token);

            if (bestSuit.Value < 250)
            {
                if (forceFullSuit)
                {
                    ShowLog($"[Bot] Choosing Trump: {bestSuit.Key}");
                    Rpc_RespondToTrump(PlayerIndex, (int)bestSuit.Key, 1);
                    return bestSuit.Key;
                }

                ShowLog("[Bot] No strong trump suit. Passing...");
                Rpc_RespondToTrump(PlayerIndex, 0, 0);
                return Suit.None;
            }

            ShowLog($"[Bot] Choosing Trump: {bestSuit.Key}");
            Rpc_RespondToTrump(PlayerIndex, (int)bestSuit.Key, 1);
            return bestSuit.Key;
        }

        private float EvaluateTrumpSuit(Suit potentialTrump)
        {
            return hand.Select(card => card.GetCardPower(potentialTrump, Suit.None)).Aggregate(0f, (current, power) => current + power);
        }
        
        public override async UniTask<Card> AskToExchangeTrumpCard(Card topKittyCard)
        {
            if (handIsFaceUp) RevealHand(true);

            var trumpSuit = topKittyCard.cardData.suit;
            var selectedCard = hand.OrderBy(card => card.GetCardPower(trumpSuit, Suit.None)).First();

            hand.Remove(selectedCard);
            hand.Add(topKittyCard);

            var selectedCardDataDto = new CardDataDto()
            {
                suit = selectedCard.cardData.suit,
                rank = selectedCard.cardData.rank
            };
            var topKittyCardDataDto = new CardDataDto()
            {
                suit = topKittyCard.cardData.suit,
                rank = topKittyCard.cardData.rank
            };

            var selectedCardJson = JsonConvert.SerializeObject(selectedCardDataDto);
            var topKittyCardJson = JsonConvert.SerializeObject(topKittyCardDataDto);
            
            RPC_ExchangeTrumpCard(selectedCardJson, topKittyCardJson);
            
            ShowLog($"Weakest Card : {selectedCard.cardData.rank} of {selectedCard.cardData.suit} :: Power {selectedCard.GetCardPower(trumpSuit, Suit.None)}");
            
            await AnimateCardExchange(selectedCard, topKittyCard, false);

            return topKittyCard;

        }

        public override UniTask<bool> AskToGoAlone()
        {
            // Default: don't go alone
            return UniTask.FromResult(false);
        }

    }

}