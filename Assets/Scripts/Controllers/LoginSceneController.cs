using System;
using Cysharp.Threading.Tasks;
using Data;
using Fusion;
using Managers;
using Network;
using UIArchitecture;
using UnityEngine;

namespace Controllers
{
    public class LoginSceneController : MonoBehaviour
    {
        public static LoginSceneController Instance;
        
        private Views _waitingPanel;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            UiManager.Instance.LoadSceneUi(SceneName.Login);
            
        }

    }
}
