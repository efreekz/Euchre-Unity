using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Helper
{
    public class LoadingAnimation : MonoBehaviour
    {
        [Header("Tween Settings")]
        [SerializeField] private float rotationAmount = 360f;   // How much to rotate (in degrees)
        [SerializeField] private float duration = 2f;           // Time for one rotation
        [SerializeField] private int loops = -1;                // -1 = infinite
        [SerializeField] private LoopType loopType = LoopType.Restart; // Restart, Yoyo, Incremental
        [SerializeField] private Ease ease = Ease.Linear;       // Easing type

        private Tween _rotationTween;

        private void OnEnable()
        {
            StartRotation();
        }

        private void OnDisable()
        {
            StopRotation();
        }

        private void StartRotation()
        {
            _rotationTween?.Kill();

            _rotationTween = transform
                .DORotate(new Vector3(0, 0, rotationAmount), duration, RotateMode.FastBeyond360)
                .SetEase(ease)
                .SetLoops(loops, loopType);
        }

        private void StopRotation()
        {
            _rotationTween?.Kill();
        }
    }
}
