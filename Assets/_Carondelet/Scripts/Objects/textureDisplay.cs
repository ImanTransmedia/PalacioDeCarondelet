using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

public class textureDisplay : MonoBehaviour
{
    [Header("Configuracion del Objeto")]
    [SerializeField] private string objectName;
    [SerializeField] public LocalizedString itemName;
    [SerializeField] private LocalizedString itemDescription;
    public Vector3 displayScale = Vector3.one;
    [SerializeField] public Vector3 eyeOffset = new Vector3(0f, 0f, 0f);

    [Space(10)]
    [Header("Downloaded Sprite")]
    [SerializeField] private URLSalon salon;
    [SerializeField] private string imageName;
    [SerializeField] private Sprite itemImage;
    [SerializeField] private Vector3 imageScale = new Vector3(1f, 1f, 1f);

    [Space(10)]
    [Header("Eventos")]
    public UnityEvent onDisplayStart;
    public UnityEvent onDisplayEnd;

    private bool isUIOpen = false;
    private bool _filled = false;
    private bool _refreshing = false;

    private HUDManager hudManager;
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
        hudManager = FindFirstObjectByType<HUDManager>();

        if (GameManager.Instance != null && GameManager.Instance.Config != null && !_filled)
        {
            Debug.Log($"[TextureDisplay:{name}] config lista en Start");
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

        Debug.Log($"[TextureDisplay:{name}] OnConfigReady");
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
            Debug.LogWarning($"[TextureDisplay:{name}] sin InteractuableAutofiller");
            return false;
        }

        var objeto = autofiller.objeto;
        if (GameManager.Instance != null && GameManager.Instance.TryGetObjetoByName(objeto, out config))
            return true;

        Debug.LogWarning($"[TextureDisplay:{name}] no se encontro '{objeto}'");
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
            Debug.Log($"[TextureDisplay:{name}] aplicando '{config.identifier}'");
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
            Debug.Log($"[TextureDisplay:{name}] re-aplicando config");
            ApplyFromConfig(cfg);
        }

        if (!isUIOpen) ShowItemUI();
        else CloseItemUI();

        _refreshing = false;
    }

    private void ShowItemUI()
    {
        string n = itemName.GetLocalizedString();
        string d = itemDescription.GetLocalizedString();

        UIIngameManager.Instance.SetTextureScale(displayScale);
        UIIngameManager.Instance.ShowTextureLoader(true);
        UIIngameManager.Instance.ShowTexturePanel(n, d, itemImage, imageScale);

        isUIOpen = true;
        onDisplayStart?.Invoke();

        if (itemImage != null)
        {
            UIIngameManager.Instance.ShowTextureLoader(false);
            return;
        }

        StartCoroutine(GameManager.Instance.DownloadImageSprite(
            salon,
            imageName,
            sprite =>
            {
                itemImage = sprite;
                UIIngameManager.Instance.UpdateTextureImage(itemImage, imageScale);
                UIIngameManager.Instance.ShowTextureLoader(false);
            },
            err =>
            {
                Debug.LogError(err);
                UIIngameManager.Instance.ShowTextureLoader(false);
            }
        ));
    }

    private void CloseItemUI()
    {
        UIIngameManager.Instance.HideTexturePanel();
        isUIOpen = false;
        onDisplayEnd?.Invoke();
    }
}
