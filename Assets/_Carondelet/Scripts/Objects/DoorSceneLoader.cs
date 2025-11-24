using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;

public class DoorSceneLoader : MonoBehaviour
{
    [SerializeField] public LocalizedString nombreEscenario;
    [SerializeField] public string doorID;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] public string sceneAddress;

    [SerializeField] public Vector3 doorIconOffset;

    private GameObject loadingScreenInstance;
    private AsyncOperationHandle<SceneInstance> _sceneLoadHandle;


    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Main Player" && gameObject.name == "ColliderDoorScene")
        {
            DoorManager.Instance.SaveAdressableString(sceneAddress);
            //UIIngameManager.Instance.ClearRenderTexture();
            DoorManager.Instance.LastDoorUsed = doorID;
            sceneAddress = DoorManager.Instance.GetAdressableAdress();
            //ShowLoadingScreen();
            //LoadSceneNoAdressable();
            //LoadNewScene();
            LoadLoadingScreen();
        }
    }

    public void LoadSceneAdressable()
    {
        //ShowLoadingScreen();
        //LoadNewScene();
        FindObjectOfType<DialogueController>().StopAllDialogueAndMedia();
        LoadLoadingScreen();
    }

    //private void ShowLoadingScreen()
    //{
    //    if (!loadingScreenPrefab) return;

    //    loadingScreenInstance = Instantiate(loadingScreenPrefab);
    //    DontDestroyOnLoad(loadingScreenInstance);
    //}

    //void LoadSceneNoAdressable()
    //{
    //    SceneManager.LoadScene(EscenaNoAdressable);
    //}

    public void LoadLoadingScreen()
    {
        DoorManager.Instance.SaveAdressableString(sceneAddress);
        SceneManager.LoadScene("LoadingScreen");
    }

    public void LoadNewScene()
    {
        DoorManager.Instance.LastDoorUsed = doorID;
        //ShowLoadingScreen();

        // Release previous scene if needed
        //if (_sceneLoadHandle.IsValid())
        //{
        //    Addressables.Release(_sceneLoadHandle);
        //}
        LoadLoadingScreen();
        //Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Single).Completed += HandleSceneLoadComplete;
    }

    private void HandleSceneLoadComplete(AsyncOperationHandle<SceneInstance> handle)
    {
        //if (handle.Status == AsyncOperationStatus.Succeeded)
        //{
        //    CleanupLoadingScreen();
        //}
        //else
        //{
        //    Debug.LogError($"Scene load failed: {handle.OperationException}");
        //    CleanupLoadingScreen();
        //}
        //    Debug.Log("Scene loaded successfully");
    }

    private void CleanupLoadingScreen()
    {
        if (loadingScreenInstance != null)
        {
            Destroy(loadingScreenInstance);
            loadingScreenInstance = null;
        }
    }

    //private void OnDestroy()
    //{
    //    // Clean up resources when this object is destroyed
    //    if (_sceneLoadHandle.IsValid())
    //    {
    //        _sceneLoadHandle.Completed -= HandleSceneLoadComplete;
    //        Addressables.Release(_sceneLoadHandle);
    //    }
    //    CleanupLoadingScreen();
    //}

    public Transform GetSpawnPoint() => spawnPoint;
}
