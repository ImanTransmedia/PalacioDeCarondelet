using UnityEngine;
using UnityEngine.UI;

public class ToolTipOrchestrator : MonoBehaviour
{
    [Header("Botones que activan cada tooltip (en el mismo orden)")]
    [SerializeField] private Button[] buttons;

    [SerializeField] private GameObject[] tooltips;

    [SerializeField] private int startSelectedIndex = -1;

    private void Awake()
    {
        // Validaciones mínimas
        if (buttons == null|| buttons.Length == 0 )
        {
            Debug.LogWarning("[ToolTipOrchestrator] Asigna los arrays de botones en el inspector");
            return;
        }

    }

    private void Start()
    {
        if (startSelectedIndex >= 0 && startSelectedIndex < tooltips.Length)
        {
            Select(startSelectedIndex);
        }
        else
        {
            for (int i = 0; i < buttons.Length; i++)
                SetButtonEnabled(i, true);
        }
    }

    public void Select(int index)
    {
        if (!IsValidIndex(index)) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            bool isSelected = (i == index);
            //SetButtonEnabled(i, !isSelected);       
        }
    }

    public void ResetAll()
    {
        for (int i = 0; i < buttons.Length; i++)
            SetButtonEnabled(i, true);
    }

    private void SetButtonEnabled(int i, bool enabled)
    {
        if (i < 0 || i >= buttons.Length || buttons[i] == null) return;
        buttons[i].interactable = enabled;


    }



    private bool IsValidIndex(int index)
    {
        if (buttons == null || tooltips == null) return false;

        bool ok = index >= 0 && index < buttons.Length && index < tooltips.Length;
        if (!ok) Debug.LogWarning($"[ToolTipOrchestrator] Índice fuera de rango: {index}");
        return ok;
    }
}
