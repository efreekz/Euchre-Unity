using Cysharp.Threading.Tasks;
using Data;
using Managers;
using TMPro;
using UIArchitecture;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = System.Object;

namespace Ui.MainMenuScreens
{
    public class ResultScreen : FullScreenView
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text winLoseText;
        [SerializeField] private TMP_Text rewardText;

        private GameResult _resultData; 
        
        // Some nice color palettes
        private readonly Color[] _winColors = new Color[]
        {
            new Color(0.2f, 0.8f, 0.3f), // Green
            new Color(1f, 0.84f, 0f),   // Gold
            new Color(0.3f, 0.7f, 1f),  // Blue
            new Color(0.9f, 0.3f, 0.9f) // Purple
        };

        private readonly Color[] _loseColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),  // Red
            new Color(0.9f, 0.4f, 0.1f),// Orange
            new Color(0.7f, 0.7f, 0.7f),// Gray
            new Color(0.5f, 0f, 0f)     // Dark red
        };
        private readonly string[] _loseMessages =
        {
            "Better luck next time!",
            "Don’t give up, champion!",
            "Every defeat is a step to victory!",
            "You fought well, but lost the battle.",
            "Keep trying, your win is coming!"
        };

        
        protected override void Initialize(Object obj)
        {
            if (obj is GameResult gameResult)
            {
                _resultData = gameResult;
                if (_resultData.IsLocalPlayerWinner)
                {
                    winLoseText.text = "You Won!";
                    if (_resultData.Reward > 0)
                    {
                        winLoseText.color = _winColors[Random.Range(0, _winColors.Length)];

                        rewardText.richText = true;
                        rewardText.color = Color.white;

                        var hex = ColorUtility.ToHtmlStringRGB(winLoseText.color);
                        rewardText.text = $"You have got <color=#{hex}>{_resultData.Reward} Freekz</color>";
                        CurrencyManager.AddFreekz(_resultData.Reward, "Game Won", "You won the game").Forget();
                    }
                }
                else
                {
                    winLoseText.text = $"You Lost";
                    rewardText.text = _loseMessages[Random.Range(0, _loseMessages.Length)];

                    // Pick a random "lose" color
                    winLoseText.color = _loseColors[Random.Range(0, _loseColors.Length)];
                }
            }
            else
            {
                GameLogger.ShowLog($"Game Result not found", GameLogger.LogType.Error);
            }
            closeButton.onClick.AddListener(OnClickClose);
        }

        protected override void Cleanup()
        {
            closeButton.onClick.RemoveListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UiManager.Instance.HidePanel(this);
        }
        
        
    }
}
