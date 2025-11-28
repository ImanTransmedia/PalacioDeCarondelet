using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-1000)]
public class AudioManagerBDC : MonoBehaviour
{
    // ===== Singleton =====
    public static AudioManagerBDC I { get; private set; }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        BuildDictionaries();
        CreatePools();
        CreateMusicSources();
        CreateExclusiveSfxSource();
    }

    // ===== Mixer & Grupos =====
    [Header("Mixer")]
    public AudioMixer mixer;                       
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Exposed Params (en AudioMixer)")]
    public string masterVolParam = "MasterVol";    
    public string musicVolParam = "MusicVol";     
    public string sfxVolParam = "SFXVol";      

    // ===== Clips nombrados =====
    [Serializable]
    public class NamedClip
    {
        public string id;
        public AudioClip clip;
    }

    [Header("Clips")]
    public NamedClip[] musicClips;
    public NamedClip[] sfxClips;

    Dictionary<string, AudioClip> _music = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
    Dictionary<string, AudioClip> _sfx = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

    void BuildDictionaries()
    {
        _music.Clear(); _sfx.Clear();
        foreach (var nc in musicClips) if (nc?.clip) _music[nc.id] = nc.clip;
        foreach (var nc in sfxClips) if (nc?.clip) _sfx[nc.id] = nc.clip;
    }

    [Header("Música")]
    [Range(0f, 1f)] public float defaultMusicFade = 0.75f;
    [Range(0f, 1f)] public float musicSpatialBlend = 0f; 

    private AudioSource _musicA, _musicB;
    private AudioSource _activeMusic, _idleMusic;
    private Coroutine _musicFadeCo;

    void CreateMusicSources()
    {
        _musicA = gameObject.AddComponent<AudioSource>();
        _musicB = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { _musicA, _musicB })
        {
            src.playOnAwake = false;
            src.loop = true;
            src.outputAudioMixerGroup = musicGroup;
            src.spatialBlend = musicSpatialBlend;
        }
        _activeMusic = _musicA;
        _idleMusic = _musicB;
    }


    private void OnDisable()
    {
        StopMusic(0f);
    }

    public void PlayMusic(string id, float fadeSeconds = -1f, bool loop = true, float pitch = 1f)
    {
        if (!_music.TryGetValue(id, out var clip) || clip == null)
        {
            Debug.LogWarning($"[AudioManager] Music id '{id}' no encontrado.");
            return;
        }

        if (fadeSeconds < 0f) fadeSeconds = defaultMusicFade;

        if (_activeMusic.clip == clip && _activeMusic.isPlaying) return;

        _idleMusic.clip = clip;
        _idleMusic.loop = loop;
        _idleMusic.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        _idleMusic.volume = 0f;
        _idleMusic.Play();

        if (_musicFadeCo != null) StopCoroutine(_musicFadeCo);
        _musicFadeCo = StartCoroutine(CrossfadeMusicCo(fadeSeconds));
    }

    public void StopMusic(float fadeSeconds = -1f)
    {
        if (fadeSeconds < 0f) fadeSeconds = defaultMusicFade;
        if (_musicFadeCo != null) StopCoroutine(_musicFadeCo);
        _musicFadeCo = StartCoroutine(FadeOutMusicCo(fadeSeconds));
    }

    System.Collections.IEnumerator CrossfadeMusicCo(float t)
    {
        float aStart = _activeMusic ? _activeMusic.volume : 1f;
        float time = 0f;

        while (time < t)
        {
            time += Time.unscaledDeltaTime;
            float k = (t <= 0f) ? 1f : Mathf.Clamp01(time / t);
            if (_activeMusic) _activeMusic.volume = Mathf.Lerp(aStart, 0f, k);
            if (_idleMusic) _idleMusic.volume = Mathf.Lerp(0f, 1f, k);
            yield return null;
        }

        if (_activeMusic) { _activeMusic.Stop(); _activeMusic.volume = 1f; }

        // swap
        var temp = _activeMusic;
        _activeMusic = _idleMusic;
        _idleMusic = temp;

        _musicFadeCo = null;
    }

    System.Collections.IEnumerator FadeOutMusicCo(float t)
    {
        float start = _activeMusic ? _activeMusic.volume : 1f;
        float time = 0f;

        while (time < t)
        {
            time += Time.unscaledDeltaTime;
            float k = (t <= 0f) ? 1f : Mathf.Clamp01(time / t);
            if (_activeMusic) _activeMusic.volume = Mathf.Lerp(start, 0f, k);
            yield return null;
        }

        if (_activeMusic) { _activeMusic.Stop(); _activeMusic.volume = 1f; }
        _musicFadeCo = null;
    }

    // ===== SFX (uno exclusivo o varios concurrentes) =====
    [Header("SFX Pool")]
    public int initialSfxPool = 10;
    public int maxSfxPool = 32;
    [Range(0f, 1f)] public float sfxSpatialBlend = 0f; // 0 = 2D, 1 = 3D

    private readonly Queue<AudioSource> _sfxFree = new Queue<AudioSource>();
    private readonly List<AudioSource> _sfxBusy = new List<AudioSource>();
    private AudioSource _exclusiveSfxSource;

    void CreatePools()
    {
        for (int i = 0; i < Mathf.Max(1, initialSfxPool); i++)
            _sfxFree.Enqueue(CreateSfxSource($"SFX_{i:00}"));
    }

    void CreateExclusiveSfxSource()
    {
        _exclusiveSfxSource = CreateSfxSource("SFX_Exclusive");
    }

    AudioSource CreateSfxSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.outputAudioMixerGroup = sfxGroup;
        src.spatialBlend = sfxSpatialBlend;
        return src;
    }

    void Update()
    {
        for (int i = _sfxBusy.Count - 1; i >= 0; i--)
        {
            var src = _sfxBusy[i];
            if (!src.isPlaying)
            {
                _sfxBusy.RemoveAt(i);
                if (_sfxFree.Count + _sfxBusy.Count < maxSfxPool)
                {
                    _sfxFree.Enqueue(src);
                }
                else
                {
                    Destroy(src.gameObject);
                }
            }
        }
    }

    public void PlaySFX(string id, bool exclusive = false, float volume = 1f, float pitch = 1f)
    {
        if (!_sfx.TryGetValue(id, out var clip) || clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX id '{id}' no encontrado.");
            return;
        }

        if (exclusive)
        {
            _exclusiveSfxSource.Stop();
            _exclusiveSfxSource.clip = clip;
            _exclusiveSfxSource.volume = Mathf.Clamp01(volume);
            _exclusiveSfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            _exclusiveSfxSource.Play();
            return;
        }

        var src = GetFreeSfxSource();
        if (src == null) { Debug.LogWarning("[AudioManager] Pool SFX lleno."); return; }

        src.transform.position = Vector3.zero;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        src.spatialBlend = sfxSpatialBlend;
        src.Play();
        _sfxBusy.Add(src);
    }

    public void PlaySFXAt(string id, Vector3 worldPos, bool exclusive = false, float volume = 1f, float pitch = 1f)
    {
        if (exclusive) { PlaySFX(id, true, volume, pitch); return; }

        if (!_sfx.TryGetValue(id, out var clip) || clip == null)
        {
            Debug.LogWarning($"[AudioManager] SFX id '{id}' no encontrado.");
            return;
        }

        var src = GetFreeSfxSource();
        if (src == null) { Debug.LogWarning("[AudioManager] Pool SFX lleno."); return; }

        src.transform.position = worldPos;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        src.spatialBlend = sfxSpatialBlend;
        src.Play();
        _sfxBusy.Add(src);
    }

    AudioSource GetFreeSfxSource()
    {
        if (_sfxFree.Count > 0) return _sfxFree.Dequeue();
        if (_sfxBusy.Count + _sfxFree.Count < maxSfxPool) return CreateSfxSource($"SFX_{_sfxBusy.Count + _sfxFree.Count:00}");
        return null;
    }

    // ===== PLAYLIST =====
    [Header("Playlist")]
    public bool loopPlaylist = true;      
    public float crossfadeBetweenTracks = 1.0f;

    private Coroutine _playlistCo;
    private List<string> _playlist;
    private int _currentTrackIndex = 0;

    public void StartPlaylist(List<string> ids, bool loop = true, float fadeSeconds = -1f)
    {
        if (ids == null || ids.Count == 0)
        {
            Debug.LogWarning("[AudioManager] Playlist vacía o nula.");
            return;
        }

        StopPlaylist(); // detener si ya hay una corriendo

        loopPlaylist = loop;
        crossfadeBetweenTracks = (fadeSeconds > 0) ? fadeSeconds : defaultMusicFade;

        _playlist = new List<string>(ids);
        _currentTrackIndex = 0;

        _playlistCo = StartCoroutine(PlaylistCoroutine());
    }


    public void StopPlaylist()
    {
        if (_playlistCo != null)
        {
            StopCoroutine(_playlistCo);
            _playlistCo = null;
        }
    }

    public void PlaySFXBetweenScenes(string id, float volume = 1f, float pitch = 1f)
    {
        PlaySFX(id, exclusive: true, volume: volume, pitch: pitch);
    }

    private IEnumerator PlaylistCoroutine()
    {
        if (_playlist == null || _playlist.Count == 0) yield break;

        while (true)
        {
            string id = _playlist[_currentTrackIndex];

            PlayMusic(id, crossfadeBetweenTracks, true);

            if (_music.TryGetValue(id, out var clip) && clip != null)
            {
                float waitTime = Mathf.Max(0, clip.length - crossfadeBetweenTracks);
                yield return new WaitForSecondsRealtime(waitTime);
            }
            else
            {
                yield return new WaitForSecondsRealtime(1f);
            }

            _currentTrackIndex++;

            if (_currentTrackIndex >= _playlist.Count)
            {
                if (loopPlaylist)
                    _currentTrackIndex = 0;
                else
                    break;
            }
        }

        _playlistCo = null;
    }



    public void SetMasterVolume(float linear01) => SetDb(masterVolParam, LinearToDb(linear01));
    public void SetMusicVolume(float linear01) => SetDb(musicVolParam, LinearToDb(linear01));
    public void SetSfxVolume(float linear01) => SetDb(sfxVolParam, LinearToDb(linear01));

    void SetDb(string param, float dB)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;
        mixer.SetFloat(param, dB);
    }

    public static float LinearToDb(float linear01)
    {
        linear01 = Mathf.Clamp(linear01, 0.0001f, 1f);
        return 20f * Mathf.Log10(linear01); 
    }

    public bool HasMusic(string id) => _music.ContainsKey(id);
    public bool HasSFX(string id) => _sfx.ContainsKey(id);
}

