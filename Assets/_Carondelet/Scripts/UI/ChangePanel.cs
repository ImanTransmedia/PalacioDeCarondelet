using UnityEngine;

public class ChangePanel : MonoBehaviour
{
    [SerializeField] GameObject creditosPanel;
    [SerializeField] GameObject infoPanel;

    public void ShowCreditosPanel()
    {
        creditosPanel.SetActive(true);
        infoPanel.SetActive(false);
    }

    public void ShowInfoPanel()
    {
        creditosPanel.SetActive(false);
        infoPanel.SetActive(true);
    }
}
