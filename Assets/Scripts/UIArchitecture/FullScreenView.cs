using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;


[RequireComponent(typeof(CanvasGroup))]
public abstract class FullScreenView : Views
{
    public float animationTime = 0.2f;
    private CanvasGroup _canvasGroup;
    private CancellationToken _destroyToken;

    protected void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _destroyToken = this.GetCancellationTokenOnDestroy();
    }

    public override void Show(object obj)
    {
        gameObject.SetActive(true);
        base.Show(obj);
        _canvasGroup.alpha = 0f;
        _canvasGroup.DOFade(1f, animationTime)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true); // ensures tween runs even if Time.timeScale = 0
    }

    public override async void Hide()
    {
        if (_canvasGroup == null || !gameObject) return; // Safety check

        // Create tween and store reference
        Tween fadeTween = _canvasGroup
            .DOFade(0f, animationTime)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true); // Keep running even if Time.timeScale = 0

        try
        {
            await fadeTween.AsyncWaitForCompletion();
        }
        catch
        {
            // Ignored — tween killed or object destroyed
        }

        if (this != null && gameObject != null) // Avoid null after scene unload
        {
            gameObject.SetActive(false);
            base.Hide();
        }
    }

    private void OnDestroy()
    {
        _canvasGroup?.DOKill(); // Kill all tweens related to this canvas group
    }

    public override void SetDefault()
    {
        gameObject.SetActive(false);
    }
}