using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class LoadingScreen : MonoBehaviour
{
    public static bool IsSceneReady { get; private set; }
    private static LoadingScreen _instance;
    private string _originalScene;
    private static bool _isNewScene;

    public string sceneAddress;
    public bool _isAnimationEnded = false;

    [HideInInspector]
    public float baseProgress; 
    [HideInInspector]
    public float additiveProgress; 

    public Slider slider;
    public TextMeshProUGUI textPercentage;

    [Header("Loading Settings")]
    [SerializeField] private float smoothingSpeed = 1f;


    void Awake()
    {
        sceneAddress = DoorManager.Instance.GetAdressableAdress();

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _originalScene = SceneManager.GetActiveScene().name;

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        StartCoroutine(LoadSceneAfterSubScenes());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isNewScene = scene.name != _originalScene;
    }

    public void ChangeAnimationEndedCheck()
    {
        _isAnimationEnded = true;
    }

    private IEnumerator LoadSceneAfterSubScenes()
    {
        IsSceneReady = false;
        Debug.Log("Cargando escena base...");

        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
        var clearCache = Addressables.ClearDependencyCacheAsync(sceneAddress, false);
        yield return clearCache;
        Addressables.Release(clearCache);

        var handle = Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Single);
        while (!handle.IsDone)
        {
            baseProgress = Mathf.Clamp01(handle.PercentComplete);  
            float target = baseProgress * 0.5f;                
            slider.value = Mathf.Lerp(slider.value, target, Time.deltaTime * smoothingSpeed);
            textPercentage.text = $"{Mathf.CeilToInt(slider.value * 100f)}%";
            yield return null;
        }
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Error al cargar escena base: {sceneAddress}");
            yield break;
        }

        Debug.Log("Escena base cargada. Comprobando sub‑escenas...");

        AdditiveSceneLoader additiveLoader = Object.FindFirstObjectByType<AdditiveSceneLoader>();

        if (additiveLoader == null || additiveLoader.subSceneKeys == null || additiveLoader.subSceneKeys.Count == 0)
        {
            Debug.Log("No hay sub‑escenas: rellenando barra al 100%...");
            while (slider.value < 0.99f)
            {
                slider.value = Mathf.Lerp(slider.value, 1f, Time.deltaTime * smoothingSpeed);
                textPercentage.text = $"{Mathf.CeilToInt(slider.value * 100f)}%";
                yield return null;
            }
            slider.value = 1f;
            textPercentage.text = "100%";

            yield return new WaitUntil(() => _isAnimationEnded);
            IsSceneReady = true;
            Debug.Log("Escena lista. Cerrando loading screen.");
            Destroy(gameObject);
            yield break;
        }
        else
        {
            Debug.Log("Sub‑escenas detectadas. Cargando de 50% a 100%...");
            while (!additiveLoader.IsDone)
            {
                additiveProgress = additiveLoader.Progress;     
                float target = 0.5f + (additiveProgress * 0.5f); 
                slider.value = Mathf.Lerp(slider.value, target, Time.deltaTime * smoothingSpeed);
                textPercentage.text = $"{Mathf.CeilToInt(slider.value * 100f)}%";
                yield return null;
            }

            while (slider.value < 0.99f)
            {
                slider.value = Mathf.Lerp(slider.value, 1f, Time.deltaTime * smoothingSpeed);
                textPercentage.text = $"{Mathf.CeilToInt(slider.value * 100f)}%";
                yield return null;
            }



            bool sceneIsReady = false;
            SceneLoadingTracker.OnSceneCompletelyReady = () => sceneIsReady = true;
            Debug.Log("Esperando a que todas las subescenas estén listas…");
            yield return new WaitUntil(() => sceneIsReady || textPercentage.text == "100%");

            IsSceneReady = true;
            Debug.Log("Sub‑escenas listas. Cerrando loading screen.");
            Destroy(gameObject);

        }
    }
}
