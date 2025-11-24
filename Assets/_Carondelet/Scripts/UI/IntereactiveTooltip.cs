using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

public class IntereactiveTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public CanvasGroup panelCanvasGroup;
    public TextMeshProUGUI targetText;
    public LocalizedString textoLocalizado;
    public float fadeDuration = 0.2f;
    private bool lastState = false;

    public Transform contenidoTransform;

    public UnityEvent OnHoverEnter;
    public UnityEvent OnHoverExit;
    public UnityEvent OnClick;
    public List<GameObject> buttonList;

    public Vector3 dioramaTargetRotation;
    public float zoomDistance = 0.5f;

    public bool state = false;

    [Header("Otros Tooltips a desactivar")]
    public List<IntereactiveTooltip> otrosTooltips;

    private void SetPanelVisibility(bool visible)
    {
        StopAllCoroutines();
        StartCoroutine(FadePanel(visible ? 1f : 0f));
    }

  private System.Collections.IEnumerator FadePanel(float targetAlpha)
{
    panelCanvasGroup.blocksRaycasts = false;
    panelCanvasGroup.interactable = false;
    
    float startAlpha = panelCanvasGroup.alpha;
    float elapsedTime = 0f;

    while (elapsedTime < fadeDuration)
    {
        elapsedTime += Time.deltaTime;
        panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
        yield return null;
    }

    panelCanvasGroup.alpha = targetAlpha;

   panelCanvasGroup.interactable = targetAlpha == 1f;
panelCanvasGroup.blocksRaycasts = targetAlpha == 1f;
}

    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }
    public void OnPointerClick(PointerEventData eventData) { }

public void changeState()
{
    bool newState = !state;

    if (newState)
    {
        foreach (var tooltip in otrosTooltips)
        {
            if (tooltip != null && tooltip != this)
            {
                tooltip.state = false;
                tooltip.SetPanelVisibility(false);
            }
        }
    }

    state = newState;
    SetPanelVisibility(state); 
}

    private void Update()
    {
           if (state != lastState)
            {
                SetPanelVisibility(state);
                lastState = state;
            }
    }

public void SetTargetTextFromContenido()
{
    changeState();

    if (contenidoTransform != null)
    {
        var localizeEvent = contenidoTransform.GetComponent<LocalizeStringEvent>();

        if (localizeEvent != null)
        {
            var localizedString = localizeEvent.StringReference;

            if (localizedString != null)
            {

                localizedString.StringChanged -= OnLocalizedStringChanged;
                localizedString.StringChanged += OnLocalizedStringChanged;
                localizedString.RefreshString();
            }
        }
        else
        {
            Debug.LogWarning("contenidoTransform no tiene un componente LocalizeStringEvent.");
        }
    }

    for (int i = 0; i < buttonList.Count; i++)
    {
        if (buttonList[i] != null)
        {
            buttonList[i].SetActive(i == 0);
        }
    }
}

    private void OnLocalizedStringChanged(string localizedText)
    {
        if (targetText != null)
        {
            targetText.text = localizedText;
        }
        textoLocalizado.StringChanged -= OnLocalizedStringChanged;
    }
}
