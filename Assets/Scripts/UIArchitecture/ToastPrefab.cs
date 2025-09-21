using TMPro;
using UnityEngine;

namespace UIArchitecture
{
    public class ToastPrefab : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float timeToDelete = 1f;

        // Animation
        [SerializeField] private GameObject toastParent;
        [SerializeField] private Transform moveFrom;
        [SerializeField] private Transform moveTo;
        [SerializeField] private float animationTime = 0.5f;
        // [SerializeField] private float waitBeforeReturn = 0.6f;

        public void Init(string message)
        {
            text.text = message;
            AnimateToast();
            Destroy(gameObject, timeToDelete);
        }
        private void AnimateToast()
        {
            // Move to target position
            iTween.MoveTo(toastParent, iTween.Hash(
                "position", moveTo.position,
                "time", animationTime,
                "easetype", iTween.EaseType.easeInOutSine,
                "oncomplete", "MoveBack",
                "oncompletetarget", gameObject // Ensure it calls MoveBack on this script
            
            ));
        }

        private void MoveBack()
        {
            // Move back to original position
            iTween.MoveTo(toastParent, iTween.Hash(
                "position", moveFrom.position,
                "time", animationTime,
                "easetype", iTween.EaseType.easeInOutSine,
                "delay", 1.0f // Delay before MoveBack is called
            ));
        }

    }
}