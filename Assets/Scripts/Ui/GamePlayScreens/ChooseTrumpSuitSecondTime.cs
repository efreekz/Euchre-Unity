using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Controllers;
using DG.Tweening;
using GamePlay;
using GamePlay.Cards;
using TMPro;
using UIArchitecture;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.GamePlayScreens
{
    public class ChooseTrumpSuitSecondTime : PopUpView
    {
        [System.Serializable]
        private class SuitButton
        {
            public Toggle toggle;
            public Image image;
            public Suit suit;
        }

        [SerializeField] private Button passButton;
        [SerializeField] private Button orderUpButton;
        [SerializeField] private Button orderUpAndGoAloneButton;
        [SerializeField] private SuitButton[] suitButtons;
        [SerializeField] private Slider timer;
        [SerializeField] private float time = 30f;

        private ChooseTrumpSuitSecondTimeData _data;
        private TaskCompletionSource<(Suit, int)> _choiceTaskSource;
        public Task<(Suit, int)> GetChoice => _choiceTaskSource.Task;

        private Suit _selectedSuit;
        private Coroutine _timeoutCoroutine;
        private float _timeToPass;
        private Tweener _shakeTween;

        protected override void Initialize(Object data)
        {
            if (data is ChooseTrumpSuitSecondTimeData suitData)
            {
                _data = suitData;
                _choiceTaskSource = new TaskCompletionSource<(Suit, int)>();

                int suitCount = Mathf.Min(suitButtons.Length, _data.SuitsToChoose.Count);

                var cardController = GamePlayController.Instance is not null
                    ? GamePlayController.Instance.cardsController
                    : GamePlayControllerNetworked.Instance.cardsController;

                for (int i = 0; i < suitButtons.Length; i++)
                {
                    var suitButton = suitButtons[i];
                    if (i < suitCount)
                    {
                        var suit = _data.SuitsToChoose[i];
                        suitButton.toggle.gameObject.SetActive(true);
                        suitButton.image.sprite = cardController.GetSuitData(suit).sprite;
                        suitButton.suit = suit;

                        int index = i;
                        suitButton.toggle.onValueChanged.AddListener(isOn =>
                        {
                            if (isOn)
                            {
                                _selectedSuit = suitButtons[index].suit;
                            }
                        });
                    }
                    else
                    {
                        suitButtons[i].toggle.gameObject.SetActive(false);
                    }
                }

                if (suitCount > 0)
                {
                    suitButtons[0].toggle.isOn = true;
                    _selectedSuit = suitButtons[0].suit;
                }

                passButton.gameObject.SetActive(!_data.ForceFullSuit);

                // ✅ Start timeout coroutine
                _timeoutCoroutine = StartCoroutine(TimerCoroutine());
            }
            else
            {
                Debug.LogError("Data is not in expected format");
            }

            passButton.onClick.AddListener(OnClickPassButton);
            orderUpButton.onClick.AddListener(OnClickOrderItUpButton);
            orderUpAndGoAloneButton.onClick.AddListener(OnClickOrderItUpAndGoAloneButton);
        }

        protected override void Cleanup()
        {
            _shakeTween?.Kill();

            passButton.onClick.RemoveAllListeners();
            orderUpButton.onClick.RemoveAllListeners();
            orderUpAndGoAloneButton.onClick.RemoveAllListeners();

            foreach (var suitButton in suitButtons)
            {
                suitButton.toggle.onValueChanged.RemoveAllListeners();
            }

            if (_timeoutCoroutine != null)
                StopCoroutine(_timeoutCoroutine);
        }

        private void OnClickPassButton()
        {
            _choiceTaskSource?.TrySetResult((Suit.None, 0));
            UiManager.Instance.HidePanel(this);
        }

        private void OnClickOrderItUpButton()
        {
            _choiceTaskSource?.TrySetResult((_selectedSuit, 1));
            UiManager.Instance.HidePanel(this);
        }

        private void OnClickOrderItUpAndGoAloneButton()
        {
            _choiceTaskSource?.TrySetResult((_selectedSuit, 2));
            UiManager.Instance.HidePanel(this);
        }

        private IEnumerator TimerCoroutine()
        {
            _timeToPass = time;
            
            // Start shaking the timer using DOTween on localPosition
            _shakeTween = timer.transform
                .DOShakePosition(duration: time, strength: new Vector3(5f, 5f, 0), vibrato: 20, randomness: 90, snapping: false, fadeOut: false)
                .SetRelative()
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetEase(Ease.Linear);

            while (_timeToPass > 0)
            {
                _timeToPass -= Time.deltaTime;
                timer.value = Mathf.Clamp01(_timeToPass / time);
                yield return null;
            }
            
            yield return null;
            if (_data.ForceFullSuit)
                OnClickOrderItUpButton();
            else
                OnClickPassButton();
        }
    }

    public struct ChooseTrumpSuitSecondTimeData
    {
        public List<Suit> SuitsToChoose;
        public bool ForceFullSuit;
    }
}
