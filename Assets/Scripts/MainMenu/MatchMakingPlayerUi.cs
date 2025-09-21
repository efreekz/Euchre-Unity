using Helper;
using Managers;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
    public class MatchMakingPlayerUi : MonoBehaviour
    {
        [SerializeField] private LoadingAnimation loadingObject;
        [SerializeField] private GameObject dataContainer;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private Button addBotButton;
        private int _index;

        public void Initialize(bool showBotButton, int index)
        {
            addBotButton.onClick.AddListener(AddBotButtonClicked);
            addBotButton.gameObject.SetActive(showBotButton && loadingObject.isActiveAndEnabled);
            _index = index;
        }

        private void AddBotButtonClicked()
        {
            if (MultiplayerManager.Instance && MultiplayerManager.Instance.Runner && MultiplayerManager.Instance.Runner.IsServer)
                MultiplayerManager.Instance.RPCManager.SpawnBotAtSeat(_index);
        }

        public void SetData(string playerName)
        {
            playerNameText.text = playerName;
            dataContainer.SetActive(true);
            loadingObject.gameObject.SetActive(false);
            addBotButton.gameObject.SetActive(false);
        }

    }
}