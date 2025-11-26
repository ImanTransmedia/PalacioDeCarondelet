using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static string CommonImageURL = "https://palaciocarondelet360.presidencia.gob.ec/MediaResources/Images/";
    public static string CommonVideoURL = "https://palaciocarondelet360.presidencia.gob.ec/MediaResources/Videos/";
    public static string ConfigurationURL = "https://historiasmagneticas.com/Carondelet/configInteractuables.json";
    public static string CommonVideoListURL = "https://historiasmagneticas.com/Carondelet/MediaResources/Videos/SalonAzul/";

    private const bool ALWAYS_REFRESH = true;
    private const bool CACHE_BUST_WITH_TIMESTAMP = true;

    public GeneralConfig Config { get; private set; }
    public event Action OnConfigReady;

    private string CachePath => Path.Combine(Application.persistentDataPath, "configInteractuables.json");
    private bool _isLoading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        StartCoroutine(LoadConfigCoroutine());
    }

    private IEnumerator LoadConfigCoroutine()
    {
        yield return StartCoroutine(LoadOrDownloadAndParse());
        if (Config != null) OnConfigReady?.Invoke();
    }

    // ------------- Público: recargar bajo demanda -------------
    public IEnumerator ReloadConfigCoroutine(Action<bool> onDone = null)
    {
        if (_isLoading)
        {
            // Espera hasta que termine la carga en curso
            while (_isLoading) yield return null;
            onDone?.Invoke(Config != null);
            yield break;
        }

        yield return StartCoroutine(DownloadParseAndCache()); // fuerza descarga
        onDone?.Invoke(Config != null);
        if (Config != null) OnConfigReady?.Invoke();
    }

    // ------------- Core -------------
    private IEnumerator LoadOrDownloadAndParse()
    {
        _isLoading = true;
        string json = null;

        if (ALWAYS_REFRESH)
        {
            bool downloaded = false;
            yield return StartCoroutine(DownloadConfig(result =>
            {
                json = StripBOM(result);
                SaveToCache(json);
                downloaded = true;
                Debug.Log("[GameManager] config descargada");
            },
            err =>
            {
                Debug.LogError($"[GameManager] no se pudo descargar: {err}, intentando cache");
            }));

            if (!downloaded && !TryLoadFromCache(out json))
            {
                Debug.LogError("[GameManager] sin descarga y sin cache");
            }
        }
        else
        {
            if (!TryLoadFromCache(out json))
            {
                yield return StartCoroutine(DownloadConfig(result =>
                {
                    json = StripBOM(result);
                    SaveToCache(json);
                    Debug.Log("[GameManager] config descargada");
                },
                err =>
                {
                    Debug.LogError($"[GameManager] no se pudo descargar: {err}");
                }));
            }
        }

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                json = StripBOM(json);
                Config = JsonUtility.FromJson<GeneralConfig>(json);
                Debug.Log($"[GameManager] config ok, salones: {Config?.salones?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameManager] error parseando JSON: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("[GameManager] config vacía");
        }

        _isLoading = false;
    }

    private IEnumerator DownloadParseAndCache()
    {
        _isLoading = true;
        string json = null;

        yield return StartCoroutine(DownloadConfig(result =>
        {
            json = StripBOM(result);
            SaveToCache(json);
            Debug.Log("[GameManager] recarga descargada");
        },
        err =>
        {
            Debug.LogError($"[GameManager] recarga fallida: {err}");
        }));

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                Config = JsonUtility.FromJson<GeneralConfig>(json);
                Debug.Log($"[GameManager] recarga ok, salones: {Config?.salones?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameManager] error parseando recarga: {ex.Message}");
            }
        }

        _isLoading = false;
    }

    private bool TryLoadFromCache(out string json)
    {
        json = null;
        try
        {
            if (File.Exists(CachePath))
            {
                json = StripBOM(File.ReadAllText(CachePath));
                return !string.IsNullOrWhiteSpace(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameManager] error leyendo cache: {e.Message}");
        }
        return false;
    }

    private void SaveToCache(string json)
    {
        try
        {
            json = StripBOM(json);
            var utf8NoBom = new System.Text.UTF8Encoding(false);
            File.WriteAllText(CachePath, json, utf8NoBom);
            Debug.Log($"[GameManager] cache guardada: {CachePath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameManager] no se pudo guardar cache: {e.Message}");
        }
    }

    private IEnumerator DownloadConfig(Action<string> onSuccess, Action<string> onError)
    {
        string url = ConfigurationURL;
        if (CACHE_BUST_WITH_TIMESTAMP)
        {
            string ts = DateTime.UtcNow.Ticks.ToString();
            url += (url.Contains("?") ? "&" : "?") + "_ts=" + ts;
        }

        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            req.SetRequestHeader("Pragma", "no-cache");
            req.SetRequestHeader("Expires", "0");

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result == UnityWebRequest.Result.Success)
#else
            if (!req.isNetworkError && !req.isHttpError)
#endif
            {
                onSuccess?.Invoke(req.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }

    // ---------------- Utilidades para otros scripts ----------------
    public List<string> GetSalonNames()
    {
        var list = new List<string>();
        if (Config?.salones != null)
            foreach (var s in Config.salones) list.Add(s.salon);
        return list;
    }
    public static string MakeSAVideoBaseName(int index /*1..N*/)
    {
        return $"SA_Vid_{index:000}";
    }

    public IEnumerator ResolveSalonAzulVideoUrl(
        int index,
        Action<string> onSuccess,
        Action<string> onError = null)
    {
        var baseName = MakeSAVideoBaseName(index);
        yield return StartCoroutine(ResolveVideoUrl(baseName, onSuccess, onError,true));
    }


    public List<string> GetObjetoNames(string salon)
    {
        var list = new List<string>();
        var s = Config?.salones?.Find(x => string.Equals(x.salon, salon, StringComparison.OrdinalIgnoreCase));
        if (s?.objetos != null)
            foreach (var o in s.objetos) list.Add(o.identifier);
        return list;
    }

    public bool TryGetObjeto(string salon, string identifier, out InteractableEntry obj)
    {
        obj = null;
        var s = Config?.salones?.Find(x => string.Equals(x.salon, salon, StringComparison.OrdinalIgnoreCase));
        if (s == null) return false;
        obj = s.objetos?.Find(o => string.Equals(o.identifier, identifier, StringComparison.OrdinalIgnoreCase));
        return obj != null;
    }

    public bool TryGetObjetoByName(string objectName, out InteractableEntry obj)
    {
        obj = null;

        if (Config?.salones == null)
        {
            Debug.LogError("[GameManager] config o salones nulos");
            return false;
        }
        foreach (var salon in Config.salones)
        {
            Debug.Log($"[GameManager] buscando '{objectName}' en '{salon.salon}'");
            var found = salon.objetos?.Find(o =>
                string.Equals(o.identifier, objectName, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                obj = found;
                return true;
            }
        }
        return false;
    }

    public bool TryGetObjetoByName(string objectName, out InteractableEntry obj, out string salonName)
    {
        obj = null;
        salonName = null;
        if (Config?.salones == null) return false;

        foreach (var salon in Config.salones)
        {
            var found = salon.objetos?.Find(o =>
                string.Equals(o.identifier, objectName, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                obj = found;
                salonName = salon.salon;
                return true;
            }
        }
        return false;
    }

    public bool TryGetObjetoByNameInSalon(string salon, string objectName, out InteractableEntry obj)
    {
        obj = null;
        var s = Config?.salones?.Find(x =>
            string.Equals(x.salon, salon, StringComparison.OrdinalIgnoreCase));
        if (s == null) return false;

        obj = s.objetos?.Find(o =>
            string.Equals(o.identifier, objectName, StringComparison.OrdinalIgnoreCase));
        return obj != null;
    }

    public List<(string salon, InteractableEntry obj)> GetObjetosByNameAll(string objectName)
    {
        var result = new List<(string salon, InteractableEntry obj)>();
        if (Config?.salones == null) return result;

        foreach (var s in Config.salones)
        {
            var matches = s.objetos?.FindAll(o =>
                string.Equals(o.identifier, objectName, StringComparison.OrdinalIgnoreCase));
            if (matches != null)
                foreach (var m in matches)
                    result.Add((s.salon, m));
        }
        return result;
    }

    public LocalizedString MakeLoc(string table, string entry)
    {
        var ls = new LocalizedString();
        if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(entry))
        {
            Debug.LogWarning($"[MakeLoc] tabla/clave vacías: '{table}' / '{entry}'");
            return ls;
        }

        ls.TableReference = table;

        if (long.TryParse(entry, out var id))
            ls.TableEntryReference = id;
        else
            ls.TableEntryReference = entry;

        Debug.Log($"[MakeLoc] ok: '{table}' / '{entry}'");
        return ls;
    }

    public IEnumerator DownloadImageSprite(URLSalon salon, string imageBaseName, Action<Sprite> onSuccess, Action<string> onError = null)
    {
        string baseName = System.IO.Path.GetFileNameWithoutExtension(imageBaseName);
        string[] exts = { ".jpg", ".png" };

        foreach (var ext in exts)
        {
            string url = $"{CommonImageURL}{salon}/{baseName}{ext}";
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (req.result == UnityWebRequest.Result.Success)
#else
                if (!req.isNetworkError && !req.isHttpError)
#endif
                {
                    var tex = DownloadHandlerTexture.GetContent(req);
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    onSuccess?.Invoke(sprite);
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"img fail: {url} -> {req.error}");
                }
            }
        }

        onError?.Invoke($"sin imagen '{baseName}' en {salon}");
    }

    public IEnumerator ResolveVideoUrl(string videoBaseName, Action<string> onSuccess, Action<string> onError = null, bool isVideoSA = false)
    {
        string baseName = System.IO.Path.GetFileNameWithoutExtension(videoBaseName);
        string[] exts = { ".mp4" , ".webm" };

        foreach (var ext in exts)
        {
            string url =  isVideoSA? $"{CommonVideoListURL}{baseName}{ext}": $"{CommonVideoURL}{baseName}{ext}";
            using (UnityWebRequest head = UnityWebRequest.Head(url))
            {
                yield return head.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                bool ok = head.result == UnityWebRequest.Result.Success || (head.responseCode >= 200 && head.responseCode < 300);
#else
                bool ok = !head.isNetworkError && !head.isHttpError && (head.responseCode >= 200 && head.responseCode < 300);
#endif
                if (ok)
                {
                    onSuccess?.Invoke(url);
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"video HEAD fail: {url} -> {head.error} ({head.responseCode})");
                }
            }
        }

        onError?.Invoke($"sin video '{baseName}' .mp4");
    }

    private static string StripBOM(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.TrimStart('\uFEFF', '\u200B', '\u200E', '\u200F');
    }
}

public enum URLSalon
{
    SalonAmarillo,
    SalonBanquetes,
    SalonGabinete,
    DespachoPresidencial,
    Petril,
    PatioInterior,
    Museo,
    Balcon
}
