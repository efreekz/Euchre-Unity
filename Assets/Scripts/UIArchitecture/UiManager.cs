using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ui;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = System.Object;

namespace UIArchitecture
{
    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;

        public List<UiSceneData> sceneData;

        private readonly List<Views> _activeScreens = new List<Views>();
        // private readonly Stack<PanelData> _panelStack = new Stack<PanelData>();

        public Canvas ActiveCanvas { get; private set; }
        public Views ActivePanel { get; private set; }

        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadSceneUi(SceneName sceneName)
        {
            UiSceneData uiSceneData = GetSceneData(sceneName);

            if (uiSceneData is not null)
            {
                _activeScreens.Clear();
                // _panelStack.Clear();

                ActiveCanvas = Instantiate(uiSceneData.canvasPrefab);
                foreach (var views in uiSceneData.viewsData)
                {
                    var screen = Instantiate(views, ActiveCanvas.transform);
                    _activeScreens.Add(screen);

                    screen.SetDefault();

                    if (views.screenName != uiSceneData.startingScreen)
                        continue;

                    ActivePanel = screen;
                    ActivePanel.Show(null);

                    // Push the starting screen with null data onto the stack
                    // _panelStack.Push(new PanelData(ActivePanel, null));
                }
            }
            else
            {
                Debug.LogWarning($"Unhandled scene: {sceneName}");
            }
        }

        private UiSceneData GetSceneData(SceneName sceneName)
        {
            return sceneData.FirstOrDefault(data => data.sceneName == sceneName);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public Views ShowPanel(UiScreenName uiScreenName, Object data)
        {
            Views view = GetUiView(uiScreenName);
            if (!view.showLastPanel)
            {
                ActivePanel.Hide();
            }
            ActivePanel = view;
            ActivePanel.Show(data);

            // _panelStack.Push(new PanelData(ActivePanel, data));
            return ActivePanel;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void HidePanel(Views viewToHide)
        {
            foreach (var screen in _activeScreens.Where(screen => screen == viewToHide))
            {
                screen.Hide();
                return;
            }
        }

        public Views GetUiView(UiScreenName uiScreenName)
        {
            foreach (var activeScreen in _activeScreens)
            {
                if (activeScreen.screenName == uiScreenName)
                {
                    return activeScreen;
                }
            }
            throw new AssertionException("No Ui Panel Added", "No Ui Panel Added");
        }

        [SerializeField] private Toast toastPrefab;
        public void ShowToast(string message)
        {
            var toast = Instantiate(toastPrefab, ActiveCanvas.transform);
            toast.Initialize(message);
        }

        // Helper class to store panel and associated data
        private class PanelData
        {
            public Views Panel { get; }
            public Object Data { get; }

            public PanelData(Views panel, Object data)
            {
                Panel = panel;
                Data = data;
            }
        }
    }

   
}

public enum UiScreenName
{
    // Login Screens
    LoginScreen,
    SignUpScreen,
    ForgotPasswordScreen,
    EmailVerificationScreen,
    LoginHomeScreen,
    LoginInfoScreen,
    
    // Main Menu Screens
    MainMenuHomeScreen,
    MatchMakingPanel,
    SettingsScreen,
    
    
    // Game Play Screens
    GamePlayScreens,
    ChooseTrumpSuitPopup,
    ChooseTrumpSuitSecondPopup,
    AskToGoAlonePopup,
    PausePopup,
    LoadingScreen,
    AccountDetails,
    
    WaitingPanel,
    ResultPanel,
    TranscetionsScreen
    
}

public enum SceneName
{
    Login,
    MainMenu,
    GamePlay,
}