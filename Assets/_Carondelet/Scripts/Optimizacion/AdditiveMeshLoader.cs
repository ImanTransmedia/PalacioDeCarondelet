using UnityEngine;
using UnityEngine.SceneManagement;

public class AdditiveMeshLoader : MonoBehaviour
{
    [SerializeField] string meshSceneName = "MaquetaScene";
    [SerializeField] Transform pivotRoot;
    [SerializeField] Transform pivotMaqueta;
    [SerializeField] string meshRootTag = "MeshRoot";
    [SerializeField] AdditiveSceneLoader sceneLoader;

    void Start()
    {
        if (sceneLoader == null)
            sceneLoader = Object.FindFirstObjectByType<AdditiveSceneLoader>();
        sceneLoader.OnDone += OnSceneLoaded;
    }

    private void OnSceneLoaded()
    {
        var scene = SceneManager.GetSceneByName(meshSceneName);
        if (!scene.IsValid() || !scene.isLoaded) return;

        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            if (go.CompareTag(meshRootTag))
            {
                go.transform.SetParent(pivotRoot, true);
                go.gameObject.transform.localPosition = pivotMaqueta.localPosition;
                go.gameObject.transform.localRotation = pivotMaqueta.localRotation;
                go.gameObject.transform.localScale = pivotMaqueta.localScale;
            }
        }
    }
}
