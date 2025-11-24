using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ScoreTextFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI label;

    [Header("Efecto de pulso al subir")]
    [SerializeField] private float pulseDuration = 0.18f;
    [SerializeField] private float pulseScale = 1.18f;
    [SerializeField] private float shakeMagnitude = 14f;
    [SerializeField] private float shakeFrequency = 40f;
    [SerializeField] private AnimationCurve easeUp = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve easeDown = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Color peakColor = Color.red;

    [Header("Formato")]
    [SerializeField] private string format = "{0}";

    private RectTransform _rt;
    private Vector2 _basePos;
    private Vector3 _baseScale;
    private Color _baseColor;

    private int _shownScore;
    private Coroutine _pulseCo;

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
        _shownScore = SafeScore();
        SetText(_shownScore);
        _rt.anchoredPosition = _basePos;
        _rt.localScale = _baseScale;
        if (label) label.color = _baseColor;
    }

    private void Update()
    {
        int current = SafeScore();
        if (current != _shownScore)
        {
            bool increased = current > _shownScore;
            _shownScore = current;
            SetText(_shownScore);
            if (increased)
            {
                if (_pulseCo != null) StopCoroutine(_pulseCo);
                _pulseCo = StartCoroutine(Co_Pulse());
            }
        }
    }

    private int SafeScore()
    {
        return GameManagerBDC.Instance != null ? GameManagerBDC.Instance.Score : _shownScore;
    }

    private void SetText(int value)
    {
        if (!label) return;
        label.text = string.IsNullOrEmpty(format) ? value.ToString() : string.Format(format, value);
    }

    private IEnumerator Co_Pulse()
    {
        float dur = Mathf.Max(0.0001f, pulseDuration);
        float half = dur * 0.5f;
        float t = 0f;
        if (label) label.color = _baseColor;

        while (t < half)
        {
            t += Time.deltaTime;
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
            t += Time.deltaTime;
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
