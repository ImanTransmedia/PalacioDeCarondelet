using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

public class ItemDisplay : MonoBehaviour
{
    [Header("Configuracion del Objeto")]
    [SerializeField] private string objectName;
    [SerializeField] private LocalizedString itemName;
    [SerializeField] private LocalizedString itemDescription;
    [SerializeField] public Vector3 eyeOffset = new Vector3(0f, 0f, 0f);
    public bool oscilate = false;
    public bool stuck = false;

    [Space(10)]
    [Header("VideoDisplay")]
    [SerializeField] private bool isVideoDisplay = false;
    [SerializeField] private string videoName = "video";
    [SerializeField] private string videoReverse = "videoReverse";
    [Header("Item Video Player")]
    [SerializeField] private Vector2 videoPosition = new Vector2(0, 0);
    [SerializeField] private Vector3 videoScale = new Vector3(1, 1, 1);

    [Space(10)]
    [Header("Slideshow de Imagenes")]
    [SerializeField] private bool isSlideShow = false;
    [SerializeField] private bool showSlideOnly = false;
    [SerializeField] private URLSalon salon = URLSalon.SalonAmarillo;
    [SerializeField] private List<string> imageNames = new List<string>();

    [Space(10)]
    [Header("Eventos")]
    public UnityEvent onDisplayStart;
    public UnityEvent onDisplayEnd;

    private bool isUIOpen = false;
    private bool _filled = false;
    private bool _refreshing = false;
    private Coroutine _waiter;

    private void OnEnable()
    {
        TrySubscribeToConfigReady();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnConfigReady -= HandleConfigReady;

        if (_waiter != null)
        {
            StopCoroutine(_waiter);
            _waiter = null;
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.Config != null && !_filled)
        {
            Debug.Log($"[ItemDisplay:{name}] config lista en Start");
            FillOnceAtPlay();
            _filled = true;
        }
    }

    private void TrySubscribeToConfigReady()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnConfigReady -= HandleConfigReady;
            GameManager.Instance.OnConfigReady += HandleConfigReady;
        }
        else
        {
            _waiter = StartCoroutine(WaitForGMThenSubscribe());
        }
    }

    private IEnumerator WaitForGMThenSubscribe()
    {
        while (GameManager.Instance == null)
            yield return null;

        GameManager.Instance.OnConfigReady -= HandleConfigReady;
        GameManager.Instance.OnConfigReady += HandleConfigReady;

        if (GameManager.Instance.Config != null && !_filled)
            HandleConfigReady();
    }

    private void HandleConfigReady()
    {
        if (_filled) return;

        Debug.Log($"[ItemDisplay:{name}] OnConfigReady");
        FillOnceAtPlay();
        _filled = true;

        GameManager.Instance.OnConfigReady -= HandleConfigReady;
    }

    private bool TryGetMyConfig(out InteractableEntry config)
    {
        config = null;

        var autofiller = gameObject.GetComponent<InteractuableAutofiller>();
        if (autofiller.IsUnityNull() || !autofiller.isActiveAndEnabled)
        {
            Debug.LogWarning($"[ItemDisplay:{name}] sin InteractuableAutofiller");
            return false;
        }

        var objeto = autofiller.objeto;
        if (GameManager.Instance != null && GameManager.Instance.TryGetObjetoByName(objeto, out config))
            return true;

        Debug.LogWarning($"[ItemDisplay:{name}] no se encontro '{objeto}'");
        return false;
    }

    private void ApplyFromConfig(InteractableEntry config)
    {
        objectName = config.identifier;
        eyeOffset = config.eyeOffset;
        oscilate = config.oscilate;

        if (System.Enum.TryParse<URLSalon>(config.salonDescarga, out var parsedSalon))
            salon = parsedSalon;

        isVideoDisplay = config.isVideo;
        videoName = config.videoName;
        videoReverse = config.videoReverse;
        videoPosition = config.videoPosition;
        videoScale = config.videoScale;

        isSlideShow = isVideoDisplay ? false : true;
        showSlideOnly = isSlideShow ? true : false;
        imageNames = (config.imageNames != null && isSlideShow) ? new List<string>(config.imageNames) : new List<string>();
    }

    public void FillOnceAtPlay()
    {
        if (TryGetMyConfig(out var config))
        {
            Debug.Log($"[ItemDisplay:{name}] aplicando '{config.identifier}'");
            ApplyFromConfig(config);
        }
    }

    public void OnInteract()
    {
        if (!_refreshing)
            StartCoroutine(RefreshApplyAndToggle());
    }

    private IEnumerator RefreshApplyAndToggle()
    {
        _refreshing = true;

        bool ok = false;
        yield return StartCoroutine(GameManager.Instance.ReloadConfigCoroutine(done => ok = done));

        if (ok && TryGetMyConfig(out var cfg))
        {
            Debug.Log($"[ItemDisplay:{name}] re-aplicando config");
            ApplyFromConfig(cfg);
        }

        if (!isUIOpen)
        {
            if (isSlideShow) ShowSlideShowUI();
            else if (isVideoDisplay) ShowVideoUI();
        }
        else
        {
            if (isSlideShow) CloseSlideShowUI();
            else if (isVideoDisplay) CloseVideoUI();
        }

        _refreshing = false;
    }

    private void ShowVideoUI()
    {
        string n = itemName.GetLocalizedString();
        string d = itemDescription.GetLocalizedString();
        StartCoroutine(OpenVideoResolved(n, d));
    }

    private IEnumerator OpenVideoResolved(string name, string description)
    {
        UIIngameManager.Instance.ShowObjLoader(true);
        UIIngameManager.Instance.ShowItemPanel(name, description);

        isUIOpen = true;
        onDisplayStart?.Invoke();

        string forwardUrl = null;
        string reverseUrl = null;

        yield return StartCoroutine(GameManager.Instance.ResolveVideoUrl(
            videoName,
            url => { forwardUrl = url; },
            err => { Debug.LogError(err); }
        ));

        if (!string.IsNullOrWhiteSpace(videoReverse))
        {
            yield return StartCoroutine(GameManager.Instance.ResolveVideoUrl(
                videoReverse,
                url => { reverseUrl = url; },
                err => { Debug.LogWarning(err); }
            ));
        }

        if (string.IsNullOrEmpty(forwardUrl))
        {
            UIIngameManager.Instance.ShowObjLoader(false);
            yield break;
        }

        UIIngameManager.Instance.ShowVideoPanel(
            name,
            description,
            forwardUrl,
            oscilate,
            string.IsNullOrWhiteSpace(reverseUrl) ? null : reverseUrl,
            videoPosition,
            videoScale
        );
    }

    private void CloseVideoUI()
    {
        UIIngameManager.Instance.HideVideoPanel();
        isUIOpen = false;
        onDisplayEnd?.Invoke();
        UIIngameManager.Instance.CustomClose?.Invoke();
    }

    private void ShowSlideShowUI()
    {
        string n = itemName.GetLocalizedString();
        string d = itemDescription.GetLocalizedString();

        GameObject prefabToShow = null;

        UIIngameManager.Instance.ShowSlideShowPanel(
            n,
            d,
            showSlideOnly,
            prefabToShow,
            oscilate,
            stuck,
            salon,
            imageNames
        );

        isUIOpen = true;
        onDisplayStart?.Invoke();
    }

    private void CloseSlideShowUI()
    {
        UIIngameManager.Instance.HideSlideShowPanel();
        isUIOpen = false;
        onDisplayEnd?.Invoke();
        UIIngameManager.Instance.CustomClose?.Invoke();
    }
}
