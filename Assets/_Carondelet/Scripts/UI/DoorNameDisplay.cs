using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class DoorNameDisplay : MonoBehaviour
{
    public TextMeshProUGUI doorNameText;
    private LocalizedString currentLocalizedString;

    private void OnEnable()
    {
        if (currentLocalizedString != null)
        {
            currentLocalizedString.StringChanged += OnLocalizedStringChanged;
            currentLocalizedString.RefreshString(); 
        }
    }

    private void OnDisable()
    {
        if (currentLocalizedString != null)
        {
            currentLocalizedString.StringChanged -= OnLocalizedStringChanged;
        }
    }

   public void UpdateDoorName(LocalizedString newLocalizedString)
{
    if (currentLocalizedString != null)
    {
        currentLocalizedString.StringChanged -= OnLocalizedStringChanged;
    }

    currentLocalizedString = newLocalizedString;

    if (currentLocalizedString != null)
    {
        currentLocalizedString.StringChanged += OnLocalizedStringChanged;
        currentLocalizedString.GetLocalizedStringAsync().Completed += handle =>
        {
            if (doorNameText != null && handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                doorNameText.text = handle.Result;
            }
        };
    }
}

    private void OnLocalizedStringChanged(string localizedText)
    {
        if (doorNameText != null)
        {
            doorNameText.text = localizedText;
        }
    }
}
