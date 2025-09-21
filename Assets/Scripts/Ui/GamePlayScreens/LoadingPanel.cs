using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.GamePlayScreens
{
    public class LoadingPanel : FullScreenView
    {
        [SerializeField] private Image loadingImage;
        [SerializeField] private int totalFrames = 8;
        [SerializeField] private float waitPerFrame = 0.1f;

        private Tween _rotationTween;
        
        protected override void Initialize(Object obj)
        {
            StartSteppedRotation();
        }

        protected override void Cleanup()
        {
            _rotationTween?.Kill();
            _rotationTween = null;
        }
        
        private void StartSteppedRotation()
        {
            float stepAngle = 360f / totalFrames;
            int currentStep = 0;

            _rotationTween = DOTween.Sequence()
                .AppendCallback(() =>
                {
                    currentStep = (currentStep + 1) % totalFrames;
                    float targetZ = currentStep * stepAngle;
                    loadingImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -targetZ);
                })
                .AppendInterval(waitPerFrame)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true); // Optional if you want it to run while game is paused
        }

    }
}
