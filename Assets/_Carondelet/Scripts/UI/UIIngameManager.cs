using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIIngameManager : MonoBehaviour
{
    [SerializeField]

    public UIManager uiManager;
    public AccessibilityManager accessibilityManager;

    [Header("Mensajes interacciones")]
    public GameObject interactionText;
    public GameObject doorInteractionText;

    public static UIIngameManager Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    [SerializeField]
    private float fadeDuration = 0.4f;

    [Header("Main menu")]
    public GameObject mainMenu;
    public GameObject mainMenuKeyLabel;

    [Header("3D item display")]
    [SerializeField]
    private GameObject itemPanel;

    [SerializeField]
    private TextMeshProUGUI itemNameText;

    [SerializeField]
    private TextMeshProUGUI itemDescriptionText;

    [SerializeField]
    private RawImage itemVideoPlayer;
    [SerializeField]
    private VideoPlayer videoPlayer;

    [SerializeField] private Material OriginalMat;

    public  UnityEvent CustomClose;


    [SerializeField] private CanvasGroup itemVideoCanvasGroup;
    [SerializeField] private int framesBeforeShow = 10;
    [SerializeField] private float videoFadeInDuration = 0.5f;

    [Header("Slideshow Panel (3D + Carrusel)")]
    [SerializeField] private GameObject slideshowPanel;
    [SerializeField] private RawImage slideshow3DRawImage;
    [SerializeField] private Image slideshowImageDisplay;
    [SerializeField] private TextMeshProUGUI slideshowTitleText;
    [SerializeField] private TextMeshProUGUI slideshowDescriptionText;
    [SerializeField] private Button slideshowNextButton;
    [SerializeField] private Button slideshowPreviousButton;
    private bool slideOnlyMode = false;

    private Coroutine slideshowCoroutine;
    private List<Sprite> slideshowSprites = new List<Sprite>();
    private int slideshowIndex = 0;
    private bool imagesLoaded = false;

    [Header("3D Render Settings")]
    [SerializeField]
    private Vector3 modelRotationSpeed = new Vector3(0, 30, 0);

    [SerializeField]
    private LayerMask modelRenderLayer;

    [SerializeField]
    private Vector3 modelPositionOffset = new Vector3(0, 0, 2);

    [Header("Painting display")]
    [SerializeField]
    private GameObject paintingPanel;

    [SerializeField]
    private Image paintingUIImage;

    [SerializeField]
    private TextMeshProUGUI paintingTitleText1;

    [SerializeField]
    private TextMeshProUGUI paintingSubTitleText1;

    [Header("Texture display")]
    [SerializeField]
    private GameObject texturePanel;

    [SerializeField]
    private Image textureUIImage;

    [SerializeField]
    private TextMeshProUGUI textureTitleText1;

    [SerializeField]
    private TextMeshProUGUI textureDescriptionText1;
    private bool cursorVisible = false;

    [Header("Loading Spinners")]
    [SerializeField] private GameObject objLoader;
    [SerializeField] private GameObject paintingLoader;
    [SerializeField] private GameObject textureLoader;
    [SerializeField] private GameObject slideshowLoader;

    private float SinCount = 0;
    bool OscilationMode = false;
    bool StuckMode = false;
    private bool _isFirstVideoPlay = true;
    private bool _showLoaderThisCycle = false;
    private string _videoUrlForward;
    private string _videoUrlReverse;
    private bool _playForwardNext = true;
    private bool _waitingFirstFrame = false;

    private List<Texture2D> capturedFrames = new List<Texture2D>();
    private bool capturingFrames = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (uiManager == null)
        {
            GameObject uiscript = GameObject.FindGameObjectWithTag("UIManager");
            if (uiscript != null)
            {
                uiManager = uiscript.GetComponent<UIManager>();
            }
            else
            {
                Debug.LogWarning("No se asigno un Uimanager en UIIngameManager");
            }
        }
        if (slideshowNextButton != null)
            slideshowNextButton.onClick.AddListener(OnNextSlide);
        if (slideshowPreviousButton != null)
            slideshowPreviousButton.onClick.AddListener(OnPreviousSlide);

        if (slideshowNextButton != null)
            slideshowNextButton.gameObject.SetActive(false);
        if (slideshowPreviousButton != null)
            slideshowPreviousButton.gameObject.SetActive(false);

        if (slideshowImageDisplay != null)
            slideshowImageDisplay.enabled = false;
        if (slideshow3DRawImage != null)
            slideshow3DRawImage.gameObject.SetActive(false);
    }

    public void ResetMaterial()
    {
        if (itemVideoPlayer != null && OriginalMat != null)
        {
            Debug.Log("Resetting material");
            itemVideoPlayer.material = OriginalMat;
        }
    }

    public void ShowPaintingLoader(bool show)
    {
        if (paintingLoader != null)
            paintingLoader.SetActive(show);
    }

    public void ShowTextureLoader(bool show)
    {
        if (textureLoader != null)
            textureLoader.SetActive(show);
    }

    public void ShowSlideshowLoader(bool show)
    {
        if (slideshowLoader != null)
            slideshowLoader.SetActive(show);
    }

    public void ShowObjLoader(bool show)
    {
        if (objLoader != null)
            objLoader.SetActive(show);
    }

    public void ShowInteractPrompt(bool isDoor)
    {
        if (isDoor)
        {
            if (doorInteractionText != null)
                doorInteractionText.SetActive(true);
        }
        else
        {
            if (interactionText != null)
                interactionText.SetActive(true);
        }
    }

    public void HideInteractPrompt(bool isDoor)
    {
        if (isDoor)
        {
            if (doorInteractionText != null)
                doorInteractionText.SetActive(false);
        }
        else
        {
            if (interactionText != null)
                interactionText.SetActive(false);
        }
    }

    public void ShowItemPanel(string name, string description)
    {
        uiManager.showCursor();
        if (itemPanel != null)
        {
            canvasGroup = itemPanel.GetComponent<CanvasGroup>();
        }
        itemVideoPlayer.gameObject.SetActive(false);
        accessibilityManager.RefreshAccessibilitySettings();
        itemNameText.text = name;
        itemDescriptionText.text = description;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, true, itemPanel));
    }

    public void HideItemPanel()
    {
        uiManager.hideCursor();
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, false, itemPanel));
        }
        CustomClose?.Invoke();
    }

    public void ShowVideoPanel(
        string name,
        string description,
        string urlForward,
        bool isOscilating,
        string urlReverse = null,
        Vector2 videoPosition = default,
        Vector3 videoScale = default
    )
    {
        OscilationMode = isOscilating;

        _videoUrlForward = urlForward;
        _videoUrlReverse = string.IsNullOrWhiteSpace(urlReverse) ? null : urlReverse;
        _playForwardNext = true;
        _isFirstVideoPlay = true;

        ShowObjLoader(true);
        itemVideoPlayer.gameObject.SetActive(true);
        if (itemVideoCanvasGroup != null)
        {
            itemVideoCanvasGroup.alpha = 0f;
            itemVideoCanvasGroup.interactable = false;
            itemVideoCanvasGroup.blocksRaycasts = false;
        }
        itemVideoPlayer.enabled = false;
        itemVideoPlayer.color = Color.white;

        ApplyVideoTransform(videoPosition, videoScale);

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.frameReady -= OnVideoFrameReady;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.loopPointReached -= OnVideoFinished;

        videoPlayer.waitForFirstFrame = true;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.playOnAwake = false;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.frameReady += OnVideoFrameReady;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.loopPointReached += OnVideoFinished;

        var rtExistente = itemVideoPlayer.texture as RenderTexture;
        UseExistingRenderTextureForVideo(rtExistente);
        PrepareAndPlayCurrent();
    }

    private void ApplyVideoTransform(Vector2 videoPosition, Vector3 videoScale)
    {
        var rt = itemVideoPlayer.rectTransform;
        rt.anchoredPosition = videoPosition;

        if (videoScale == default(Vector3) || videoScale == Vector3.zero)
            videoScale = Vector3.one;
        rt.localScale = videoScale;
    }

    private void PrepareAndPlayCurrent()
    {
        _receivedFrames = 0;
        _waitingFirstFrame = true;

        _showLoaderThisCycle = _isFirstVideoPlay;
        _isFirstVideoPlay = false;

        if (_showLoaderThisCycle)
        {
            if (itemVideoCanvasGroup != null)
                itemVideoCanvasGroup.alpha = 0f;
            itemVideoPlayer.enabled = false;
            ShowObjLoader(true);
        }

        string nextUrl = _playForwardNext || string.IsNullOrEmpty(_videoUrlReverse)
            ? _videoUrlForward
            : _videoUrlReverse;

        videoPlayer.url = nextUrl;
        videoPlayer.Prepare();
    }



    private void OnVideoPrepared(VideoPlayer vp)
    {
        _receivedFrames = 0;
        vp.Play();
    }



    private int _receivedFrames = 0;

    private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (_waitingFirstFrame)
        {
            _waitingFirstFrame = false;

            itemVideoPlayer.enabled = true;
            if (itemVideoCanvasGroup != null)
                itemVideoCanvasGroup.alpha = 1f;

            if (_showLoaderThisCycle)
                ShowObjLoader(false);
        }
    }



    private void OnVideoFinished(VideoPlayer vp)
    {
        if (OscilationMode && !string.IsNullOrEmpty(_videoUrlReverse))
        {
            _playForwardNext = !_playForwardNext;
            PrepareAndPlayCurrent();
        }
        else
        {
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("Video error: " + message);
        ShowObjLoader(false);
    }

    private void UseExistingRenderTextureForVideo(RenderTexture rt)
    {
        if (rt == null)
        {
            Debug.LogError("No hay RenderTexture asignada para el video.");
            return;
        }

        bool needRelease = false;

        if (rt.antiAliasing != 1)
        {
            rt.antiAliasing = 1;
            needRelease = true;
        }

#if UNITY_2020_2_OR_NEWER
        if (rt.bindTextureMS)
        {
            rt.bindTextureMS = false;
            needRelease = true;
        }
#else
#endif

        if (needRelease)
        {
            if (rt.IsCreated()) rt.Release();
            rt.Create();
        }

        itemVideoPlayer.texture = rt;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rt;
    }

    public void HideVideoPanel()
    {
        if (videoPlayer.isPlaying) videoPlayer.Stop();

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.frameReady -= OnVideoFrameReady;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.loopPointReached -= OnVideoFinished;

        itemVideoPlayer.enabled = false;
        itemVideoPlayer.gameObject.SetActive(false);

        ShowObjLoader(false);

        HideItemPanel();
    }

    public void ShowSlideShowPanel(
        string name,
        string description,
        bool showSlideOnly,
        GameObject modelPrefab,
        bool isOscilating,
        bool isStuck,
        URLSalon salon,
        List<string> imageNames
    )
    {
        slideOnlyMode = showSlideOnly;
        uiManager.showCursor();
        accessibilityManager.RefreshAccessibilitySettings();
        slideshowIndex = 0;
        imagesLoaded = false;
        slideshowSprites.Clear();
        slideshowTitleText.text = name;
        slideshowDescriptionText.text = description;
        if (slideshowPanel != null)
        {
            canvasGroup = slideshowPanel.GetComponent<CanvasGroup>();
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, true, slideshowPanel));
        }
        if (slideOnlyMode)
        {
            if (slideshow3DRawImage != null) slideshow3DRawImage.gameObject.SetActive(false);
        }
        else
        {
            if (slideshow3DRawImage != null) slideshow3DRawImage.gameObject.SetActive(true);
            if (modelPrefab != null)
            {
                OscilationMode = isOscilating;
                StuckMode = isStuck;
            }
        }

        if (imageNames != null && imageNames.Count > 0)
            StartCoroutine(DownloadImagesForSlideshow(salon, imageNames));
        else
            UpdateSlideshowButtons();
    }

    public void HideSlideShowPanel()
    {
        uiManager.hideCursor();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            canvasGroup = slideshowPanel.GetComponent<CanvasGroup>();
            fadeCoroutine = StartCoroutine(
                FadeCanvasGroup(canvasGroup, 1f, 0f, false, slideshowPanel)
            );
        }

        if (slideshowNextButton != null)
            slideshowNextButton.gameObject.SetActive(false);
        if (slideshowPreviousButton != null)
            slideshowPreviousButton.gameObject.SetActive(false);

        slideshowSprites.Clear();
        slideshowIndex = 0;
        imagesLoaded = false;

        if (slideshowImageDisplay != null)
        {
            slideshowImageDisplay.sprite = null;
            slideshowImageDisplay.enabled = false;
        }
        if (slideshow3DRawImage != null)
            slideshow3DRawImage.gameObject.SetActive(false);
    }

    public IEnumerator DownloadImagesForSlideshow(URLSalon salon, List<string> imageNames)
    {
        imagesLoaded = false;
        slideshowSprites = new List<Sprite>(new Sprite[imageNames.Count]);

        int completedDownloads = 0;
        string firstImageName = imageNames[0];
        this.ShowSlideshowLoader(true);

        for (int i = 0; i < imageNames.Count; i++)
        {
            int index = i;
            string imgName = imageNames[index];

            StartCoroutine(DownloadSingleImage(salon, imgName, (sprite, success) =>
            {
                completedDownloads++;

                if (success && sprite != null)
                {
                    slideshowSprites[index] = sprite;

                    if (imgName == firstImageName && slideshowIndex == 0 && slideOnlyMode)
                    {
                        if (slideshow3DRawImage != null) slideshow3DRawImage.gameObject.SetActive(false);
                        if (slideshowImageDisplay != null)
                        {
                            slideshowImageDisplay.enabled = true;
                            slideshowImageDisplay.sprite = sprite;
                            this.ShowSlideshowLoader(false);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Error al cargar imagen {imgName}, se dejara vacia.");
                    slideshowSprites[index] = null;
                }

                if (completedDownloads == imageNames.Count)
                {
                    imagesLoaded = true;
                    slideshowSprites.RemoveAll(s => s == null);
                    slideshowIndex = 0;

                    if (slideOnlyMode && slideshowSprites.Count > 0)
                    {
                        if (slideshowImageDisplay != null)
                        {
                            slideshowImageDisplay.enabled = true;
                            slideshowImageDisplay.sprite = slideshowSprites[0];
                        }
                    }

                    UpdateSlideshowButtons();
                }
            }));
        }

        yield break;
    }

    private IEnumerator DownloadSingleImage(URLSalon salon, string imgName, Action<Sprite, bool> onComplete)
    {
        string baseName = System.IO.Path.GetFileNameWithoutExtension(imgName);

        yield return StartCoroutine(GameManager.Instance.DownloadImageSprite(
            salon, baseName,
            sprite => { onComplete?.Invoke(sprite, true); },
            err => { Debug.LogWarning(err); onComplete?.Invoke(null, false); }
        ));
    }

    private void UpdateSlideshowButtons()
    {
        bool hasImages = (slideshowSprites.Count > 0);

        slideshowNextButton?.gameObject.SetActive(hasImages);
        slideshowPreviousButton?.gameObject.SetActive(hasImages);

        if (slideshowNextButton != null)
        {
            slideshowNextButton.gameObject.SetActive(hasImages);
            slideshowNextButton.interactable = hasImages;
        }

        if (slideshowPreviousButton != null)
        {
            slideshowPreviousButton.gameObject.SetActive(hasImages);
            slideshowPreviousButton.interactable = hasImages;
        }
    }

    public void OnNextSlide()
    {
        if (!imagesLoaded || slideshowSprites.Count == 0) return;

        if (slideOnlyMode)
        {
            slideshowIndex = (slideshowIndex + 1) % slideshowSprites.Count;
            if (slideshowSprites[slideshowIndex] != null && slideshowImageDisplay != null)
            {
                slideshowImageDisplay.enabled = true;
                slideshowImageDisplay.sprite = slideshowSprites[slideshowIndex];
            }
            return;
        }

        int totalItems = 1 + slideshowSprites.Count;
        slideshowIndex = (slideshowIndex + 1) % totalItems;

        if (slideshowIndex == 0)
        {
            if (slideshow3DRawImage != null) slideshow3DRawImage.gameObject.SetActive(true);
            if (slideshowImageDisplay != null) slideshowImageDisplay.enabled = false;
        }
        else
        {
            if (slideshow3DRawImage != null) slideshow3DRawImage.gameObject.SetActive(false);
            int imageIdx = slideshowIndex - 1;
            if (imageIdx < slideshowSprites.Count && slideshowSprites[imageIdx] != null)
            {
                slideshowImageDisplay.enabled = true;
                slideshowImageDisplay.sprite = slideshowSprites[imageIdx];
            }
        }
    }

    public void OnPreviousSlide()
    {
        if (!imagesLoaded) return;
        if (slideOnlyMode)
        {
            if (slideshowSprites.Count == 0) return;
            slideshowIndex = (slideshowIndex - 1 + slideshowSprites.Count) % slideshowSprites.Count;
            if (slideshowImageDisplay != null)
            {
                slideshowImageDisplay.enabled = true;
                slideshowImageDisplay.sprite = slideshowSprites[slideshowIndex];
            }
            return;
        }
        int totalItems = 1 + slideshowSprites.Count;
        slideshowIndex = (slideshowIndex - 1 + totalItems) % totalItems;
        if (slideshowIndex == 0)
        {
            if (slideshow3DRawImage != null) slideshow3DRawImage.gameObject.SetActive(true);
            if (slideshowImageDisplay != null) slideshowImageDisplay.enabled = false;
        }
        else
        {
            if (slideshow3DRawImage != null) slideshow3DRawImage.gameObject.SetActive(false);
            if (slideshowImageDisplay != null)
            {
                slideshowImageDisplay.preserveAspect = true;
                slideshowImageDisplay.enabled = true;
                slideshowImageDisplay.sprite = slideshowSprites[slideshowIndex - 1];
            }
        }
    }

    private void ConfigureModelForRender_Slideshow(GameObject model)
    {
        model.layer = LayerMask.NameToLayer("UI Model");
    }

    public void SetItemImage(Image container, Sprite sprite, Vector3 scale)
    {
        Image cont = container;
        if (cont != null)
        {
            cont.sprite = sprite;
            cont.enabled = (sprite != null);
            cont.preserveAspect = true;
            cont.transform.localScale = scale;
        }
    }

    public void UpdatePaintingImage(Sprite sprite, Vector3 imageScale)
    {
        SetItemImage(paintingUIImage, sprite, imageScale);
    }

    public void UpdateTextureImage(Sprite sprite, Vector3 imageScale)
    {
        SetItemImage(textureUIImage, sprite, imageScale);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            bool isAnyPanelOpen = (itemPanel != null && itemPanel.activeSelf)
                                || (texturePanel != null && texturePanel.activeSelf)
                                || (paintingPanel != null && paintingPanel.activeSelf)
                                || (slideshowPanel != null && slideshowPanel.activeSelf);

            if (isAnyPanelOpen)
            {
                if (itemPanel != null && itemPanel.activeSelf)
                {
                    HideItemPanel();
                    
                }
                else if (texturePanel != null && texturePanel.activeSelf)
                {
                    HideTexturePanel();
                }
                else if (paintingPanel != null && paintingPanel.activeSelf)
                {
                    HidePaintingPanel();
                }
                else if (slideshowPanel != null && slideshowPanel.activeSelf)
                {
                    HideSlideShowPanel();
                }
            }
            else
            {
                cursorVisible = !cursorVisible;
                if (cursorVisible)
                {
                    uiManager.showCursor();
                    mainMenuKeyLabel.SetActive(false);
                    uiManager.OpenPanel(mainMenu);
                }
                else
                {
                    uiManager.hideCursor();
                    mainMenuKeyLabel.SetActive(true);
                    uiManager.ClosePanel(mainMenu);
                }
            }
        }
    }

    public void ShowTexturePanel(string name, string description, Sprite itemImage, Vector3 imageScale)
    {
        uiManager.showCursor();
        if (texturePanel != null)
        {
            canvasGroup = texturePanel.GetComponent<CanvasGroup>();
        }

        accessibilityManager.RefreshAccessibilitySettings();
        textureTitleText1.text = name;
        textureDescriptionText1.text = description;

        SetItemImage(textureUIImage, itemImage, imageScale);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, true, texturePanel));
    }

    public void HideTexturePanel()
    {
        uiManager.hideCursor();
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(
                FadeCanvasGroup(canvasGroup, 1f, 0f, false, texturePanel)
            );
        }
    }

    public void ShowPaintingPanel(
        string name1,
        string subTitle1,
        Sprite itemImage,
        Vector3 imageScale
    )
    {
        uiManager.showCursor();
        if (paintingPanel != null)
        {
            canvasGroup = paintingPanel.GetComponent<CanvasGroup>();
        }
        accessibilityManager.RefreshAccessibilitySettings();
        paintingTitleText1.text = name1;
        paintingSubTitleText1.text = subTitle1;

        SetItemImage(paintingUIImage, itemImage, imageScale);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, true, paintingPanel));
    }

    public void HidePaintingPanel()
    {
        uiManager.hideCursor();
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(
                FadeCanvasGroup(canvasGroup, 1f, 0f, false, paintingPanel)
            );
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        group.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float startAlpha,
        float endAlpha,
        bool activate,
        GameObject panelObject
    )
    {
        if (activate)
            panelObject.SetActive(true);

        float elapsed = 0f;
        group.alpha = startAlpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }

        group.alpha = endAlpha;

        if (!activate)
            panelObject.SetActive(false);
    }

    public void changeMainMenuStatus()
    {
        cursorVisible = !cursorVisible;
    }

    public void SetTextureScale(Vector3 scale)
    {
        if (textureUIImage != null)
        {
            textureUIImage.rectTransform.localScale = scale;
        }
    }
}
