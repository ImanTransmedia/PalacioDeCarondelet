using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class BDC_Loader : MonoBehaviour
{
    public string bdcSceneKey;

    public float Progress { get; private set; }

    AsyncOperationHandle<SceneInstance> _handle;
    bool _isLoading = false;
    bool _isLoaded = false;

    void Update()
    {
        if (_isLoading && !_handle.Equals(default(AsyncOperationHandle<SceneInstance>)))
        {
            Progress = _handle.PercentComplete;
        }
    }

    public void CargarBDCScene()
    {
        if (string.IsNullOrEmpty(bdcSceneKey)) return;
        if (_isLoaded || _isLoading) return;

        _isLoading = true;

        _handle = Addressables.LoadSceneAsync(bdcSceneKey, LoadSceneMode.Additive, true);
        _handle.Completed += OnSceneLoaded;
    }

    void OnSceneLoaded(AsyncOperationHandle<SceneInstance> handle)
    {
        _isLoading = false;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _isLoaded = true;
            Progress = 1f;
        }
        else
        {
            _isLoaded = false;
        }
    }

    public void UnloadBDCScene()
    {
        if (!_isLoaded) return;

        Addressables.UnloadSceneAsync(_handle, UnloadSceneOptions.None);
        _isLoaded = false;
        _isLoading = false;
        Progress = 0f;
    }
}
