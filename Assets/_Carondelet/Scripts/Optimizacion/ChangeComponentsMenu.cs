using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ChangeComponentsMenu : MonoBehaviour
{
    [Header("Estado")]
    public static bool isInInicio = true;

    [SerializeField] private Transform pivoteInicio;
    [SerializeField] private Transform pivoteMaqueta;
    [SerializeField] private Transform pivotePalacioInicio;
    [SerializeField] private Transform pivotePalacioMaqueta;
    [SerializeField] private GameObject palacioObject;

    [Header("Components")]
    [SerializeField] private GameObject[] inicioComponents;
    [SerializeField] private GameObject[] maquetaComponents;

    [Header("Eventos")]
    public UnityEvent onInicio;
    public UnityEvent onMaqueta;

    [Header("AutoDeteccion de Escenas")]
    [SerializeField] private bool autoDetectByScene = true;
    [SerializeField] private string[] inicioScenes;
    [SerializeField] private string[] maquetaScenes;
    [SerializeField] private bool matchByContains = false;

    private void Start()
    {
        if (autoDetectByScene)
        {
            isInInicio = EvaluateIsInicioFromLoadedScenes();
        }

        ApplyState();
    }

    private void SetComponentsActive(GameObject[] objs, bool state)
    {
        if (objs == null) return;
        foreach (var go in objs)
        {
            if (go != null) go.SetActive(state);
        }
    }

    private void ApplyState()
    {
        if (isInInicio)
        {
            SetComponentsActive(inicioComponents, true);
            SetComponentsActive(maquetaComponents, false);

            if (pivoteInicio != null) applyTransform(pivoteInicio, pivotePalacioInicio);

            onInicio?.Invoke();
        }
        else
        {
            SetComponentsActive(inicioComponents, false);
            SetComponentsActive(maquetaComponents, true);

            if (pivoteMaqueta != null) applyTransform(pivoteMaqueta, pivotePalacioMaqueta);

            onMaqueta?.Invoke();
        }
    }

    public void applyTransform(Transform pivotG, Transform pivotM)
    {
        if (pivotG == null) return;
        var t = transform;
        t.position = pivotG.position;
        t.rotation = pivotG.rotation;
        t.localScale = pivotG.localScale;

        this.palacioObject.transform.position = pivotM.position;
        this.palacioObject.transform.rotation = pivotM.rotation;
        this.palacioObject.transform.localScale = pivotM.localScale;
    }


    public void DeactivateInicioComponents()
    {
        SetComponentsActive(inicioComponents, false);
    }

    public void DeactivateMaquetaComponents()
    {
        SetComponentsActive(maquetaComponents, false);
    }


    private bool EvaluateIsInicioFromLoadedScenes()
    {
        int loaded = SceneManager.sceneCount;

        bool anyInicio = false;
        bool anyMaqueta = false;

        for (int i = 0; i < loaded; i++)
        {
            Scene scn = SceneManager.GetSceneAt(i);
            if (!scn.isLoaded) continue;

            string name = scn.name;

            if (MatchesAny(name, inicioScenes))
                anyInicio = true;

            if (MatchesAny(name, maquetaScenes))
                anyMaqueta = true;
        }

        if (anyInicio && anyMaqueta)
        {
            var active = SceneManager.GetActiveScene().name;
            if (MatchesAny(active, inicioScenes)) return true;
            if (MatchesAny(active, maquetaScenes)) return false;
            return isInInicio;
        }

        if (anyInicio) return true;
        if (anyMaqueta) return false;

        return isInInicio;
    }


    private bool MatchesAny(string sceneName, string[] list)
    {
        if (list == null || list.Length == 0) return false;

        if (matchByContains)
        {
            foreach (var s in list)
            {
                if (!string.IsNullOrEmpty(s) && sceneName.Contains(s))
                    return true;
            }
            return false;
        }
        else
        {
            foreach (var s in list)
            {
                if (!string.IsNullOrEmpty(s) && sceneName == s)
                    return true;
            }
            return false;
        }
    }


    public void RefreshByScene()
    {
        isInInicio = EvaluateIsInicioFromLoadedScenes();
        ApplyState();
    }

 
    public void ForceState(bool setInicio)
    {
        isInInicio = setInicio;
        ApplyState();
    }
}
