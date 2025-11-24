using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class SceneLoader : MonoBehaviour
{
    private string nameSceneLoad;

    private AsyncOperationHandle<SceneInstance> _sceneLoadHandle;
    [SerializeField] private AssetReference targetScene;

    [SerializeField]public string EscenaNoAdressable;

    private void Start()
    {
        nameSceneLoad = gameObject.name;
    }

    public void StartAsyncLoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void LoadNewScene()
    {

        _sceneLoadHandle = Addressables.LoadSceneAsync(targetScene,
            LoadSceneMode.Single,
            activateOnLoad: true);
    }

    public void LoadSceneNoAdresable()
    {
        SceneManager.LoadScene(EscenaNoAdressable);
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            yield return null;
        }
    }

    public IEnumerator SceneLoad()
    {
      

        string doorName = nameSceneLoad;
        AsyncOperation operation = SceneManager.LoadSceneAsync(doorName);
        while (!operation.isDone)
        {
            yield return null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Main Player")
        {
            Debug.Log("Colision!");
            LoadNewScene();
        }
    }


}
