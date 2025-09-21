using System;
using System.Linq;
using System.Threading;
using Controllers;
using Cysharp.Threading.Tasks;
using Fusion;
using GamePlay.Cards;
using Helper;
using Newtonsoft.Json;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;

namespace GamePlay.Player
{
    public class OnlinePlayer : PlayerBase
    {
        public override async UniTask<Card> PlayTurn(float time = 10f)
        {
            if (IsDisabled)
                return null;

            foreach (var card in hand)
            {
                card.SetInteractable(false);
                card.OnCardClicked = null;
            }

            var playableCards = CheckPlayableCards();

            var tcs = new UniTaskCompletionSource<Card>();
            var cts = new CancellationTokenSource();

            // Start UI timer
            PlayerElementUi.StartTurnTimer(time, cts.Token);

            // Setup manual selection
            foreach (var card in playableCards)
            {
                card.SetInteractable(true);
                card.OnCardClicked += async () =>
                {
                    if (cts.IsCancellationRequested) return;

                    cts.Cancel(); // Stop auto-play fallback
                    foreach (var c in playableCards)
                    {
                        c.SetInteractable(false);
                        c.OnCardClicked = null;
                    }

                    hand.Remove(card);

                    var cardString = JsonConvert.SerializeObject(new CardDataDto()
                    {
                        rank = card.cardData.rank,
                        suit = card.cardData.suit
                    });
                    RPC_PlayCard(PlayerIndex, cardString);

                    GamePlayControllerNetworked.Instance.currentTrickCards[PlayerIndex] = card;

                    await AnimateCardPlay(card);
                    tcs.TrySetResult(card);
                };
            }

            // Auto play fallback
            _ = UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: cts.Token).ContinueWith(() =>
            {
                if (tcs.Task.Status == UniTaskStatus.Pending)
                {
                    foreach (var c in playableCards)
                    {
                        c.SetInteractable(false);
                        c.OnCardClicked = null;
                    }

                    var trumpSuit = GamePlayControllerNetworked.Instance.TrumpSuit;
                    var leadSuit = GamePlayControllerNetworked.Instance.CurrentTrickSuit;

                    var bestCard = playableCards
                        .OrderByDescending(card => card.GetCardPower(trumpSuit, leadSuit))
                        .First();

                    hand.Remove(bestCard);

                    var cardString = JsonConvert.SerializeObject(new CardDataDto()
                    {
                        rank = bestCard.cardData.rank,
                        suit = bestCard.cardData.suit
                    });

                    RPC_PlayCard(PlayerIndex, cardString);
                    GamePlayControllerNetworked.Instance.currentTrickCards[PlayerIndex] = bestCard;
                    AnimateCardPlay(bestCard).Forget();
                    tcs.TrySetResult(bestCard); // No animation for auto-play
                }
            });

            return await tcs.Task;
        }



        public override async UniTask<bool> AskToAcceptTrump(Card topCard)
        {
            var popUp = UiManager.Instance.ShowPanel(UiScreenName.ChooseTrumpSuitPopup, topCard) as ChooseTrumpSuit;
    
            if (popUp != null)
            {
                var choice = await popUp.GetChoice;

                Rpc_RespondToTrump(PlayerIndex, (int)topCard.cardData.suit, choice);
                return true;
            }

            GameLogger.ShowLog("Failed to load panel", GameLogger.LogType.Error);
    
            // Still respond with false to avoid hanging the flow
            Rpc_RespondToTrump(PlayerIndex, (int)topCard.cardData.suit, 0);
            return false;
        }


        public override async UniTask<Suit> ChooseTrumpSuit(Card topCard, bool forceFullSuit = false)
        {
            var availableSuits = Enum.GetValues(typeof(Suit))
                .Cast<Suit>()
                .Where(suit => suit != topCard.cardData.suit && suit != Suit.None)
                .ToList();
            
            var popUp = UiManager.Instance.ShowPanel(UiScreenName.ChooseTrumpSuitSecondPopup, new ChooseTrumpSuitSecondTimeData
            {
                SuitsToChoose = availableSuits,
                ForceFullSuit = forceFullSuit
            }) as ChooseTrumpSuitSecondTime;
    
            if (popUp != null)
            {
                var choice = await popUp.GetChoice;

                Rpc_RespondToTrump(PlayerIndex, (int)choice.Item1, choice.Item2);
                return choice.Item1;
            }

            GameLogger.ShowLog("Failed to load panel", GameLogger.LogType.Error);
    
            // Still respond with false to avoid hanging the flow
            Rpc_RespondToTrump(PlayerIndex, 0, 0);
            return Suit.None;
        }

        public override async UniTask<Card> AskToExchangeTrumpCard(Card topKittyCard)
        {
            var selectedCardTcs = new UniTaskCompletionSource<Card>();

            foreach (var card in hand)
            {
                card.SetInteractable(true);
                card.OnCardClicked += () =>
                {
                    if (selectedCardTcs.Task.Status != UniTaskStatus.Succeeded)
                        selectedCardTcs.TrySetResult(card);
                };
            }

            var selectedCard = await selectedCardTcs.Task;

            // Cleanup input listeners
            foreach (var card in hand)
            {
                card.OnCardClicked = null;
                card.SetInteractable(false);
            }

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
            
            await AnimateCardExchange(selectedCard, topKittyCard, true);

            return topKittyCard;
        }


        public override UniTask<bool> AskToGoAlone()
        {
            throw new System.NotImplementedException();
        }

    }
}
