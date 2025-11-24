using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class LoadSceneAddressable : MonoBehaviour
{
    public string sceneAddress;

    public void LoadScene()
    {
        Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Single).Completed += OnSceneLoaded;
    }

    public void LoadDefaultScene()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("Escena cargada correctamente.");
        }
        else
        {
            Debug.LogError("Error al cargar la escena.");
        }
    }
}
