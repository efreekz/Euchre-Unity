using System.Collections.Generic;
using System.Linq;
using Controllers;
using Cysharp.Threading.Tasks;
using GamePlay.Cards;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GamePlay.Player
{
    public class BotPlayer : PlayerBase
    {
        [Header("Testing")] 
        [SerializeField] private bool handIsFaceUp;
        [SerializeField] private bool ignoreAskToAccept;
        
        public override async UniTask<Card> PlayTurn(float time = 10f)
        {
            if (IsDisabled) return null;

            if (handIsFaceUp) RevealHand(true);

            PlayerElementUi.PlayTurn();

            // Simulate thinking time
            await UniTask.Delay(Random.Range(600, 1200), cancellationToken: GamePlayController.CancellationTokenSource.Token);

            var playableCards = CheckPlayableCards();
            var trickSuit = GamePlayController.Instance.CurrentTrickSuit;
            var trumpSuit = GamePlayController.Instance.TrumpSuit;
            var trickCards = GamePlayController.Instance.GetCurrentTrickCards();
            var isLead = trickCards.Count == 0;
            var partnerCard = GamePlayController.Instance.GetCardPlayedByPartner(this);

            Dictionary<Card, float> posteriors = new();

            foreach (var card in playableCards)
            {
                float prior = 1f / playableCards.Length;

                int power = card.GetCardPower(trumpSuit, trickSuit);
                float contextBonus = 0f;

                if (isLead)
                {
                    contextBonus += power / 100f;
                }
                else if (partnerCard != null)
                {
                    bool partnerWinning = IsCardWinning(partnerCard, trickCards, trumpSuit, trickSuit);
                    if (partnerWinning)
                    {
                        power = 100 - power; // Invert so lower power is favored
                        contextBonus -= 0.5f;
                    }
                    else
                    {
                        contextBonus += 0.7f * (power / 100f);
                    }
                }
                else
                {
                    contextBonus += power / 100f;
                }

                float likelihood = Mathf.Clamp01((0.3f * (power / 100f)) + 0.7f * contextBonus);
                ShowLog($"Likelihood of Card: {card.cardData.rank} of {card.cardData.suit} is {likelihood}");

                float posterior = prior * likelihood;
                posteriors[card] = posterior;
                ShowLog($"Posterior of Card: {card.cardData.rank} of {card.cardData.suit} is {posterior}");
            }

            var bestCard = posteriors.OrderByDescending(kvp => kvp.Value).First().Key;

            hand.Remove(bestCard);
            await AnimateCardPlay(bestCard);
            PlayerElementUi.EndTurn();

            return bestCard;
        }

        
        private bool IsCardWinning(Card contender, List<Card> trickCards, Suit trump, Suit lead)
        {
            return trickCards.All(other =>
                contender.GetCardPower(trump, lead) >= other.GetCardPower(trump, lead));
        }

        public override async UniTask<bool> AskToAcceptTrump(Card topCard)
        {
            if (handIsFaceUp) RevealHand(true);
            if (ignoreAskToAccept) return false;

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

            await UniTask.Delay(Random.Range(1000, 2000), cancellationToken: GamePlayController.CancellationTokenSource.Token); // Simulate thinking time

            if (acceptTrump)
            {
                await SendMessageToUi($"{topCard.cardData.suit} selected as Trump");
            }
            else
            {
                await SendMessageToUi("Pass");
            }

            return acceptTrump;
        }
        
        public override async UniTask<Suit> ChooseTrumpSuit(Card topCard, bool forceFullSuit = false)
        {
            if (handIsFaceUp) RevealHand(true);

            Suit[] suits = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };
            var availableSuits = new List<Suit>(suits);

            availableSuits.Remove(topCard.cardData.suit); // can't pick topCard's suit unless forced

            Dictionary<Suit, float> suitScores = new();

            foreach (var suit in availableSuits)
            {
                var score = EvaluateTrumpSuit(suit);
                suitScores[suit] = score;
                ShowLog($"[Bot] Potential Trump: {suit}, Hand Strength Score: {score}");
            }

            var bestSuit = suitScores.OrderByDescending(pair => pair.Value).First();

            await UniTask.Delay(Random.Range(1000, 2000), cancellationToken: GamePlayController.CancellationTokenSource.Token); // Simulate thinking time

            if (bestSuit.Value < 250)
            {
                if (forceFullSuit)
                {
                    await SendMessageToUi($"{bestSuit.Key} selected as Trump");
                    ShowLog($"[Bot] Choosing Trump: {bestSuit.Key}");
                    return bestSuit.Key;
                }

                await SendMessageToUi("Pass");
                ShowLog("[Bot] No strong trump suit. Passing...");
                return Suit.None;
            }

            ShowLog($"[Bot] Choosing Trump: {bestSuit.Key}");
            await SendMessageToUi($"{bestSuit.Key} selected as Trump");

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
            var weakest = hand.OrderBy(card => card.GetCardPower(trumpSuit, Suit.None)).First();

            hand.Remove(weakest);
            hand.Add(topKittyCard);

            await AnimateCardExchange(weakest, topKittyCard, handIsFaceUp);

            if (!handIsFaceUp)
            {
                weakest.SetFaceUp(false);
                topKittyCard.SetFaceUp(false);
            }

            ShowLog($"Weakest Card : {weakest.cardData.rank} of {weakest.cardData.suit} :: Power {weakest.GetCardPower(trumpSuit, Suit.None)}");

            return weakest;
        }
        
        public override UniTask<bool> AskToGoAlone()
        {
            if (handIsFaceUp) RevealHand(true);
            return UniTask.FromResult(false);
        }

    }
}
