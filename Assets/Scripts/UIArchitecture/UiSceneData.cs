using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIArchitecture
{
    
    [System.Serializable]
    public class UiSceneData
    {
        public SceneName sceneName;
        public Canvas canvasPrefab;
        public UiScreenName startingScreen;
        public List<Views> viewsData;
    }
    
}