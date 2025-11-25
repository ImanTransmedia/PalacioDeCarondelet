using UnityEngine;
using TMPro;
using UnityEngine.Localization;

[RequireComponent(typeof(Collider))]
public class NameRegionHandler : MonoBehaviour
{
    [Header("Texto a modificar")]
    [SerializeField] private TMP_Text targetText;

    [Header("Strings localizados")]
    [SerializeField] private LocalizedString onEnterText; 
    [SerializeField] private LocalizedString onExitText; 

    [Header("Detección de jugador")]
    [SerializeField] private string playerTag = "Player";

    private Collider triggerCollider;

    private void Reset()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player != null && triggerCollider != null)
        {
            Collider playerCollider = player.GetComponent<Collider>();

            if (playerCollider != null)
            {
                bool estaDentro = triggerCollider.bounds.Intersects(playerCollider.bounds);

                if (estaDentro)
                    SetLocalizedText(onEnterText);
                else
                    SetLocalizedText(onExitText);
            }
            else
            {
                SetLocalizedText(onExitText);
            }
        }
        else
        {
            SetLocalizedText(onExitText);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        SetLocalizedText(onEnterText);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        SetLocalizedText(onExitText);
    }

    private async void SetLocalizedText(LocalizedString locString)
    {
        if (targetText == null || locString == null) return;

        var handle = locString.GetLocalizedStringAsync();
        string result = await handle.Task;

        targetText.text = result;
    }
}
