using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class LanternChargeTextFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI label;

    [Header("Pulso mientras se usa")]
    [SerializeField] private float pulseDuration = 0.18f;
    [SerializeField] private float pulseScale = 1.18f;
    [SerializeField] private float shakeMagnitude = 14f;
    [SerializeField] private float shakeFrequency = 40f;
    [SerializeField] private AnimationCurve easeUp = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve easeDown = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Color peakColor = new Color(0.56f, 0.23f, 0.86f); 

    [Header("Formato")]
    [SerializeField] private string format = "{0}%"; 

    private RectTransform _rt;
    private Vector2 _basePos;
    private Vector3 _baseScale;
    private Color _baseColor;

    private float _shownCharge;
    private bool _wasUsing;
    private Coroutine _pulseLoopCo;
    private Coroutine _onePulseCo;

    private void Reset()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (!label) label = GetComponent<TextMeshProUGUI>();
        _basePos = _rt.anchoredPosition;
        _baseScale = _rt.localScale;
        _baseColor = label ? label.color : Color.white;
    }

    private void OnEnable()
    {
        _shownCharge = SafeCharge();
        SetText(_shownCharge);

        _rt.anchoredPosition = _basePos;
        _rt.localScale = _baseScale;
        if (label) label.color = _baseColor;

        _wasUsing = SafeIsUsing();
        if (_wasUsing) StartPulseLoop();
    }

    private void OnDisable()
    {
        StopPulseLoop(resetVisuals: true);
    }

    private void Update()
    {
        float current = SafeCharge();
        if (!Mathf.Approximately(current, _shownCharge))
        {
            _shownCharge = current;
            SetText(_shownCharge);
        }

        bool isUsing = SafeIsUsing();
        if (isUsing != _wasUsing)
        {
            _wasUsing = isUsing;
            if (isUsing) StartPulseLoop();
            else StopPulseLoop(resetVisuals: true);
        }
    }

    private float SafeCharge()
    {
        var gm = GameManagerBDC.Instance;
        if (gm != null)
        {
            return gm.LanternCharge;
        }
        return _shownCharge;
    }

    private bool SafeIsUsing()
    {
        var gm = GameManagerBDC.Instance;
        return gm != null && gm.isUsingLantern;
    }

    private void SetText(float value)
    {
        if (!label) return;

        int v = Mathf.RoundToInt(value);
        label.text = string.IsNullOrEmpty(format) ? v.ToString() : string.Format(format, v);
    }

    private void StartPulseLoop()
    {
        StopPulseLoop();
        _pulseLoopCo = StartCoroutine(Co_PulseLoopWhileUsing());
    }

    private void StopPulseLoop(bool resetVisuals = false)
    {
        if (_pulseLoopCo != null) StopCoroutine(_pulseLoopCo);
        _pulseLoopCo = null;

        if (_onePulseCo != null) StopCoroutine(_onePulseCo);
        _onePulseCo = null;

        if (resetVisuals)
        {
            _rt.localScale = _baseScale;
            _rt.anchoredPosition = _basePos;
            if (label) label.color = _baseColor;
        }
    }

    private IEnumerator Co_PulseLoopWhileUsing()
    {
        while (SafeIsUsing())
        {
            _onePulseCo = StartCoroutine(Co_OnePulse());
            yield return _onePulseCo;
        }

        _rt.localScale = _baseScale;
        _rt.anchoredPosition = _basePos;
        if (label) label.color = _baseColor;
    }

    private IEnumerator Co_OnePulse()
    {
        float dur = Mathf.Max(0.0001f, pulseDuration);
        float half = dur * 0.5f;
        float t = 0f;

        if (label) label.color = _baseColor;

        while (t < half)
        {
            if (!SafeIsUsing()) yield break;

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            float e = easeUp != null ? easeUp.Evaluate(k) : k;

            _rt.localScale = Vector3.LerpUnclamped(_baseScale, _baseScale * Mathf.Max(1f, pulseScale), e);

            float x = Mathf.Sin((Time.unscaledTime + t) * shakeFrequency * Mathf.PI * 2f) * shakeMagnitude;
            _rt.anchoredPosition = _basePos + new Vector2(x, 0f);
            yield return null;
        }

        if (label) label.color = peakColor;

        t = 0f;
        while (t < half)
        {
            if (!SafeIsUsing()) yield break;

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            float e = easeDown != null ? easeDown.Evaluate(k) : k;

            _rt.localScale = Vector3.LerpUnclamped(_baseScale * Mathf.Max(1f, pulseScale), _baseScale, e);

            float x = Mathf.Sin((Time.unscaledTime + t + half) * shakeFrequency * Mathf.PI * 2f) * shakeMagnitude;
            _rt.anchoredPosition = _basePos + new Vector2(x, 0f);
            yield return null;
        }

        _rt.localScale = _baseScale;
        _rt.anchoredPosition = _basePos;
        if (label) label.color = _baseColor;
    }
}
