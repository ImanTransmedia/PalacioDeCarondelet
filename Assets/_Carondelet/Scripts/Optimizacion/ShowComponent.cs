using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class ShowComponent : MonoBehaviour
{
    public GameObject Componente;
    public GameObject Spinner;
    public AdditiveSceneLoader Loader;
    public string SFXName = "HardTap";
    [Range(0, 1.5f)]
    public float volume = 1.5f;

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
            Debug.Log("Playing SFX: " + SFXName + " at volume: " + volume);
            AudioManagerBDC.I.PlaySFX(SFXName, volume: volume);
            Componente.SetActive(true);
            Spinner.SetActive(false);

            this.enabled = false;

        }

    }
}
