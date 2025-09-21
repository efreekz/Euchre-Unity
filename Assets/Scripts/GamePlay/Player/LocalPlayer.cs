using System;
using System.Linq;
using Controllers;
using Cysharp.Threading.Tasks;
using GamePlay.Cards;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;

namespace GamePlay.Player
{
    public class LocalPlayer : PlayerBase
    {
        public override async UniTask<Card> PlayTurn(float time = 10f)
        {
            if (IsDisabled) return null;

            PlayerElementUi.PlayTurn();

            foreach (var card in hand)
            {
                card.SetInteractable(false);
                card.OnCardClicked = null;
            }

            var playableCards = CheckPlayableCards();
            var tcs = new UniTaskCompletionSource<Card>();

            foreach (var card in playableCards)
            {
                card.SetInteractable(true);
                card.OnCardClicked += async () =>
                {
                    // Disable further input
                    foreach (var c in playableCards)
                    {
                        c.OnCardClicked = null;
                    }

                    if (GamePlayController.Instance.CurrentTrickSuit == Suit.None)
                        GamePlayController.Instance.CurrentTrickSuit = card.cardData.suit;

                    hand.Remove(card);

                    await AnimateCardPlay(card);
                    PlayerElementUi.EndTurn();
                    tcs.TrySetResult(card);
                };
            }

            return await tcs.Task;
        }
        public override async UniTask<bool> AskToAcceptTrump(Card topCard)
        {
            var popUp = UiManager.Instance.ShowPanel(UiScreenName.ChooseTrumpSuitPopup, topCard) as ChooseTrumpSuit;
            // if (popUp != null)
            // {
            //     var choice = await popUp.GetChoice;
            //     if (choice)
            //     {
            //         await SendMessageToUi($"{topCard.cardData.suit} selected as Trump");
            //     }
            //     else
            //     {
            //         await SendMessageToUi("Pass");
            //     }
            //     return choice;
            // }

            Debug.LogError("Failed to load panel");
            return false;
        }
        public override async UniTask<Suit> ChooseTrumpSuit(Card topCard, bool forceFullSuit = false)
        {
            // Show UI with suit options
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
                if (choice.Item1 != Suit.None)
                {
                    await SendMessageToUi($"{choice} selected as Trump");
                }
                else
                {
                    await SendMessageToUi("Pass");
                }
                return choice.Item1;
            }

            Debug.LogError("Failed to load panel");
            return Suit.None;
        }

        public override async UniTask<Card> AskToExchangeTrumpCard(Card topKittyCard)
        {
            var selectedCardTcs = new UniTaskCompletionSource<Card>();
            _ = SendMessageToUi("Choose a Card To Discard");

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

            foreach (var card in hand)
                card.OnCardClicked = null;

            hand.Remove(selectedCard);
            hand.Add(topKittyCard);

            await AnimateCardExchange(selectedCard, topKittyCard, true);

            return topKittyCard;
        }
        
        public override async UniTask<bool> AskToGoAlone()
        {
            var popUp = UiManager.Instance.ShowPanel(UiScreenName.AskToGoAlonePopup, null) as AskToGoAlonePanel;

            if (popUp != null)
            {
                var choice = await popUp.GetChoice;
                if (choice)
                {
                    await SendMessageToUi("I Will Go Alone");
                }
                return choice;
            }

            Debug.LogError("Failed to load panel");
            return false;
        }

    }
}