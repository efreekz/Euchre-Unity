using System;
using Controllers;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using Data;
using Managers;
using Network;
using UIArchitecture;

namespace Ui.LoginScene
{
    public class SignUpScreen : FullScreenView
    {
        public TMP_InputField emailInput;
        public TMP_InputField usernameInput;
        public TMP_InputField passwordInput;
        public TMP_InputField confirmPasswordInput;
        public TMP_InputField referralInput;
        public Toggle ageToggle;
        public Button backButton;
        public Button signUpButton;

        protected override void Initialize(object obj)
        {
            confirmPasswordInput.text = string.Empty;
            emailInput.text = string.Empty;
            usernameInput.text = string.Empty;
            passwordInput.text = string.Empty;
            referralInput.text = string.Empty;
            ageToggle.isOn = false;

            signUpButton.onClick.AddListener(OnClickSignUp);
            backButton.onClick.AddListener(OnClickBack);
        }

        protected override void Cleanup()
        {
            confirmPasswordInput.text = string.Empty;
            emailInput.text = string.Empty;
            usernameInput.text = string.Empty;
            passwordInput.text = string.Empty;
            referralInput.text = string.Empty;
            ageToggle.isOn = false;
            
            signUpButton.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
        }
        
        private async void OnClickSignUp()
        {
            if (!VerifySignupFields()) return;
            
            try
            {
                UiManager.Instance.ShowPanel(UiScreenName.WaitingPanel, null);
                // await AuthManager.Instance.SignUp(emailInput.text, passwordInput.text, referralInput.text, ageToggle.isOn, 
                //     LoginSceneController.Instance.OnSucessfullLogin, 
                //     LoginSceneController.Instance.OnLoginFailed);

                await PlayfabManager.Instance.SignUpWithInvitation(emailInput.text, passwordInput.text,
                    usernameInput.text, referralInput.text, GameManager.OnSucessfullLogin,
                    GameManager.OnLoginFailed);
            }
            catch (Exception e)
            {
                GameLogger.LogNetwork(e.Message);
            }
        }

        private bool VerifySignupFields()
        {
            if (string.IsNullOrWhiteSpace(confirmPasswordInput.text) ||
                string.IsNullOrWhiteSpace(emailInput.text) ||
                string.IsNullOrWhiteSpace(passwordInput.text) ||
                string.IsNullOrWhiteSpace(referralInput.text) ||
                string.IsNullOrWhiteSpace(usernameInput.text))
            {
                UiManager.Instance.ShowToast("All fields are required.");
                return false;
            }

            if (!emailInput.text.Contains("@") || !emailInput.text.Contains("."))
            {
                UiManager.Instance.ShowToast("Please enter a valid email address.");
                return false;
            }

            if (passwordInput.text.Length < 6)
            {
                UiManager.Instance.ShowToast("Password must be at least 6 characters.");
                return false;
            }

            if (passwordInput.text != confirmPasswordInput.text)
            {
                UiManager.Instance.ShowToast("Passwords do not match.");
                return false;
            }

            if (!ageToggle.isOn)
            {
                UiManager.Instance.ShowToast("Please Confirm that you are above 18 years old.");
                return false;
            }

            return true;
        }

        private void OnClickBack()
        {
            UiManager.Instance.HidePanel(this);
        }

    }
}
