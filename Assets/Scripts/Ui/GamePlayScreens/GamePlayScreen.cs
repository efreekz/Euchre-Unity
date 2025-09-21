using Controllers;
using DG.Tweening;
using GamePlay.Cards;
using GamePlay.Ui;
using TMPro;
using UIArchitecture;
using UnityEngine;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.GamePlayScreens
{
    public class GamePlayScreen : FullScreenView
    {
        [SerializeField] private Transform deckParent;
        [SerializeField] private Image activeTrumpSuit;
        [SerializeField] private PlayerElementUi[] playerElements;
        [SerializeField] private ScoreBoard scoreBoard;
        
        [SerializeField] private Button pauseButton;
       
        private Transform originalTrumpParent;

        protected override void Initialize(Object obj)
        {
            originalTrumpParent = activeTrumpSuit.transform.parent;
            activeTrumpSuit.gameObject.SetActive(false);
            foreach (var playerElementUi in playerElements)
            {
                playerElementUi.Init(deckParent);
            }
            
            pauseButton.onClick.AddListener(OnClickPause);
        }

        protected override void Cleanup()
        {
            pauseButton.onClick.RemoveAllListeners();
        }

        private void OnClickPause()
        {
            UiManager.Instance.ShowPanel(UiScreenName.PausePopup, null);
        }

        public PlayerElementUi GetPlayerElement(int index)
        {
            return playerElements[index];
        }

        public Transform GetDeck()
        {
            return deckParent;
        }

        public void DisableDeck()
        {
            deckParent.gameObject.SetActive(false);
        }

        public void ActiveTrumpSuit(Suit suit, PlayerElementUi playerElementUi)
        {
            if (suit == Suit.None)
            {
                Debug.LogError($"Suit is None");
                return;
            }
            
            activeTrumpSuit.sprite = GamePlayControllerNetworked.Instance.cardsController.GetSuitData(suit).sprite;
            activeTrumpSuit.gameObject.SetActive(true);
            
            RectTransform suitRect = activeTrumpSuit.rectTransform;
            RectTransform holderRect = playerElementUi.trumpHolder;
            holderRect.gameObject.SetActive(true);

            // Reparent but keep world position (so animation looks natural)
            suitRect.SetParent(holderRect, worldPositionStays: true);

            // Reset anchors before tween
            suitRect.anchorMin = Vector2.zero;
            suitRect.anchorMax = Vector2.one;

            // Animate to target position/size/rotation/scale
            suitRect.DOAnchorPos(Vector2.zero, 0.5f).SetDelay(1f);         // move to center
            suitRect.DOSizeDelta(Vector2.zero, 0.5f).SetDelay(1f);         // stretch to fit
            suitRect.DOLocalRotate(Vector3.zero, 0.5f).SetDelay(1f);       // reset rotation
            suitRect.DOScale(Vector3.one, 0.5f).SetDelay(1f);              // normalize scale
        }

        public void UpdateScore(TeamData teamA, TeamData teamB)
        {
            scoreBoard.SetScore(teamA, teamB);
        }

        public void UpdateScore(NetworkTeamData teamA, NetworkTeamData teamB)
        {
            scoreBoard.SetScore(teamA, teamB);
        }

        public void UpdateCurrentTricksCountForTeamA(int trickCount)
        {
            scoreBoard.SetCurrentTricksForTeamA(trickCount);
        }
        public void UpdateCurrentTricksCountForTeamB(int trickCount)
        {
            scoreBoard.SetCurrentTricksForTeamB(trickCount);
        }
        public void Reset()
        {
            deckParent.gameObject.SetActive(true);

            RectTransform suitRect = activeTrumpSuit.rectTransform;
            
            suitRect.SetParent(originalTrumpParent, worldPositionStays: true);

            suitRect.anchorMin = Vector2.zero;
            suitRect.anchorMax = Vector2.one;
            suitRect.anchoredPosition = Vector2.zero;
            suitRect.sizeDelta = Vector2.zero;
            suitRect.localRotation = Quaternion.identity;
            suitRect.localScale = Vector3.one;

            activeTrumpSuit.gameObject.SetActive(false);
        }

    }
}
