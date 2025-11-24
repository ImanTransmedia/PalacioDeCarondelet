using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour, IPointerClickHandler
{
    [Header("Video principal")]
    public VideoPlayer video;

    public Vector3 eyeOffset;

    [Header("Foco de cámara (opcional)")]
    public Transform cameraPivot;

    [Header("Área de tap / click")]
    public RectTransform tapArea;

    [Header("Panel de previews (vistos)")]
    public RectTransform panelVistos;
    public CanvasGroup panelVistosGroup;
    public float panelOculto = -480f;
    public float panelVisible = 0f;

    [Header("Barra de acciones")]
    public RectTransform panelAcciones;
    public CanvasGroup panelAccionesGroup;
    public float panelAccionesOculto = -480f;
    public float panelAccionesVisible = 0f;

    [Header("Botones y overlays")]
    public CanvasGroup botonPlayGroup;
    public CanvasGroup botonPauseGroup;
    public CanvasGroup botonRetro1sGroup;
    public CanvasGroup botonAdel1sGroup;

    [Header("Duraciones UI")]
    public float durSlide = 0.25f;
    public float durFade = 0.2f;
    public float autoOcultarSeg = 2f;
    public float overlaySkipSeg = 0.6f;

    [Header("Salto dentro del video")]
    public float saltoSeg = 10f;

    [Header("Previews")]
    public float thumbnailTime = 1f;
    public Image[] previewSlots;

    [Tooltip("Límite de pasos al buscar el siguiente video válido (wrap-around).")]
    public int searchGuardLimit = 500;

    private int _currentAbsIndex = 1;

    private readonly Dictionary<int, string> _urlCache = new Dictionary<int, string>();
    private readonly HashSet<int> _notFoundCache = new HashSet<int>();
    private readonly Dictionary<int, Sprite> _thumbCache = new Dictionary<int, Sprite>();

    private int[] _slotToAbsIndex;

    float ultimoInputTime = -999f;
    Vector2 ultimoTapPos;
    float ultimoTapTime;
    float ventanaDobleTap = 0.3f;
    Coroutine slideRutina;


    private FirstPersonMovement firstPerson;

    void Awake()
    {
        if (panelVistos != null)
        {
            var p = panelVistos.anchoredPosition;
            p.y = panelOculto;
            panelVistos.anchoredPosition = p;
        }
        SetGroup(panelVistosGroup, 0f, false);
        SetGroup(botonPauseGroup, 0f, false);
        SetGroup(botonPlayGroup, 0f, false);
        SetGroup(botonRetro1sGroup, 0f, false);
        SetGroup(botonAdel1sGroup, 0f, false);
        SetGroup(panelAccionesGroup, 0f, false);
    }

    void OnEnable()
    {
        if (video != null)
        {
            video.started += OnVideoStarted;
            video.loopPointReached += OnVideoFinished;
        }
    }

    void OnDisable()
    {
        if (video != null)
        {
            video.started -= OnVideoStarted;
            video.loopPointReached -= OnVideoFinished;
        }
    }

    void Start()
    {
        firstPerson = FindFirstObjectByType<FirstPersonMovement>();
        if (firstPerson != null) firstPerson.moveSpeed = 0f;

        SetupPreviewClickHandlers();

        SincronizarEstadoInicial();
        StartCoroutine(InicioDetectar4YPreparar());
    }

    private void SetupPreviewClickHandlers()
    {
        if (previewSlots == null) return;

        _slotToAbsIndex = new int[previewSlots.Length];
        for (int i = 0; i < _slotToAbsIndex.Length; i++) _slotToAbsIndex[i] = -1;

        for (int i = 0; i < previewSlots.Length; i++)
        {
            var img = previewSlots[i];
            if (img == null) continue;

            img.raycastTarget = true;
            img.color = Color.white; // asegura opacidad total en UI

            var btn = img.GetComponent<Button>();
            if (btn == null) btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            int captured = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => UI_PlayPreviewSlot(captured));
            btn.interactable = false;
        }
    }

    private IEnumerator InicioDetectar4YPreparar()
    {
        for (int i = 1; i <= 4; i++)
            yield return StartCoroutine(EnsureUrlCached(i));

        if (_urlCache.ContainsKey(1))
        {
            _currentAbsIndex = 1;
            CargarYReproducir(_urlCache[1]);
        }
        else
        {
            // No hay ni el 1
            yield break;
        }

        yield return StartCoroutine(ActualizarPreviews());
    }

    private void CargarYReproducir(string url)
    {
        if (video == null) return;
        video.source = VideoSource.Url;
        video.url = url;
        video.Prepare();
        video.prepareCompleted += OnVideoPreparedAndPlay;
    }

    void OnVideoPreparedAndPlay(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPreparedAndPlay;
        vp.Play();
        OcultarPanel();
    }

    void OnVideoStarted(VideoPlayer vp)
    {
        OcultarUITodoForzado();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(UI_NextVideo_Coroutine());
    }

    private IEnumerator EnsureUrlCached(int absIndex)
    {
        if (_urlCache.ContainsKey(absIndex) || _notFoundCache.Contains(absIndex))
            yield break;

        bool done = false;
        string resolved = null;

        yield return StartCoroutine(GameManager.Instance.ResolveSalonAzulVideoUrl(
            absIndex,
            url => { resolved = url; done = true; },
            err => { resolved = null; done = true; }
        ));

        if (!done) yield break;

        if (!string.IsNullOrEmpty(resolved))
        {
            _urlCache[absIndex] = resolved;
            _notFoundCache.Remove(absIndex);
        }
        else
        {
            _notFoundCache.Add(absIndex);
        }
    }

    private IEnumerator FindNextValidIndexCoroutine(int fromExclusive, Action<int> onFoundOrMinusOne)
    {
        if (_urlCache.Count == 0 && _notFoundCache.Count == 0)
        {
            onFoundOrMinusOne?.Invoke(-1);
            yield break;
        }

        int guard = 0;
        int probe = fromExclusive;

        while (guard++ < searchGuardLimit)
        {
            // avanza uno
            probe++;
            if (probe > int.MaxValue - 1) probe = 1; 

            if (probe == fromExclusive)
            {
                onFoundOrMinusOne?.Invoke(-1);
                yield break;
            }

            // asegurar en cache
            yield return StartCoroutine(EnsureUrlCached(probe));

            if (_urlCache.ContainsKey(probe))
            {
                onFoundOrMinusOne?.Invoke(probe);
                yield break;
            }

        }

        // Guard rail
        onFoundOrMinusOne?.Invoke(-1);
    }

    private IEnumerator ActualizarPreviews()
    {
        if (previewSlots == null || previewSlots.Length == 0) yield break;

        int nextBase = _currentAbsIndex;

        for (int s = 0; s < previewSlots.Length; s++)
        {
            Image slot = previewSlots[s];
            if (slot == null) continue;

            int foundIdx = -1;
            bool finished = false;
            yield return StartCoroutine(FindNextValidIndexCoroutine(nextBase, idx => { foundIdx = idx; finished = true; }));
            if (!finished) yield break;

            var btn = slot.GetComponent<Button>();

            if (foundIdx > 0 && _urlCache.TryGetValue(foundIdx, out string url))
            {
                _slotToAbsIndex[s] = foundIdx;

                if (_thumbCache.TryGetValue(foundIdx, out Sprite cached))
                {
                    slot.sprite = cached;
                    slot.enabled = true;
                    slot.color = Color.white; // visibilidad total
                    if (btn) btn.interactable = true;
                }
                else
                {
                    yield return StartCoroutine(CrearThumbnail(url, thumbnailTime, sprite =>
                    {
                        if (sprite != null)
                        {
                            _thumbCache[foundIdx] = sprite;
                            slot.sprite = sprite;
                            slot.enabled = true;
                            slot.color = Color.white;
                            if (btn) btn.interactable = true;
                        }
                        else
                        {
                            slot.sprite = null;
                            slot.enabled = true;
                            slot.color = Color.white;
                            if (btn) btn.interactable = true;
                        }
                    }));
                }

                nextBase = foundIdx;
            }
            else
            {
                _slotToAbsIndex[s] = -1;
                slot.sprite = null;
                slot.enabled = false;
                slot.color = new Color(1, 1, 1, 1);
                if (btn) btn.interactable = false;
            }
        }
    }

    private IEnumerator CrearThumbnail(string url, float atTimeSeconds, Action<Sprite> onDone)
    {
        var go = new GameObject("VP_Thumb_Temp");
        go.hideFlags = HideFlags.HideAndDontSave;
        var vp = go.AddComponent<VideoPlayer>();

        vp.source = VideoSource.Url;
        vp.url = url;
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = false;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.renderMode = VideoRenderMode.RenderTexture;

        const int texW = 512;
        const int texH = 288;
        var rt = new RenderTexture(texW, texH, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        vp.targetTexture = rt;

        bool prepared = false;
        bool seekDone = false;
        bool frameReady = false;
        double targetTime = Mathf.Max(0f, atTimeSeconds);

        VideoPlayer.EventHandler preparedHandler = null;
        VideoPlayer.EventHandler seekHandler = null;
        VideoPlayer.FrameReadyEventHandler frameHandler = null;
        VideoPlayer.ErrorEventHandler errorHandler = null;

        preparedHandler = _ => { prepared = true; };
        seekHandler = _ => { seekDone = true; };
        frameHandler = (p, frameIdx) => { frameReady = true; };
        errorHandler = (p, msg) => { Debug.LogWarning($"[ThumbVP] errorReceived: {msg} | {url}"); };

        // Suscribir
        vp.prepareCompleted += preparedHandler;
        vp.seekCompleted += seekHandler;
        vp.sendFrameReadyEvents = true;
        vp.frameReady += frameHandler;
        vp.errorReceived += errorHandler;

        // PREPARE
        vp.Prepare();
        float timeout = Time.time + 5f;
        while (!prepared)
        {
            if (Time.time > timeout) { Debug.LogWarning("[ThumbVP] prepare timeout"); break; }
            yield return null;
        }
        if (!vp.isPrepared)
        {
            // Limpieza
            vp.frameReady -= frameHandler;
            vp.seekCompleted -= seekHandler;
            vp.prepareCompleted -= preparedHandler;
            vp.errorReceived -= errorHandler;
            vp.sendFrameReadyEvents = false;

            vp.targetTexture = null;
            rt.Release(); UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(go);
            onDone?.Invoke(null);
            yield break;
        }

        if (vp.length > 0.01)
            targetTime = Math.Min(targetTime, Math.Max(0.0, vp.length - 0.033));
        seekDone = false;
        vp.time = targetTime;

        vp.Play();
        timeout = Time.time + 5f;
        while (!seekDone)
        {
            if (Time.time > timeout) { Debug.LogWarning("[ThumbVP] seek timeout"); break; }
            yield return null;
        }
        vp.Pause();

#if UNITY_2018_2_OR_NEWER
        if (!frameReady)
        {
            try { vp.StepForward(); } catch { /* backend sin soporte */ }
        }
#endif

        timeout = Time.time + 2f;
        while (!frameReady && Time.time < timeout)
            yield return null;

        yield return new WaitForEndOfFrame();

        if (!frameReady)
        {
            vp.Play();
            yield return new WaitForSeconds(0.05f);
            vp.Pause();
            yield return new WaitForEndOfFrame();
        }

        // Capturar del RT
        Sprite sprite = null;
        try
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);



            var px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++) px[i].a = 255;   // opaco
            tex.SetPixels32(px);
            tex.Apply(false, false);

            // si usas tu corrección gamma, mantén alfa=1 allí también:
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                var p = tex.GetPixels32();
                for (int i = 0; i < p.Length; i++)
                {
                    float r = Mathf.Pow(p[i].r / 255f, 1f / 2.2f);
                    float g = Mathf.Pow(p[i].g / 255f, 1f / 2.2f);
                    float b = Mathf.Pow(p[i].b / 255f, 1f / 2.2f);
                    p[i] = new Color(r, g, b, 1f); // alfa forzado
                }
                tex.SetPixels32(p);
                tex.Apply(false, false);
            }

            RenderTexture.active = prev;

            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ThumbVP] ReadPixels fail: " + ex.Message);
        }

        // Desuscribir y limpiar
        vp.frameReady -= frameHandler;
        vp.seekCompleted -= seekHandler;
        vp.prepareCompleted -= preparedHandler;
        vp.errorReceived -= errorHandler;
        vp.sendFrameReadyEvents = false;

        vp.targetTexture = null;
        if (vp.isPlaying) vp.Stop();
        rt.Release(); UnityEngine.Object.Destroy(rt);
        UnityEngine.Object.Destroy(go);

        onDone?.Invoke(sprite);
    }

    public void UI_NextVideo()
    {
        StartCoroutine(UI_NextVideo_Coroutine());
    }

    private IEnumerator UI_NextVideo_Coroutine()
    {
        int target = _currentAbsIndex + 1;
        yield return StartCoroutine(EnsureUrlCached(target));

        if (_urlCache.TryGetValue(target, out string url))
        {
            _currentAbsIndex = target;
            CargarYReproducir(url);
        }
        else
        {
            yield return StartCoroutine(EnsureUrlCached(1));
            if (_urlCache.TryGetValue(1, out string url1))
            {
                _currentAbsIndex = 1;
                CargarYReproducir(url1);
            }
            else
            {
                yield break;
            }
        }

        yield return StartCoroutine(ActualizarPreviews());
        MostrarOverlayTemporal(botonPlayGroup);
        ReiniciarAutoOcultar();
    }

    public void UI_PrevVideo()
    {
        StartCoroutine(UI_PrevVideo_Coroutine());
    }

    private IEnumerator UI_PrevVideo_Coroutine()
    {
        if (_currentAbsIndex <= 1)
        {

            yield return StartCoroutine(EnsureUrlCached(1));
            if (_urlCache.TryGetValue(1, out string url1))
            {
                _currentAbsIndex = 1;
                CargarYReproducir(url1);
                yield return StartCoroutine(ActualizarPreviews());
                MostrarOverlayTemporal(botonPlayGroup);
                ReiniciarAutoOcultar();
            }
            yield break;
        }

        int target = _currentAbsIndex - 1;
        yield return StartCoroutine(EnsureUrlCached(target));
        if (_urlCache.TryGetValue(target, out string url))
        {
            _currentAbsIndex = target;
            CargarYReproducir(url);
            yield return StartCoroutine(ActualizarPreviews());
            MostrarOverlayTemporal(botonPlayGroup);
            ReiniciarAutoOcultar();
        }
        else
        {
            // fallback al 1
            yield return StartCoroutine(EnsureUrlCached(1));
            if (_urlCache.TryGetValue(1, out string url1))
            {
                _currentAbsIndex = 1;
                CargarYReproducir(url1);
                yield return StartCoroutine(ActualizarPreviews());
                MostrarOverlayTemporal(botonPlayGroup);
                ReiniciarAutoOcultar();
            }
        }
    }

    public void UI_PlayPreviewSlot(int slotIndex)
    {
        StartCoroutine(UI_PlayPreviewSlot_Coroutine(slotIndex));
    }

    private IEnumerator UI_PlayPreviewSlot_Coroutine(int slotIndex)
    {
        if (previewSlots == null || slotIndex < 0 || slotIndex >= previewSlots.Length) yield break;

        int targetAbs = (_slotToAbsIndex != null && slotIndex < _slotToAbsIndex.Length)
                        ? _slotToAbsIndex[slotIndex]
                        : -1;

        if (targetAbs <= 0)
        {
            int fallback = _currentAbsIndex + (slotIndex + 1);
            yield return StartCoroutine(EnsureUrlCached(fallback));
            if (_urlCache.TryGetValue(fallback, out string urlF))
            {
                _currentAbsIndex = fallback;
                CargarYReproducir(urlF);
                yield return StartCoroutine(ActualizarPreviews());
                MostrarOverlayTemporal(botonPlayGroup);
                ReiniciarAutoOcultar();
            }
            yield break;
        }

        yield return StartCoroutine(EnsureUrlCached(targetAbs));
        if (_urlCache.TryGetValue(targetAbs, out string url))
        {
            _currentAbsIndex = targetAbs;
            CargarYReproducir(url);
            yield return StartCoroutine(ActualizarPreviews());
            MostrarOverlayTemporal(botonPlayGroup);
            ReiniciarAutoOcultar();
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        ultimoInputTime = Time.time;

        bool esDoblePorClickCount = e.clickCount >= 2;
        bool esDoblePorTiempo = (Time.time - ultimoTapTime) <= ventanaDobleTap &&
                                (e.position - ultimoTapPos).sqrMagnitude < (Screen.dpi > 0 ? (Screen.dpi * 0.2f) : 80f);
        ultimoTapTime = Time.time;
        ultimoTapPos = e.position;

        if (esDoblePorClickCount || esDoblePorTiempo)
        {
            if (tapArea == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tapArea, e.position, e.pressEventCamera, out var lp);
            bool izquierda = lp.x < 0f;
            if (izquierda)
            {
                Saltar(-saltoSeg);
                MostrarOverlaySalto(botonRetro1sGroup);
            }
            else
            {
                Saltar(saltoSeg);
                MostrarOverlaySalto(botonAdel1sGroup);
            }
            ReiniciarAutoOcultar();
        }
        else
        {
            SingleTapToggle();
        }
    }

    void SingleTapToggle()
    {
        if (video == null) return;

        if (video.isPlaying)
        {
            video.Pause();
            MostrarPanel();
            MostrarOverlayTemporal(botonPauseGroup);
        }
        else
        {
            video.Play();
            OcultarPanel();
            MostrarOverlayTemporal(botonPlayGroup);
        }
        ReiniciarAutoOcultar();
    }

    public void UI_Play()
    {
        if (video == null) return;
        video.Play();
        OcultarPanel();
        MostrarOverlayTemporal(botonPlayGroup);
        ReiniciarAutoOcultar();
    }

    public void UI_Pause()
    {
        if (video == null) return;
        video.Pause();
        MostrarPanel();
        MostrarOverlayTemporal(botonPauseGroup);
        ReiniciarAutoOcultar();
    }

    public void UI_Adelantar10()
    {
        Saltar(saltoSeg);
        MostrarOverlaySalto(botonAdel1sGroup);
        ReiniciarAutoOcultar();
    }

    public void UI_Retroceder10()
    {
        Saltar(-saltoSeg);
        MostrarOverlaySalto(botonRetro1sGroup);
        ReiniciarAutoOcultar();
    }

    void Saltar(float segundos)
    {
        if (video == null || !video.isPrepared) return;
        double t = video.time + segundos;
        if (t < 0) t = 0;
        if (video.length > 0) t = Math.Min(t, Math.Max(0.0, video.length - 0.033));
        video.time = t;
    }

    void SincronizarEstadoInicial()
    {
        if (video == null)
        {
            OcultarUITodoForzado();
            return;
        }

        if (video.isPlaying || video.playOnAwake)
        {
            OcultarUITodoForzado();
        }
        else
        {
            MostrarPanel();
            MostrarOverlayTemporal(botonPauseGroup);
        }
    }

    void Update()
    {
        if (autoOcultarSeg > 0f && Time.time - ultimoInputTime > autoOcultarSeg)
        {
            if (video != null && video.isPlaying)
            {
                OcultarUI();
            }
            else
            {
                OcultarSoloOverlaysEnPausa();
            }
            ultimoInputTime = float.MaxValue;
        }
    }

    void MostrarPanel()
    {
        if (slideRutina != null) StopCoroutine(slideRutina);
        slideRutina = StartCoroutine(SlideY(panelVistos, panelVistosGroup, panelVisible, durSlide));
        FadeTo(panelVistosGroup, 1f, true);

        slideRutina = StartCoroutine(SlideY(panelAcciones, panelAccionesGroup, panelAccionesVisible, durSlide));
        FadeTo(panelAccionesGroup, 1f, true);
    }

    void OcultarPanel()
    {
        if (slideRutina != null) StopCoroutine(slideRutina);
        slideRutina = StartCoroutine(SlideY(panelVistos, panelVistosGroup, panelOculto, durSlide));
        slideRutina = StartCoroutine(SlideY(panelAcciones, panelAccionesGroup, panelAccionesOculto, durSlide));
    }

    void OcultarUI()
    {
        OcultarPanel();
        FadeTo(botonPauseGroup, 0f, false);
        FadeTo(botonPlayGroup, 0f, false);
        FadeTo(botonRetro1sGroup, 0f, false);
        FadeTo(botonAdel1sGroup, 0f, false);
    }

    void OcultarUITodoForzado()
    {
        OcultarUI();
        if (panelVistosGroup != null)
        {
            panelVistosGroup.alpha = 0f;
            panelVistosGroup.interactable = false;
            panelVistosGroup.blocksRaycasts = false;
        }

        if(panelAccionesGroup != null)
        {
            panelAccionesGroup.alpha = 0f;
            panelAccionesGroup.interactable = false;
            panelAccionesGroup.blocksRaycasts = false;
        }
    }

    void OcultarSoloOverlaysEnPausa()
    {
        FadeTo(botonPauseGroup, 0f, false);
        FadeTo(botonPlayGroup, 0f, false);
        FadeTo(botonRetro1sGroup, 0f, false);
        FadeTo(botonAdel1sGroup, 0f, false);
        if (panelVistosGroup != null)
        {
            panelVistosGroup.alpha = 1f;
            panelVistosGroup.interactable = true;
            panelVistosGroup.blocksRaycasts = true;
        }
        if (panelAccionesGroup != null)
        {
            panelAccionesGroup.alpha = 1f;
            panelAccionesGroup.interactable = true;
            panelAccionesGroup.blocksRaycasts = true;
        }
    }

    void MostrarOverlayTemporal(CanvasGroup g)
    {
        StopAllCoroutinesDeGrupo(g);
        StartCoroutine(OverlayTemporalRutina(g, autoOcultarSeg));
    }

    void MostrarOverlaySalto(CanvasGroup g)
    {
        StopAllCoroutinesDeGrupo(g);
        StartCoroutine(OverlayTemporalRutina(g, overlaySkipSeg));
    }

    IEnumerator OverlayTemporalRutina(CanvasGroup g, float visibleSeg)
    {
        SetGroupInstant(g, 0f, true);
        yield return FadeEnum(g, 1f, durFade);
        yield return new WaitForSeconds(Mathf.Max(0f, visibleSeg));
        yield return FadeEnum(g, 0f, durFade);
        SetInteract(g, false);
    }

    void ReiniciarAutoOcultar()
    {
        ultimoInputTime = Time.time;
    }

    void SetGroup(CanvasGroup g, float alpha, bool interact)
    {
        if (g == null) return;
        g.alpha = alpha;
        g.interactable = interact;
        g.blocksRaycasts = interact;
    }

    void SetGroupInstant(CanvasGroup g, float alpha, bool interact)
    {
        SetGroup(g, alpha, interact);
    }

    void SetInteract(CanvasGroup g, bool interact)
    {
        if (g == null) return;
        g.interactable = interact;
        g.blocksRaycasts = interact;
    }

    void FadeTo(CanvasGroup g, float a, bool interact)
    {
        if (g == null) return;
        StopAllCoroutinesDeGrupo(g);
        StartCoroutine(FadeEnum(g, a, durFade, interact));
    }

    IEnumerator FadeEnum(CanvasGroup g, float target, float dur, bool setInteractAtEnd = true)
    {
        if (g == null) yield break;
        if (target > 0f) SetInteract(g, true);
        float start = g.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, dur);
            g.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }
        g.alpha = target;
        if (target == 0f && setInteractAtEnd) SetInteract(g, false);
        if (target > 0f && setInteractAtEnd) SetInteract(g, true);
    }

    IEnumerator SlideY(RectTransform rt, CanvasGroup cg, float toY, float dur)
    {
        if (rt == null) yield break;
        if (cg != null && toY == panelVisible) SetInteract(cg, true);
        Vector2 start = rt.anchoredPosition;
        Vector2 end = new Vector2(start.x, toY);
        float t = 0f;
        if (cg != null && toY == panelVisible) StartCoroutine(FadeEnum(cg, 1f, durFade));
        if (cg != null && toY == panelOculto) StartCoroutine(FadeEnum(cg, 0f, durFade));
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, dur);
            rt.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rt.anchoredPosition = end;
        if (cg != null && toY == panelOculto) SetInteract(cg, false);
    }

    void StopAllCoroutinesDeGrupo(CanvasGroup g)
    {
        // reservado para cancelaciones específicas por grupo
    }

    public void OnInteract()
    {
        UIManager uiManager = FindFirstObjectByType<UIManager>();

        if (firstPerson != null && firstPerson.isInteracting)
        {
            if (uiManager != null) uiManager.hideCursor();
            if (firstPerson != null)
            {
                firstPerson.ReturnCamera();
                firstPerson.isInteracting = false;
            }
        }
        else
        {
            if (uiManager != null) uiManager.showCursor();

            if (video != null)
            {
                MostrarPanel();
                MostrarOverlayTemporal(botonPauseGroup);
                ReiniciarAutoOcultar();
            }

            if (cameraPivot != null)
            {
                if (firstPerson != null)
                {
                    firstPerson.MoveCameraToTarget(cameraPivot);
                }
            }
            else
            {
                if (firstPerson != null)
                {
                    firstPerson.isInteracting = true;
                }
            }
        }
    }
}
