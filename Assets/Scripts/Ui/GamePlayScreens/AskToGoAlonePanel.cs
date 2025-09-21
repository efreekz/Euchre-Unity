using System.Threading.Tasks;
using UIArchitecture;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.GamePlayScreens
{
    public class AskToGoAlonePanel : PopUpView
    {
        public Button yesButton;
        public Button noButton;
        
        private TaskCompletionSource<bool> _choiceTaskSource;
        
        public Task<bool> GetChoice => _choiceTaskSource.Task;
        
        protected override void Initialize(Object obj)
        {
            _choiceTaskSource = new TaskCompletionSource<bool>();
            yesButton.onClick.AddListener(OnClickYes);
            noButton.onClick.AddListener(OnClickNo);
        }

        protected override void Cleanup()
        {
            yesButton.onClick.RemoveAllListeners();
            noButton.onClick.RemoveAllListeners();
        }
        
        private void OnClickYes()
        {
            _choiceTaskSource?.TrySetResult(true);
            UiManager.Instance.HidePanel(this);
        }

        private void OnClickNo()
        {
            _choiceTaskSource?.TrySetResult(false);
            UiManager.Instance.HidePanel(this);
        }
    }
}