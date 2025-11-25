using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class NameRegionHandler : MonoBehaviour
{
    [Header("Texto a modificar")]
    [SerializeField] private TMP_Text targetText;

    [Header("Strings localizados")]
    [SerializeField] private LocalizedString onEnterText; 
    [SerializeField] private LocalizedString onExitText;  

    private void Reset()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void OnTriggerEnter(Collider other)
    {

        SetLocalizedText(onEnterText);
    }

    private void OnTriggerExit(Collider other)
    {
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
