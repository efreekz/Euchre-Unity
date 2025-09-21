using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Controllers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using GamePlay.Cards;
using GamePlay.Interfaces;
using GamePlay.Ui;
using Helper;
using Newtonsoft.Json;
using UnityEngine;

namespace GamePlay.Player
{
    public abstract class PlayerBase : NetworkBehaviour
    {
        [Networked] public int PlayerIndex { get; private set; }
        
        public List<Card> hand = new List<Card>();
        public List<CardData> handData = new List<CardData>();
        public PlayerElementUi PlayerElementUi { get; private set; }

        public bool IsBot => this is OnlineBot or BotPlayer;

        private static IGameController Controller
        {
            get
            {
                if (GamePlayController.Instance is not null) return GamePlayController.Instance;
                else return GamePlayControllerNetworked.Instance;
            }
        }

        private int _trickWon = 0;
        private bool _isDisabled;

        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                if (value)
                {
                    DisablePlayer();
                }
                else
                {
                    EnablePlayer();
                }
                _isDisabled = value;

            }
        }

        public int TricksWon
        {
            get => _trickWon;
            set
            {
                _trickWon = value;
                // PlayerElementUi.SetTrickCount(_trickWon);
            }
        }

        public virtual void Initialize(int index, PlayerElementUi thisPlayerElementUi)
        {
            PlayerIndex = index;
            PlayerElementUi = thisPlayerElementUi;
            PlayerElementUi.AttachPlayer(this);
        }
        
        private void EnablePlayer()
        {
            PlayerElementUi.PlayerIsDisabled(false);
        }

        private void DisablePlayer()
        {
            PlayerElementUi.PlayerIsDisabled(true);
        }

        public abstract UniTask<Card> PlayTurn(float time = 10f);
        public abstract UniTask<bool> AskToAcceptTrump(Card topCard);
        public abstract UniTask<Suit> ChooseTrumpSuit(Card topCard, bool forceFullSuit = false); 
        public abstract UniTask<Card> AskToExchangeTrumpCard(Card topKittyCard);
        public abstract UniTask<bool> AskToGoAlone();
        
        public async UniTask AddCardToHandUI(Card card, float animationTime)
        {
            var moveSequence = DOTween.Sequence();
            moveSequence.Append(card.transform.DOMove(PlayerElementUi.handTransform.position, animationTime));
            moveSequence.Join(card.transform.DORotate(PlayerElementUi.handTransform.rotation.eulerAngles, animationTime));

            await moveSequence.AsyncWaitForCompletion();
            card.transform.SetParent(PlayerElementUi.handTransform);
        }
        public async UniTask AddCardToWinDeckUI(Card[] cards, float animationTime)
        {
            List<Task> animationTasks = new List<Task>();

            foreach (var card in cards)
            { 
                var targetPos = PlayerElementUi.winDeckTransform.position;
                var targetRot = PlayerElementUi.winDeckTransform.rotation.eulerAngles;

                var moveTween = card.transform.DOMove(targetPos, animationTime);
                var rotateTween = card.transform.DORotate(targetRot, animationTime);
                animationTasks.Add(moveTween.AsyncWaitForCompletion());
                animationTasks.Add(rotateTween.AsyncWaitForCompletion());
                card.SetFaceUp(false);
            }

            await Task.WhenAll(animationTasks);

            foreach (var card in cards)
            {
                card.transform.SetParent(PlayerElementUi.winDeckTransform);

                card.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                card.rectTransform.anchoredPosition3D = Vector3.zero; // Immediately reset
            }
        }

        public void ReassignHand(List<CardData> overrideCards)
        {
            if (overrideCards.Count != hand.Count)
            {       
                GameLogger.ShowLog("Can Not Assign Hand Cards Because Hand Card Counts Are Not Equal", GameLogger.LogType.Error);
                return;
            }

            var i = 0;
            foreach (var card in hand)
            {
                card.SetCardData(overrideCards[i]);
                i++;
            }
        }

        public void RevealHand(bool showFaceUp)
        {
            foreach (var card in hand)
            {
                card.SetFaceUp(showFaceUp);
                // card.SetInteractable(true);
            }
        }
        
        protected Card[] CheckPlayableCards()
        {
            var currentSuit = Controller.CurrentTrickSuit;
            var trumpSuit = Controller.TrumpSuit;
            var playable = new List<Card>();

            if (currentSuit == Suit.None)
            {
                playable.AddRange(hand);
                return playable.ToArray();
            }

            var hasMatchingSuit = hand.Any(card => GetEffectiveSuit(card.cardData) == currentSuit);

            foreach (var card in hand)
            {
                var effectiveSuit = GetEffectiveSuit(card.cardData);

                if (hasMatchingSuit)
                {
                    if (effectiveSuit == currentSuit)
                        playable.Add(card);
                }
                else
                {
                    playable.Add(card);
                }
            }

            return playable.ToArray();

            Suit GetEffectiveSuit(CardData cardData)
            {
                return cardData.rank switch
                {
                    Rank.Jack when cardData.suit == trumpSuit => trumpSuit,
                    Rank.Jack when IsSameColor(cardData.suit, trumpSuit) => trumpSuit,
                    _ => cardData.suit
                };
            }

            bool IsSameColor(Suit suit1, Suit suit2)
            {
                return suit1 is Suit.Clubs or Suit.Spades &&
                       suit2 is Suit.Clubs or Suit.Spades
                       ||
                       suit1 is Suit.Hearts or Suit.Diamonds &&
                       suit2 is Suit.Hearts or Suit.Diamonds;
            }
        }


        public async UniTask SendMessageToUi(string message)
        {
            await PlayerElementUi.ShowMessage(message);
        }
        
        public void ResetUiElement()
        {
            PlayerElementUi.Reset();
        }

        public void SetHimDealer(bool isDealer)
        {
            PlayerElementUi.SetDealer(isDealer);
        }
        
        public void UpdateUiOnPlayerTurn()
        {
            PlayerElementUi.PlayTurn();
        }

        public void UpdateUiOnEndTurn()
        {
            PlayerElementUi.EndTurn();
        }
        
        
        #region Animation

        public async UniTask AnimateCardExchange(Card oldCard, Card newCard, bool isLocalPlayer)
        {
            var handTransform = PlayerElementUi.handTransform;
            var originalTransform = newCard.transform;

            // Set face-up for the new card early so it's ready visually
            newCard.SetFaceUp(isLocalPlayer);
            // Move and rotate the new card into the hand
            var newCardMove = newCard.transform.DOMove(handTransform.position, 0.5f).AsyncWaitForCompletion();
            var newCardRotate = newCard.transform.DORotate(handTransform.rotation.eulerAngles, 0.5f).AsyncWaitForCompletion();

            // Prepare the old card to be removed
            oldCard.SetFaceUp(false);
            // Move and rotate the old card away at the same time
            var oldCardMove = oldCard.transform.DOMove(originalTransform.position, 0.5f).AsyncWaitForCompletion();
            var oldCardRotate = oldCard.transform.DORotate(originalTransform.rotation.eulerAngles, 0.5f).AsyncWaitForCompletion();
            
            // Wait for all animations to finish together
            await Task.WhenAll(newCardMove, newCardRotate, oldCardMove, oldCardRotate);

            oldCard.transform.SetParent(PlayerElementUi.Deck);
            newCard.transform.SetParent(handTransform);

            // Optional short delay for pacing
            await UniTask.Delay(500);
        }
        public async UniTask AnimateCardExchange(Card newCard)
        {
            var handTransform = PlayerElementUi.handTransform;
            var originalTransform = newCard.transform;

            // Set face-up for the new card early so it's ready visually
            newCard.SetFaceUp(false);
            // Move and rotate the new card into the hand
            var newCardMove = newCard.transform.DOMove(handTransform.position, 0.5f).AsyncWaitForCompletion();
            var newCardRotate = newCard.transform.DORotate(handTransform.rotation.eulerAngles, 0.5f).AsyncWaitForCompletion();

            var oldCard = hand.GetRandom();
            // Move and rotate the old card away at the same time
            var oldCardMove = oldCard.transform.DOMove(originalTransform.position, 0.5f).AsyncWaitForCompletion();
            var oldCardRotate = oldCard.transform.DORotate(originalTransform.rotation.eulerAngles, 0.5f).AsyncWaitForCompletion();

            // Wait for all animations to finish together
            await Task.WhenAll(newCardMove, newCardRotate, oldCardMove, oldCardRotate);

            oldCard.transform.SetParent(PlayerElementUi.Deck);
            newCard.transform.SetParent(handTransform);
            hand.Remove(oldCard);
            hand.Add(newCard);

            // Optional short delay for pacing
            await UniTask.Delay(500);
        }

        protected async UniTask AnimateCardPlay(Card card)
        {
            if (card == null) return;

            card.SetFaceUp(true);
            card.transform.SetParent(PlayerElementUi.playedCardTransform);

            // Start tweens
            Tween moveTween = card.transform
                .DOMove(PlayerElementUi.playedCardTransform.position, 0.3f)
                .SetEase(Ease.InOutQuad);

            Tween rotateTween = card.transform
                .DORotate(PlayerElementUi.playedCardTransform.rotation.eulerAngles, 0.3f)
                .SetEase(Ease.OutSine);

            // Wait for both tweens to complete
            await Task.WhenAll(moveTween.AsyncWaitForCompletion(), rotateTween.AsyncWaitForCompletion());
        }

        #endregion


        #region Testing

        [Header("Testing")] [SerializeField] private bool showLog;

        protected void ShowLog(string message)
        {
            if (showLog)
                GameLogger.ShowLog(message);
        }


        #endregion

        #region RPCs
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_NotifyInitialized()
        {
            GamePlayControllerNetworked.Instance.SetPlayerInitialized(PlayerIndex);
        }
        
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        protected void Rpc_RespondToTrump(int playerIndex, int suit, int choice)
        {
            ShowLog($"[{gameObject.name}] {playerIndex} responded: choice = {choice}");

            GamePlayControllerNetworked.Instance.TrumpAcceptedTcs.TrySetResult(new TrumpSelectionData(playerIndex, (Suit)suit, choice));
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        protected void RPC_ExchangeTrumpCard(string selectedCardJson, string topKittyCardJson)
        {
            var selectedCardDataDto = JsonConvert.DeserializeObject<CardDataDto>(selectedCardJson);
            var topKittyCardDataDto = JsonConvert.DeserializeObject<CardDataDto>(topKittyCardJson);
            var dealer = GamePlayControllerNetworked.Instance.playerManager.GetDealerPlayer();
            var topKittyCard = GamePlayControllerNetworked.Instance.TopKittyCard;
            
            ShowLog($"DealerIndex : {dealer.PlayerIndex}\nLocalPlayer Index : {GamePlayControllerNetworked.Instance.playerManager.GetLocalPlayerBase().PlayerIndex}");

            if (HasStateAuthority)
            {
                GamePlayControllerNetworked.Instance.CardExchangedTcs.TrySetResult();
            }
            
            if (HasInputAuthority)
            {
                return;
            }

            if (dealer.PlayerIndex != GamePlayControllerNetworked.Instance.playerManager.GetLocalPlayerBase().PlayerIndex)
            {
                dealer.AnimateCardExchange(topKittyCard).Forget();
            }

        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        protected void RPC_PlayCard(int playerIndex, string cardString)
        {
            var selectedCardDataDto = JsonConvert.DeserializeObject<CardDataDto>(cardString);
            var cardData = GamePlayControllerNetworked.Instance.cardsController.ToCardData(selectedCardDataDto);
            var playerBase = GamePlayControllerNetworked.Instance.playerManager.GetPlayer(playerIndex);
            
            ShowLog($"[{gameObject.name}] {playerBase.PlayerIndex} played card {cardData.rank} of {cardData.suit}");
            
            if (HasStateAuthority)
            {
                GamePlayControllerNetworked.Instance.CardPlayedTcs.TrySetResult(cardData);
            }
            
            
            if (HasInputAuthority)
            {
                return;
            }

            HelperPlayCard(playerBase, cardData).Forget();
        }

        #region Helper RPC

        private async UniTask HelperPlayCard(PlayerBase playerBase, CardData cardData)
        {
            var card = hand.GetRandom();
            card.SetCardData(cardData);
            GamePlayControllerNetworked.Instance.currentTrickCards[playerBase.PlayerIndex] = card;
            await playerBase.AnimateCardPlay(card);
            hand.Remove(card);
        }

        #endregion
        
        #endregion

    }
}