using System;
using UnityEngine;

public static class SceneLoadingTracker
{
    public static Action OnSceneCompletelyReady;
    public static bool IsSceneReady { get; private set; }

    public static void NotifyLoadingComplete()
    {
        Debug.Log("Propagando la carga de escenas.");
        OnSceneCompletelyReady?.Invoke();
        IsSceneReady = true;
        OnSceneCompletelyReady = null;
    }
}
