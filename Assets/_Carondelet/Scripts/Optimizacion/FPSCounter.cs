using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    public GameObject panel;
    float deltaTime;

    // Contador para el triple tap
    private float lastTapTime = 0f;
    private const float tripleTapDelay = 0.5f; // Tiempo máximo entre taps para contarlos
    private int tapCount = 0;

    void Update()
    {
        // Calcula el tiempo entre fotogramas (FPS smoothing)
        deltaTime += (Time.deltaTime - deltaTime) / 10;

        // Calcula los FPS y formatealos como un texto
        float fps = 1.0f / deltaTime;
        // Se usa Mathf.RoundToInt en lugar de Mathf.Ceil para un valor más común en contadores de FPS
        fpsText.text = string.Format("FPS: {0}", Mathf.RoundToInt(fps));

        // Llama al método para detectar el toggle
        CheckForToggleInput();
    }

    // Nuevo método para verificar la entrada de teclado o móvil
    void CheckForToggleInput()
    {
        // 1. Detección de Teclado (Ctrl + H)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
        {
            TogglePanelVisibility();
        }
        else if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.H))
        {
            TogglePanelVisibility();
        }

        // 2. Detección de Triple Tap en Móvil
        // Solo verifica si hay toques y la plataforma es móvil
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Ended)
            {
                // Verifica el tiempo transcurrido desde el último tap
                if (Time.time - lastTapTime < tripleTapDelay)
                {
                    tapCount++;
                }
                else
                {
                    // Si pasa mucho tiempo, reinicia el contador
                    tapCount = 1;
                }

                lastTapTime = Time.time;

                // Si se detectan 3 taps seguidos
                if (tapCount == 3)
                {
                    TogglePanelVisibility();
                    tapCount = 0; // Reinicia el contador
                }
            }
        }
    }

    // Nuevo método para cambiar la visibilidad del panel
    void TogglePanelVisibility()
    {
        // La negación (!) de activeSelf invierte el estado actual del panel
        panel.SetActive(!panel.activeSelf);
    }
}