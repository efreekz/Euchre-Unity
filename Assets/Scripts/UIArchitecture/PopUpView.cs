using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class PopUpView : Views
{
    [Header("Animation Variables")] 
    public GameObject targetToMove;
    public Transform moveFrom;
    public Transform moveTo;
    public Image bg;

    
    public override void Show(object obj)
    {
        gameObject.SetActive(true);
        AnimatePanelOpen();
        base.Show(obj);
    }

    public override void Hide()
    {
        StartCoroutine(OnCloseAnimationComplete());
        AnimatePanelClose();
    }

    public override void SetDefault()
    {
        gameObject.SetActive(false);
        ResetPanelToDefault();
    }

    private void AnimatePanelOpen()
    {
        // Set initial position and transparency
        targetToMove.transform.position = moveFrom.position;
        var color = bg.color;
        color.a = 0f;
        bg.color = color;

        // Move the panel to the target position with a spring effect
        iTween.MoveTo(targetToMove, iTween.Hash(
            "position", moveTo.position, // Target position
            "time", 0.5f, // Duration
            "easetype", iTween.EaseType.spring // Ease type
        ));

        // Fade in the background
        iTween.ValueTo(gameObject, iTween.Hash(
            "from", 0f, // Starting alpha
            "to", 0.6f, // Target alpha
            "time", 0.75f, // Duration
            "easetype", iTween.EaseType.easeInOutQuad, // Ease type
            "onupdate", "UpdateAlpha" // Method to update alpha
        ));
    }

    private void AnimatePanelClose()
    {
        // Move the panel back to the original position
        iTween.MoveTo(targetToMove, iTween.Hash(
            "position", moveFrom.position, // Move back to initial position
            "time", 0.5f, // Duration
            "easetype", iTween.EaseType.easeInOutQuad // Ease type
        ));

        // Fade out the background
        iTween.ValueTo(gameObject, iTween.Hash(
            "from", 0.6f, // Current alpha
            "to", 0f, // Target alpha
            "time", 0.5f, // Duration
            "easetype", iTween.EaseType.easeInOutQuad, // Ease type
            "onupdate", "UpdateAlpha" // Method to update alpha
        ));
    }

    private void UpdateAlpha(float newAlpha)
    {
        var color = bg.color;
        color.a = newAlpha;
        bg.color = color;
    }

    private void ResetPanelToDefault()
    {
        // Reset position
        targetToMove.transform.position = moveFrom.position;

        // Reset alpha
        var color = bg.color;
        color.a = 0f;
        bg.color = color;
    }

    private IEnumerator OnCloseAnimationComplete()
    {
        yield return new WaitForSeconds(0.5f);
        ResetPanelToDefault();
        base.Hide();
        gameObject.SetActive(false);
    }
}

