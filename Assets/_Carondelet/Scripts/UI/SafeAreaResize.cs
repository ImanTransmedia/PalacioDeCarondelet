using UnityEngine;

public class SafeAreaResize : MonoBehaviour
{
    //Este script redimensiona un panel para adaptarse a pantalla con notch o no rectangulares
    public RectTransform rectTransform; //Tamaño del panel que se redimenzionara
    private Rect lastSafeArea;
    private Vector2 lastResolution;
    private DeviceOrientation lastOrientation;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lastResolution = new Vector2(Screen.width, Screen.height);
        lastOrientation = Input.deviceOrientation;
        ApplySafeArea();
    }

    void Update()
    {
        if (Screen.width != lastResolution.x || Screen.height != lastResolution.y || Input.deviceOrientation != lastOrientation)
        {
            lastResolution = new Vector2(Screen.width, Screen.height);
            lastOrientation = Input.deviceOrientation;
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        if (safeArea != lastSafeArea) 
        {
            lastSafeArea = safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }
}
