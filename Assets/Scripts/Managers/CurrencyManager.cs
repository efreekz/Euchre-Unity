using System;
using Cysharp.Threading.Tasks;
using Data;
using Network;
using UIArchitecture;

namespace Managers
{
    public static class CurrencyManager
    {
        public static Action<float> UpdateFreekz;

        private static float _freekz;

        public static float Freekz 
        {
            get => _freekz;
            set
            {
                _freekz = value;
                UpdateFreekz?.Invoke(_freekz);
            }
        }

        public static async UniTask<bool> AddFreekz(int value, string reason, string description)
        {
            if (value <= 0) return false;

            var waitPanel = UiManager.Instance.ShowPanel(UiScreenName.WaitingPanel, null);
            var result = await PlayfabManager.Instance.UpdateBalance(value, reason, description, TransectionType.Credit);
            UiManager.Instance.HidePanel(waitPanel);
            
            return result;
        }
        
        public static async UniTask<bool> SubtractFreekz(int value, string reason, string description)
        {
            if (Freekz - value < 0) return false;
            
            var waitPanel = UiManager.Instance.ShowPanel(UiScreenName.WaitingPanel, null);
            var result = await PlayfabManager.Instance.UpdateBalance(-value, reason, description, TransectionType.Debit);
            UiManager.Instance.HidePanel(waitPanel);
                
            return result;

        }
    }
}