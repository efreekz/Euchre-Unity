using UIArchitecture;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.LoginScene
{
    public class LoginHomeScreen : FullScreenView
    {
        public Button loginButton;
        public Button signUpButton;
        public Button infoButton;
        
        
        protected override void Initialize(Object obj)
        {
            loginButton.onClick.AddListener(OnClickLoginButton);
            signUpButton.onClick.AddListener(OnClickSignUpButton);
            infoButton.onClick.AddListener(OnClickInfoButton);
        }

        protected override void Cleanup()
        {
            loginButton.onClick.RemoveAllListeners();
            signUpButton.onClick.RemoveAllListeners();
            signUpButton.onClick.RemoveAllListeners();
        }
        
        
        private void OnClickInfoButton()
        {
            UiManager.Instance.ShowPanel(UiScreenName.LoginInfoScreen, null);
        }

        private void OnClickSignUpButton()
        {
            UiManager.Instance.ShowPanel(UiScreenName.SignUpScreen, null);
        }

        private void OnClickLoginButton()
        {
            UiManager.Instance.ShowPanel(UiScreenName.LoginScreen, null);
        }

    }
}
