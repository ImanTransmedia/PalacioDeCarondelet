using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollRectInitializer : MonoBehaviour
{
    [Header("Refs")]
    public ScrollRect scrollRect;                 // Asigna por Inspector
    [Tooltip("True = arriba (vertical), False = abajo")]
    public bool startAtTop = true;                // Para vertical
    [Tooltip("True = izquierda (horizontal), False = derecha")]
    public bool startAtLeft = true;               // Para horizontal
    [Tooltip("Ejecutar automáticamente al habilitar el panel")]
    public bool runOnEnable = true;

    [Header("Timing")]
    [Tooltip("Cuántos frames esperar antes de fijar la posición")]
    public int waitFrames = 1;                    // súbelo si el contenido se crea tarde

    void Reset()
    {
        // Auto-asignación al agregar el script
        scrollRect = GetComponentInChildren<ScrollRect>(true);
    }

    void OnEnable()
    {
        if (runOnEnable) InitializeNow();
    }

    /// Llamable desde otros scripts tras activar/mostrar el panel
    public void InitializeNow()
    {
        if (gameObject.activeInHierarchy && scrollRect != null)
            StartCoroutine(InitCoroutine());
    }

    IEnumerator InitCoroutine()
    {
        // Espera algunos frames para que se instancien/posicionen los ítems
        for (int i = 0; i < waitFrames; i++) yield return null;

        // Fuerza cálculo de layout antes de tocar la posición
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        Canvas.ForceUpdateCanvases();

        // IMPORTANTE: verticalNormalizedPosition -> 1 = arriba, 0 = abajo (con Direction BottomToTop)
        if (scrollRect.vertical)
            scrollRect.verticalNormalizedPosition = startAtTop ? 1f : 0f;

        // horizontalNormalizedPosition -> 0 = izquierda, 1 = derecha
        if (scrollRect.horizontal)
            scrollRect.horizontalNormalizedPosition = startAtLeft ? 0f : 1f;

        // Vuelve a forzar por si el Scrollbar se actualiza en este mismo frame
        Canvas.ForceUpdateCanvases();
    }
}

