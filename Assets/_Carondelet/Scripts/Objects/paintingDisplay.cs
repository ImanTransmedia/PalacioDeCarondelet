using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.UIElements;

public class paintingDisplay : MonoBehaviour
{
    [Header("Configuracion del Objeto")]
    [SerializeField] private string objectName;
    public LocalizedString itemName1;
    public LocalizedString itemSubTitle1;
    [SerializeField] public Vector3 eyeOffset = new Vector3(0f, 0f, 0f);
    public bool isInCarrousel = false;
    public int indexInCarrousel = 0;

    [Header("Imagen")]
    public URLSalon salon;
    public string imageName;
    public Sprite itemImage;
    public Vector3 imageScale = Vector3.one;

    [Header("Eventos")]
    public UnityEvent onDisplayStart;
    public UnityEvent onDisplayEnd;

    private bool isUIOpen = false;
    private bool _refreshing = false;
    private bool _filled = false;
    private Coroutine _waiter;

    private void OnEnable()
    {
        TrySubscribeToConfigReady();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnConfigReady -= HandleConfigReady;
        }

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
        {
            FillOnceAtPlay();
            _filled = true;
        }

        _waiter = null;
    }

    private void HandleConfigReady()
    {
        if (!_filled)
        {
            FillOnceAtPlay();
            _filled = true;
        }
    }

    // ---------- Config ----------
    private bool TryGetMyConfig(out InteractableEntry config)
    {
        config = null;

        var autofiller = gameObject.GetComponent<InteractuableAutofiller>();
        if (autofiller.IsUnityNull() || !autofiller.isActiveAndEnabled)
        {
            Debug.LogWarning($"[PaintingDisplay:{name}] sin InteractuableAutofiller");
            return false;
        }

        var objeto = autofiller.objeto;
        if (GameManager.Instance != null && GameManager.Instance.TryGetObjetoByName(objeto, out config))
        {
            return true;
        }

        Debug.LogWarning($"[PaintingDisplay:{name}] no se encontro '{objeto}'");
        return false;
    }

    private void ApplyFromConfig(InteractableEntry config)
    {
        objectName = config.identifier;
        eyeOffset = config.eyeOffset;
        imageName = config.imageName;
        imageScale = config.videoScale;

        if (Enum.TryParse<URLSalon>(config.salonDescarga, out var parsedSalon))
            salon = parsedSalon;
    }

    public void FillOnceAtPlay()
    {
        if (TryGetMyConfig(out var config))
        {
            ApplyFromConfig(config);
        }
    }

    // ---------- Interaccion con refresh ----------
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
            ApplyFromConfig(cfg);
        }

        if (!isUIOpen) ShowItemUI();
        else CloseItemUI();

        _refreshing = false;
    }

    // ---------- UI ----------
    public void ShowItemUI()
    {
        string name1 = itemName1.GetLocalizedString();
        string subTitle1 = itemSubTitle1.GetLocalizedString();

        UIIngameManager.Instance.ShowPaintingLoader(true);

        if (itemImage != null)
        {
            ShowImage(name1, subTitle1);
        }
        else
        {
            StartCoroutine(GameManager.Instance.DownloadImageSprite(
                salon,
                imageName,
                sprite =>
                {
                    itemImage = sprite;
                    ShowImage(name1, subTitle1);
                },
                err =>
                {
                    Debug.LogError(err);
                    UIIngameManager.Instance.ShowPaintingLoader(false);
                }
            ));
        }
    }

    private void ShowImage(string name, string description)
    {
        UIIngameManager.Instance.ShowPaintingPanel(name, description, itemImage, imageScale);
        UIIngameManager.Instance.ShowPaintingLoader(false);

        isUIOpen = true;
        onDisplayStart?.Invoke();

        if (isInCarrousel)
        {
            CarouselManager carouselManager = FindFirstObjectByType<CarouselManager>();
            if (carouselManager != null)
                carouselManager.OpenCarouselAtIndex(indexInCarrousel);
        }
    }

    private void CloseItemUI()
    {
        isUIOpen = false;
        UIIngameManager.Instance.HidePaintingPanel();
        onDisplayEnd?.Invoke();
    }
}
