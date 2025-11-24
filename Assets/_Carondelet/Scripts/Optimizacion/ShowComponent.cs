using UnityEngine;

public class ShowComponent : MonoBehaviour
{
    public GameObject Componente;
    public GameObject Spinner;
    public AdditiveSceneLoader Loader;

    private void Start()
    {
        Componente.SetActive(false);
        Spinner.SetActive(true);
        Loader = Object.FindFirstObjectByType<AdditiveSceneLoader>();
    }
    private void Update()
    {
        if (Loader != null && Loader.IsDone)
        {
            Componente.SetActive(true);
            Spinner.SetActive(false);
            this.enabled = false;
        }

    }
}
