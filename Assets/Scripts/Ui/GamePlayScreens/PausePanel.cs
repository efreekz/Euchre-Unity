using Cysharp.Threading.Tasks;
using Managers;
using UIArchitecture;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.GamePlayScreens
{
    public class PausePanel : PopUpView
    {
        public Button leaveGameButton;
        public Button resumeGameButton;

        protected override void Initialize(Object obj)
        {
            leaveGameButton.onClick.AddListener(OnLeaveGame);
            resumeGameButton.onClick.AddListener(OnResumeGame);
        }

        private void OnResumeGame()
        {
            UiManager.Instance.HidePanel(this);
        }

        private void OnLeaveGame()
        {
            GameManager.LoadScene(SceneName.MainMenu);
            MultiplayerManager.Instance.ShutDown().Forget();
        }

        protected override void Cleanup()
        {
            leaveGameButton.onClick.RemoveAllListeners();
            resumeGameButton.onClick.RemoveAllListeners();
        }
    }
}
