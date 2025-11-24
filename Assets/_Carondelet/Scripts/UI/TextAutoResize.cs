using TMPro;
using UnityEngine;

public class textAutoResize : MonoBehaviour
{
    public float mobileFontSizeBonus = 4f; 

    private TMP_Text tmpText;

    void Start()
    {
        tmpText = GetComponent<TMP_Text>();

        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && uiManager.isMobile)
        {
            tmpText.fontSize += mobileFontSizeBonus;
        }
    }
}