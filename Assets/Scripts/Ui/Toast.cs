using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace Ui
{
    public class Toast : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float timeToWait = 5f;
        [SerializeField] private float timeToShow = 0.1f;
        [SerializeField] private RectTransform parent;
        [SerializeField] private RectTransform moveToPosition;
        [SerializeField] private RectTransform moveFromPosition;

        public void Initialize(string message)
        {
            text.text = message;
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            PlayToastAnimation();
        }

        private void PlayToastAnimation()
        {
            // Start at the "from" position
            parent.position = moveFromPosition.position;

            // Sequence for sliding in, waiting, sliding out, then destroying
            Sequence toastSequence = DOTween.Sequence();

            toastSequence
                // Slide in
                .Append(parent.DOMove(moveToPosition.position, timeToShow).SetEase(Ease.OutCubic))
                // Wait
                .AppendInterval(timeToWait)
                // Slide out
                .Append(parent.DOMove(moveFromPosition.position, timeToShow).SetEase(Ease.InCubic))
                // Destroy after animation
                .OnComplete(() => Destroy(gameObject));
        }
    }
}