using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GamePlay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GamePlay.Ui
{
    public class PlayerElementUi : MonoBehaviour
    {
        public Transform handTransform;
        public Transform playedCardTransform;
        public Transform winDeckTransform;
        public Transform wonTrickParent;
        public Transform disabledParent;
        public RectTransform trumpHolder;
        public TMP_Text wonTrickCount;
        public TMP_Text idText;

        public GameObject turnObject;
        public GameObject dealerObject;
        public RectTransform messageParent;
        public TMP_Text messageText;
        public int messageTime = 2;

        private CancellationTokenSource _cancellationToken;
        private void Awake()
        {
            _cancellationToken = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _cancellationToken?.Cancel();
            _cancellationToken?.Dispose();
        }

        public Transform Deck
        {
            get;
            private set;
        }
        public void Init(Transform deckTransform)
        {
            Deck = deckTransform;
            wonTrickCount.text = "";
        }

        public void Reset()
        {
            trumpHolder.gameObject.SetActive(false);
            // for (int i = 0; i < handTransform.childCount; i++)
            // {
            //     Destroy(handTransform.GetChild(i).gameObject);
            // }
            // for (int i = 0; i < playedCardTransform.childCount; i++)
            // {
            //     Destroy(playedCardTransform.GetChild(i).gameObject);
            // }
            // for (int i = 0; i < winDeckTransform.childCount; i++)
            // {
            //     Destroy(winDeckTransform.GetChild(i).gameObject);
            // }
        }

        public void PlayerIsDisabled(bool isDisabled)
        {
            handTransform.gameObject.SetActive(!isDisabled);
            disabledParent.gameObject.SetActive(isDisabled);
        }
        

        public async UniTask ShowMessage(string message)
        {
            messageText.text = message;
            messageParent.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageParent);

            await UniTask.Delay(messageTime * 1000, cancellationToken: _cancellationToken.Token);

            messageParent.gameObject.SetActive(false);
        }

        public void AttachPlayer(PlayerBase player)
        {
            var playerName = player.IsBot ? $"Bot {player.PlayerIndex + 1}" : $"Player {player.PlayerIndex + 1}";
            idText.text = playerName;
        }


        public void PlayTurn()
        {
            turnObject.SetActive(true);
        }

        public void EndTurn()
        {
            turnObject.SetActive(false);
        }

        public void SetDealer(bool enable)
        {
            dealerObject.SetActive(enable);
        }

        // public void SetTrickCount(int trickWon)
        // {
        //     if (trickWon > 0)
        //     {
        //         wonTrickParent.gameObject.SetActive(true);
        //         wonTrickCount.text = trickWon.ToString();
        //
        //         // Reset scale first to avoid stacking
        //         wonTrickParent.transform.localScale = Vector3.one;
        //
        //         // Punch the scale for emphasis
        //         wonTrickParent.transform
        //             .DOScale(1.2f, 0.2f)
        //             .SetEase(Ease.OutBack)
        //             .OnComplete(() =>
        //                 wonTrickParent.transform.DOScale(1f, 0.2f).SetEase(Ease.InQuad));
        //     }
        //     else
        //     {
        //         wonTrickParent.gameObject.SetActive(false);
        //     }
        // }

        public Slider turnTimer;
        public void StartTurnTimer(float duration, CancellationToken token)
        {
            turnTimer.gameObject.SetActive(true);
            turnTimer.maxValue = duration;
            turnTimer.value = duration;

            DOTween.Kill(turnTimer); // Stop any previous animations

            // Animate the slider value from duration to 0
            var tween = DOTween.To(() => duration, x => turnTimer.value = x, 0, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => turnTimer.gameObject.SetActive(false))
                .SetId(turnTimer);

            // Cancel tween if player plays early
            token.Register(() =>
            {
                tween.Kill();
                turnTimer.gameObject.SetActive(false);
            });
        }


    }
}
