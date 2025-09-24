using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Data;
using Managers;
using Network;
using UIArchitecture;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ui.MainMenuScreens
{
    public class TranscetionsScreen : PopUpView
    {
        [Header("UI References")]
        public Button closeButton;
        public GameObject loader;
        public RectTransform scrollView;

        public Transform contentParent; // ScrollView Content
        public GameObject transectionCardPrefab;

        private readonly List<GameObject> _spawnedCards = new();

        protected override async void Initialize(object obj)
        {
            closeButton.onClick.AddListener(() => UiManager.Instance.HidePanel(this));
            scrollView.gameObject.SetActive(false);
            loader.SetActive(true);
            
            // 🔄 Get Latest Transections
            var transactionResponse = await AuthManager.Instance.GetAllTransactions();
            
            loader.SetActive(false);
            
            if (transactionResponse != null)
                PopulateTransactions(transactionResponse);
            scrollView.gameObject.SetActive(true);
            
            await UniTask.DelayFrame(1);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
            
        }

        protected override void Cleanup()
        {
            closeButton.onClick.RemoveAllListeners();

            foreach (var card in _spawnedCards)
                Destroy(card);
            _spawnedCards.Clear();
        }

        private void PopulateTransactions(TransactionResponse transactionResponse)
        {
            // Clear old cards
            foreach (var card in _spawnedCards)
                Destroy(card);
            _spawnedCards.Clear();
        
            var sorted = transactionResponse.transactions
            .OrderByDescending(t =>
                {
                    DateTime.TryParseExact(
                        t.created_at,
                        "yyyy-MM-dd HH:mm:ss",    // format from your JSON
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var parsedDate);

                    return parsedDate;
                })
                .ToList();

        
            foreach (var txn in sorted)
            {
                var cardObj = Object.Instantiate(transectionCardPrefab, contentParent);
                var card = cardObj.GetComponent<MainMenu.TransectionCard>();
                card.Setup(txn);
        
                _spawnedCards.Add(cardObj);
            }
        }
    }
}
