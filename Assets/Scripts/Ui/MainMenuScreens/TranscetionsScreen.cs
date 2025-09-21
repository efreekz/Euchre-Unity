using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Managers;
using Network;
using UIArchitecture;
using UnityEngine;
using UnityEngine.UI;

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
            // closeButton.onClick.AddListener(() => UiManager.Instance.HidePanel(this));
            // scrollView.gameObject.SetActive(false);
            // loader.SetActive(true);
            //
            // // 🔄 Refresh latest transactions from Firestore
            // var userId = PlayfabManager.Instance.User.UserId;
            // PlayfabManager.Instance.CurrentUserData = await Network.FirebaseManager.Instance.GetUserData(userId);
            //
            // loader.SetActive(false);
            //
            // PopulateTransactions();
            // scrollView.gameObject.SetActive(true);
            //
            // await UniTask.DelayFrame(1);
            // LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
            
        }

        protected override void Cleanup()
        {
            closeButton.onClick.RemoveAllListeners();

            foreach (var card in _spawnedCards)
                Destroy(card);
            _spawnedCards.Clear();
        }

        // private void PopulateTransactions()
        // {
        //     // Clear old cards
        //     foreach (var card in _spawnedCards)
        //         Destroy(card);
        //     _spawnedCards.Clear();
        //
        //     var transactions = Network.PlayfabManager.Instance.CurrentUserData.transections;
        //
        //     // Sort by CreatedAt (newest first)
        //     var sorted = transactions.OrderByDescending(t => t.CreatedAt).ToList();
        //
        //     foreach (var txn in sorted)
        //     {
        //         var cardObj = Object.Instantiate(transectionCardPrefab, contentParent);
        //         var card = cardObj.GetComponent<MainMenu.TransectionCard>();
        //         card.Setup(txn);
        //
        //         _spawnedCards.Add(cardObj);
        //     }
        // }
    }
}
