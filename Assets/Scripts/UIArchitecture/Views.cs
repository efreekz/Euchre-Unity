using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = System.Object;

public abstract class Views : MonoBehaviour
{
    public UiScreenName screenName;        
    public bool showLastPanel;

    protected abstract void Initialize(Object obj);

    protected abstract void Cleanup();

    public abstract void SetDefault();

    public virtual void Show(Object obj)
    {
        Initialize(obj);
    }
    public virtual void Hide()
    {
        Cleanup();
    }
}