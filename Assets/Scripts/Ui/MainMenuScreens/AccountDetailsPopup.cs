using Data;
using Managers;
using Network;
using TMPro;
using UIArchitecture;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.MainMenuScreens
{
    public class AccountDetailsPopup : PopUpView
    {
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text emailText;
        [SerializeField] private TMP_Text userIDText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_InputField newPassword;
        [SerializeField] private TMP_InputField confirmPassword;
        [SerializeField] private Button updatePasswordButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private Button closeButton;

        private UserData _user;
        
        protected override void Initialize(Object obj)
        {
            if (PlayfabManager.Instance.CurrentUserData != null)
                _user = PlayfabManager.Instance.CurrentUserData;
            else
                UiManager.Instance.HidePanel(this);

            usernameText.text = _user.username;
            emailText.text = _user.email;
            userIDText.text = _user.id.ToString();
            newPassword.text = "";
            confirmPassword.text = "";
            updatePasswordButton.onClick.AddListener(OnClickUpdatePassword);
            logoutButton.onClick.AddListener(OnClickLogout);
            closeButton.onClick.AddListener(OnCLoseButton);
        }

        protected override void Cleanup()
        {
            usernameText.text = "";
            emailText.text = "";
            userIDText.text = "";
            newPassword.text = "";
            confirmPassword.text = "";
            updatePasswordButton.onClick.RemoveAllListeners();
            logoutButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();
        }
        
        private void OnClickLogout()
        {
            PlayfabManager.Instance.LogOut();
            GameManager.LoadScene(SceneName.Login);
        }

        private void OnCLoseButton()
        {
            UiManager.Instance.HidePanel(this);
        }
        private void OnClickUpdatePassword()
        {
            if (!ValidatePasswords(out string newPass))
                return;

            string jsonBody = BuildPasswordUpdatePayload(newPass);

            SendPasswordUpdateRequest(jsonBody);
        }

        #region Helpers

        private bool ValidatePasswords(out string newPass)
        {
            string confirmPass = confirmPassword.text.Trim();
            newPass = newPassword.text.Trim();

            if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                GameLogger.LogNetwork("Please fill in both password fields.");
                errorText.text = "Please fill in both password fields.";
                return false;
            }

            if (newPass != confirmPass)
            {
                GameLogger.LogNetwork("Passwords do not match.");
                errorText.text = "Passwords do not match.";
                return false;
            }

            if (newPass.Length < 6) // optional minimum length check
            {
                GameLogger.LogNetwork("Password must be at least 6 characters long.");
                errorText.text = "Password must be at least 6 characters long.";
                return false;
            }

            return true;
        }

        private string BuildPasswordUpdatePayload(string newPass)
        {
            var payload = new
            {
                user_id = _user.id,
                password = newPass
            };

            return JsonUtility.ToJson(payload);
        }

        private void SendPasswordUpdateRequest(string jsonBody)
        {
            
        }

        private void OnPasswordUpdateSuccess(string response)
        {
            GameLogger.LogNetwork("Password updated successfully.");
            newPassword.text = "";
            confirmPassword.text = "";
        }

        private void OnPasswordUpdateError(string error)
        {
            GameLogger.LogNetwork("Failed to update password: " + error);
        }

        #endregion

    }
}