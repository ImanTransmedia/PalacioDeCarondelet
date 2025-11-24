using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedDialogue : MonoBehaviour
{
    public LocalizedString localizedString;
    public LocalizedString localizedVideo;
    public LocalizedString localizedAudio;

    [Header("Speed Settings")]
    public float speedSpanish;
    public float speedFrench;
    public float speedKichwa;
    public float speedEnglish;
    public float finalSpeed;

    void OnEnable()
    {
       
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void OnDisable()
    {
       
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void Start()
    {
        SetFinalSpeedByLocale();
    }

    void OnLocaleChanged(Locale newLocale)
    {
        SetFinalSpeedByLocale(); 
    }

    void SetFinalSpeedByLocale()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale != null)
        {
            string code = locale.Identifier.Code;

            switch (code)
            {
                case "es":  // Spanish
                    finalSpeed = speedSpanish;
                    break;
                case "fr":  // French
                    finalSpeed = speedFrench;
                    break;
                case "es-EC":  // Kichwa / Quechua
                    finalSpeed = speedKichwa;
                    break;
                case "en":  // English
                    finalSpeed = speedEnglish;
                    break;
                default:
                    finalSpeed = speedEnglish; // fallback
                    break;
            }
        }
        else
        {
            finalSpeed = speedEnglish;
        }
    }
}
