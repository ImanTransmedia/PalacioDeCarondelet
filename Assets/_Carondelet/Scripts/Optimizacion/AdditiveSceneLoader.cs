using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;

public class AdditiveSceneLoader : MonoBehaviour
{
    public static bool AllScenesLoaded = false;
    public bool IsDone = false;
    public event Action OnDone;

    public List<string> subSceneKeys = new List<string>();

    public float Progress { get; private set; }

    public List<AsyncOperationHandle<SceneInstance>> loadedSceneHandles = new();
    private int totalSubScenes = 0;

    void Start()
    {
        totalSubScenes = subSceneKeys != null ? subSceneKeys.Count : 0;
        StartCoroutine(LoadSubScenesSequentially());
    }

    IEnumerator LoadSubScenesSequentially()
    {
        yield return new WaitForSeconds(1f);

        if (subSceneKeys == null || subSceneKeys.Count == 0)
        {
            Progress = 1f;
            IsDone = true;
            AllScenesLoaded = true;
            SceneLoadingTracker.NotifyLoadingComplete();
            OnDone?.Invoke();
            yield break;
        }

        for (int i = 0; i < subSceneKeys.Count; i++)
        {
            string key = subSceneKeys[i];

            yield return Resources.UnloadUnusedAssets();
            System.GC.Collect();

            var clearCacheHandle = Addressables.ClearDependencyCacheAsync(key, false);
            yield return clearCacheHandle;
            Addressables.Release(clearCacheHandle);

            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
                key, LoadSceneMode.Additive, true);

            while (!handle.IsDone)
            {
                float partial = handle.PercentComplete;
                Progress = ((float)i + partial) / totalSubScenes;
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedSceneHandles.Add(handle);
                Progress = (float)(i + 1) / totalSubScenes;
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                yield break;
            }
        }

        IsDone = true;
        AllScenesLoaded = true;
        SceneLoadingTracker.NotifyLoadingComplete();
        OnDone?.Invoke();
    }

    public void UnloadAllSubScenes()
    {
        StartCoroutine(UnloadAllScenesRoutine());
    }

    IEnumerator UnloadAllScenesRoutine()
    {
        foreach (var handle in loadedSceneHandles)
        {
            yield return Addressables.UnloadSceneAsync(handle, UnloadSceneOptions.None);
        }

        loadedSceneHandles.Clear();
        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
