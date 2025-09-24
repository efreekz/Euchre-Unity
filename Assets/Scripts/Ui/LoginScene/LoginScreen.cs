using System;
using Controllers;
using Cysharp.Threading.Tasks;
using Data;
using Managers;
using Network;
using TMPro;
using UIArchitecture;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.LoginScene
{
    public class LoginScreen : FullScreenView
    {
        public TMP_InputField emailInput;
        public TMP_InputField passwordInput;
        public Button loginButton;
        public Button signUpButton;

        protected override void Initialize(Object obj)
        {
            emailInput.text = string.Empty;
            passwordInput.text = string.Empty;

            loginButton.onClick.AddListener(OnClickLogin);
            signUpButton.onClick.AddListener(OnClickSignUp);
        }


        protected override void Cleanup()
        {
            emailInput.text = string.Empty;
            passwordInput.text = string.Empty;
            
            loginButton.onClick.RemoveAllListeners();
            signUpButton.onClick.RemoveAllListeners();
        }

        private void OnClickSignUp()
        {
            UiManager.Instance.ShowPanel(UiScreenName.SignUpScreen, null);
        }

        private async void OnClickLogin()
        {
            if (!VerifyLoginFields()) return;

            var waitPanel = UiManager.Instance.ShowPanel(UiScreenName.WaitingPanel, null);
            try
            {
                await AuthManager.Instance.Login(
                    emailInput.text,
                    passwordInput.text,
                    GameManager.OnSucessfullLogin,
                    GameManager.OnLoginFailed
                );
            }
            catch (Exception exception)
            {
                GameLogger.LogNetwork(exception.Message);
            }
            
            UiManager.Instance.HidePanel(waitPanel);
        }

        private bool VerifyLoginFields()
        {
            var email = emailInput.text.Trim();
            var password = passwordInput.text;

            if (string.IsNullOrEmpty(email))
            {
                UiManager.Instance.ShowToast("Email is required.");
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                UiManager.Instance.ShowToast("Password is required.");
                return false;
            }

            return true;
        }

    }
}
