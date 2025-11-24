using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CanvasManagerBDC : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject HudPanel;
    [SerializeField] private GameObject BDCPanel;
    [SerializeField] private GameObject TitlePanel;

    [Header("Canvas Groups (si no existen se crearán)")]
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private CanvasGroup bdcGroup;
    [SerializeField] private CanvasGroup titleGroup;

    [Header("Tiempos")]
    [SerializeField] private float bdcFadeInDuration = 0.5f;
    [SerializeField] private float titleFadeInDuration = 0.5f;
    [SerializeField] private float visibleAfterTitle = 1.0f; 
    [SerializeField] private float fadeOutDuration = 0.4f;  

    [Header("Callback final")]
    [Tooltip("Se invoca cuando el HUD ya fue mostrado.")]
    [SerializeField] private UnityAction onHUDShown;

    private Coroutine _sequenceCo;

    private void Start()
    {
        onHUDShown += ShowHud;
    }

    private void Reset()
    {
        if (HudPanel == null) HudPanel = GameObject.Find("HudPanel");
        if (BDCPanel == null) BDCPanel = GameObject.Find("BDCPanel");
        if (TitlePanel == null && BDCPanel != null)
        {
            var t = BDCPanel.transform.Find("TitlePanel");
            if (t) TitlePanel = t.gameObject;
        }
    }

    private void OnEnable()
    {
        // CanvasGroups
        if (hudCanvasGroup == null && HudPanel)
            hudCanvasGroup = HudPanel.GetComponent<CanvasGroup>() ?? HudPanel.AddComponent<CanvasGroup>();
        if (BDCPanel && bdcGroup == null) bdcGroup = BDCPanel.GetComponent<CanvasGroup>() ?? BDCPanel.AddComponent<CanvasGroup>();
        if (TitlePanel && titleGroup == null) titleGroup = TitlePanel.GetComponent<CanvasGroup>() ?? TitlePanel.AddComponent<CanvasGroup>();

        // Estado inicial
        if (HudPanel) HudPanel.SetActive(false);
        if (BDCPanel) BDCPanel.SetActive(true);
        if (TitlePanel) TitlePanel.SetActive(true);

        SetGroupInstant(hudCanvasGroup, 0f, false);
        SetGroupInstant(bdcGroup, 0f, false);   
        SetGroupInstant(titleGroup, 0f, false);  

        if (_sequenceCo != null) StopCoroutine(_sequenceCo);
        _sequenceCo = StartCoroutine(Sequence());
    }

    private void OnDisable()
    {
        if (_sequenceCo != null) StopCoroutine(_sequenceCo);
    }

    private IEnumerator Sequence()
    {
        yield return FadeCanvasGroup(bdcGroup, 0f, 1f, bdcFadeInDuration);
        AudioManagerBDC.I.PlaySFX("BDC", volume: 1f);
        yield return FadeCanvasGroup(titleGroup, 0f, 1f, titleFadeInDuration);

        if (visibleAfterTitle > 0f)
            yield return new WaitForSeconds(visibleAfterTitle);

        var c1 = StartCoroutine(FadeCanvasGroup(titleGroup, titleGroup ? titleGroup.alpha : 1f, 0f, fadeOutDuration));
        var c2 = StartCoroutine(FadeCanvasGroup(bdcGroup, bdcGroup ? bdcGroup.alpha : 1f, 0f, fadeOutDuration));
        yield return c1;
        yield return c2;

        if (BDCPanel) BDCPanel.SetActive(false);
        if (TitlePanel) TitlePanel.SetActive(false);
        if (HudPanel) HudPanel.SetActive(true);


        onHUDShown?.Invoke();

        var c3 = StartCoroutine(FadeCanvasGroup(hudCanvasGroup, hudCanvasGroup ? hudCanvasGroup.alpha : 0f, 1f, fadeOutDuration));
        yield return c3;

    }

    // -------- Helpers --------
    private static void SetGroupInstant(CanvasGroup cg, float alpha, bool interactable)
    {
        if (!cg) return;
        cg.alpha = alpha;
        cg.blocksRaycasts = interactable && alpha > 0.001f;
        cg.interactable = interactable && alpha > 0.999f;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (!cg || duration <= 0f)
        {
            if (cg) cg.alpha = to;
            yield break;
        }

        cg.alpha = from;
        cg.blocksRaycasts = true; 
        cg.interactable = false;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
        cg.blocksRaycasts = to > 0.001f;
        cg.interactable = to > 0.999f;
    }

    private void ShowHud()
    {

        var playlist = new List<string> { "SuperBDC1", "SuperBDC2" };
        AudioManagerBDC.I.StartPlaylist(playlist, loop: true);
    }
}
