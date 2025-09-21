using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Data;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Network
{
    public class PlayfabManager : MonoBehaviour
    {
        [Header("PlayFab Settings")]
        [SerializeField] private string titleId = "YOUR_TITLE_ID";

        public static PlayfabManager Instance;
        public UserData CurrentUserData { get; set; }

        private void Awake()
        {
            // Singleton Pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            
            if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
            {
                PlayFabSettings.TitleId = titleId;
            }
        }
        

        public async UniTask Login(string emailInputText, string passwordInputText, Action onSucessfullLogin, Action<string> onLoginFailed)
        {
            
        }

        public async UniTask SignUpWithInvitation(string emailInputText, string passwordInputText, string usernameInputText, string referralInputText, Action onSucessfullLogin, Action<string> onLoginFailed)
        {
            
        }

        public async UniTask<bool> UpdateBalance(int value, string reason, string description, TransectionType credit)
        {
            
            return false;
        }

        public async UniTask LogOut()
        {
            
        }
    }
}