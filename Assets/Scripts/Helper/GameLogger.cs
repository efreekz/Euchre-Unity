using System;
using UnityEngine;

public static class GameLogger
{
    private static readonly bool IsTesting; // Change this to false in production builds
    private static readonly bool IsTestingNetwork; // Change this to false in production builds
    static GameLogger()
    {
        IsTesting = true;
        IsTestingNetwork = false;
    }

    public enum LogType
    {
        Log,
        Warning,
        Error
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public static void ShowLog(string message, LogType logType = LogType.Log)
    {
        if (logType != LogType.Error)
            if (!IsTesting) 
                return;

        switch (logType)
        {
            case LogType.Log:
                Debug.Log(message);
                break;
            case LogType.Warning:
                Debug.LogWarning(message);
                break;
            case LogType.Error:
                Debug.LogError(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogNetwork(string message, LogType logType = LogType.Log)
    {
        if (logType != LogType.Error)
            if (!IsTestingNetwork) 
                return;

        switch (logType)
        {
            case LogType.Log:
                Debug.Log(message);
                break;
            case LogType.Warning:
                Debug.LogWarning(message);
                break;
            case LogType.Error:
                Debug.LogError(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
        }
    }
}