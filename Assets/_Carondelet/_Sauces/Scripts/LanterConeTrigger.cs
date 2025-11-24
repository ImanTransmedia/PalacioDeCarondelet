using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LanternConeTrigger : MonoBehaviour
{
    [Header("Visual opcional")]
    [SerializeField] private Light spotLight; // si tienes una luz Spot como linterna
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.black;

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        if (spotLight) spotLight.enabled = false;
    }

    private void Update()
    {
        bool usingLantern = GameManagerBDC.Instance != null && GameManagerBDC.Instance.isUsingLantern;

        // activa o desactiva el trigger y la luz
        _collider.enabled = usingLantern;
        if (spotLight)
        {
            spotLight.enabled = usingLantern;
            spotLight.color = usingLantern ? activeColor : inactiveColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!GameManagerBDC.Instance.isUsingLantern) return;

        // Busca objetos revelables
        if (other.TryGetComponent<IRevealable>(out var r))
        {
            r.Reveal(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Oculta al salir del cono
        if (other.TryGetComponent<IRevealable>(out var r))
        {
            r.Reveal(false);
        }
    }
}
    