using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;

public class LanguageManager : MonoBehaviour
{
    private const string SelectedLocaleKey = "SelectedLocaleCode";
    private bool isSwitching = false; 

    private void Awake()
    {
        StartCoroutine(RestoreSavedLanguage());
    }
     void Start()
    {
       
    }
    public void SwitchLanguage()
    {
        if (isSwitching) return;
        StartCoroutine(ChangeLanguage());
    }

    private IEnumerator ChangeLanguage()
    {
        isSwitching = true;
        yield return LocalizationSettings.InitializationOperation;
        int currentLocaleIndex = LocalizationSettings.AvailableLocales.Locales.IndexOf(LocalizationSettings.SelectedLocale);
        int nextLocaleIndex = (currentLocaleIndex + 1) % LocalizationSettings.AvailableLocales.Locales.Count;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[nextLocaleIndex];
        SaveSelectedLanguage();
        isSwitching = false;
    }

     public void SetLanguageByIndex(int index)
    {
        if (isSwitching) return;
        StartCoroutine(ChangeLanguageByIndex(index));
    }

    private IEnumerator ChangeLanguageByIndex(int index)
    {
        isSwitching = true;
        yield return LocalizationSettings.InitializationOperation;

        if (index >= 0 && index < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
            SaveSelectedLanguage();
        }

        isSwitching = false;
    }

    private IEnumerator RestoreSavedLanguage()
    {
        isSwitching = true;
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales.Count == 0)
        {
            isSwitching = false;
            yield break;
        }

        string savedCode = PlayerPrefs.GetString(SelectedLocaleKey, locales[0].Identifier.Code);
        var savedLocale = locales.Find(locale => locale.Identifier.Code == savedCode);
        LocalizationSettings.SelectedLocale = savedLocale ?? locales[0];

        isSwitching = false;
    }

    private void SaveSelectedLanguage()
    {
        var selectedLocale = LocalizationSettings.SelectedLocale;
        if (selectedLocale == null)
            return;

        PlayerPrefs.SetString(SelectedLocaleKey, selectedLocale.Identifier.Code);
        PlayerPrefs.Save();
    }
}
