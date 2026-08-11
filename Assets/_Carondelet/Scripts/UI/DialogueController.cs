using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;
using UnityEngine.Video;
using System.Text.RegularExpressions;   

public class DialogueController : MonoBehaviour
{
    private Queue<int> dialogueQueue = new Queue<int>();
    private bool isDialoguePlaying = false;

    public delegate void DialogueFinishedEventHandler();
    public event DialogueFinishedEventHandler OnDialogueFinished;

    public TextMeshProUGUI textComponent;
    public GameObject dialoguePanel;
    private CanvasGroup dialoguePanelCanvasGroup;
    public float minTextSpeed = 0.005f;
    public float maxTextSpeed = 0.12f;
    private float textSpeed = 0.0f;
    [SerializeField] private float waitBetweenSegments = 0.45f;
    private int linesPerSegment = 1;

    public List<LocalizedDialogue> dialogueList;

    [Header("Mobile text wrap (only mobile/WebGL mobile)")]
    public bool mobileWrapEnabled = true;
    public int mobileMaxWordsPerLine = 8;
    public int mobileMaxCharsPerLine = 34;

    [Header("Multimedia Settings")]
    public VideoPlayer videoPlayer;
    // public AudioSource audioSource;
    public AudioSource videoAudioOutput;

    private List<string> currentDialogueLines;
    private int currentLineIndex;
    private bool isDialogueActive = false;

    public bool startDialogueAutomatically = false;
    public int dialogueIndexToShow = 0;
    private string sessionKey;
    public bool autoAdjustTextSpeed = true;

    [Header("Subtitle synchronization patch")]
    [Min(0f)] public float subtitleStartDelay = 0.9f;
    [Min(0f)] public float subtitleEndDelay = 0.9f;
    [Min(0f)] public float subtitleEndReadingTime = 1.5f;
    [Min(1f)] public float videoPrepareTimeout = 15f;
    [Min(1f)] public float videoPlaybackGraceTime = 5f;

    private bool videoPreparedSuccessfully;
    private bool videoPlaybackError;

    void Start()
    {
        StopAllCoroutines();        
        isDialoguePlaying = false;
        isDialogueActive = false;
        dialogueQueue.Clear();
        currentDialogueLines = null;
        currentLineIndex = 0;

        textComponent.text = string.Empty;

        dialoguePanelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        dialoguePanel.SetActive(false);
        dialoguePanelCanvasGroup.alpha = 0f;

        if (videoPlayer != null)
            videoPlayer.errorReceived += OnVideoError;

        GameObject audioGO = GameObject.Find("AudioSourceVideo");
        if (audioGO != null)
            videoAudioOutput = audioGO.GetComponent<AudioSource>();

        ConfigureVideoAudio();

        if (startDialogueAutomatically)
        {
            sessionKey = "DialogueShown_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (!DoorManager.Instance.ContainsString(sessionKey))
            {
                StartCoroutine(WaitForSceneThenShowDialogue());
                DoorManager.Instance.StoreString(sessionKey);
            }
        }
    }


    IEnumerator WaitForSceneThenShowDialogue()
    {
        yield return new WaitUntil(() => LoadingScreen.IsSceneReady);
        yield return new WaitForSeconds(3f);
        Debug.Log("Subescenas listas y iniciando Baron");
        ShowDialogue(dialogueIndexToShow);
    }

    public void ShowDialogue(int index)
    {
        if (isDialoguePlaying)
        {
            Debug.Log($"[DialogueController] Diálogo en reproducción. Agregando a la cola: {index}");
            dialogueQueue.Enqueue(index);
            return;
        }
        Debug.Log($"[DialogueController] Mostrando diálogo inmediatamente: {index}");
        StartCoroutine(StartDialogueWithQueue(index));
    }

    private IEnumerator StartDialogueWithQueue(int index)
    {
        Debug.Log($"[DialogueController] Iniciando diálogo: {index}");
        isDialoguePlaying = true;
        ShowDialogueInternal(index);
        yield return new WaitUntil(() => !isDialogueActive);
        isDialoguePlaying = false;

        if (dialogueQueue.Count > 0)
        {
            int nextIndex = dialogueQueue.Dequeue();
            ShowDialogue(nextIndex);
        }
    }

    private void ShowDialogueInternal(int index)
    {
        if (index < 0 || index >= dialogueList.Count) return;

        StopAllCoroutines();
        textComponent.text = string.Empty;
        StopMediaPlayback();

        LocalizedDialogue dialogue = dialogueList[index];
        LocalizedString localizedString = dialogue.localizedString;
        string localizedText = localizedString.GetLocalizedString();
        localizedText = Regex.Replace(localizedText, @"[ \t]+", " ").Trim();
        textSpeed = dialogue.finalSpeed > 0f
            ? Mathf.Max(minTextSpeed, dialogue.finalSpeed * 1.3f)
            : 0.04f;
        currentDialogueLines = SplitTextIntoLines(localizedText);

        if (mobileWrapEnabled && IsMobileRuntime())
        {
            currentDialogueLines = WrapLinesForMobile(currentDialogueLines, mobileMaxWordsPerLine, mobileMaxCharsPerLine);
        }

        currentLineIndex = 0;

        StartCoroutine(PrepareAndStartDialogue(index));
    }

    private bool IsMobileRuntime()
    {
#if UNITY_WEBGL
        return Application.isMobilePlatform;
#else
    return Application.isMobilePlatform;
#endif
    }

    private List<string> WrapLinesForMobile(List<string> inputLines, int maxWordsPerLine, int maxCharsPerLine)
    {
        List<string> output = new List<string>();

        foreach (var line in inputLines)
        {
            // Divide por espacios múltiples
            var words = Regex.Split(line.Trim(), @"\s+");
            if (words.Length == 0) continue;

            int wordCountInLine = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (string.IsNullOrEmpty(w)) continue;

                bool isFirstWord = sb.Length == 0;

                int newLen = sb.Length + (isFirstWord ? 0 : 1) + w.Length;

                bool exceedWords = (maxWordsPerLine > 0) && (wordCountInLine >= maxWordsPerLine);
                bool exceedChars = (maxCharsPerLine > 0) && (!isFirstWord) && (newLen > maxCharsPerLine);

                if (exceedWords || exceedChars)
                {
                    output.Add(sb.ToString());
                    sb.Clear();
                    wordCountInLine = 0;
                    isFirstWord = true;
                }

                if (!isFirstWord) sb.Append(" ");
                sb.Append(w);
                wordCountInLine++;

            }

            if (sb.Length > 0)
                output.Add(sb.ToString());
        }

        return output;
    }


    private IEnumerator PrepareAndStartDialogue(int index)
    {
        isDialogueActive = true;
        videoPreparedSuccessfully = false;
        videoPlaybackError = false;
        dialoguePanel.SetActive(true);

        if (dialoguePanelCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(dialoguePanelCanvasGroup, 0f, 1f, 0.4f));

        LocalizedDialogue dialogue = dialogueList[index];

        if (dialogue.localizedVideo != null /*&& dialogue.localizedAudio != null*/)
        {
            string videoPath = dialogue.localizedVideo.GetLocalizedString();
            // string audioPath = dialogue.localizedAudio.GetLocalizedString();

            if (!string.IsNullOrEmpty(videoPath) /*&& !string.IsNullOrEmpty(audioPath)*/)
            {
                // yield return StartCoroutine(LoadAndPlayMultimedia(videoPath, audioPath));
                yield return StartCoroutine(LoadAndPlayVideo(videoPath));
                if (videoPreparedSuccessfully)
                {
                    videoPlayer.time = 0;
                    videoPlayer.Play();

                    float playDeadline = Time.realtimeSinceStartup + videoPlaybackGraceTime;
                    while (!videoPlayer.isPlaying && !videoPlaybackError && Time.realtimeSinceStartup < playDeadline)
                        yield return null;

                    if (videoPlayer.isPlaying)
                    {
                        UpdateTextSpeedBasedOnVideo();

                        if (dialoguePanelCanvasGroup != null)
                            dialoguePanelCanvasGroup.alpha = 1f;

                        if (subtitleStartDelay > 0f)
                            yield return new WaitForSecondsRealtime(subtitleStartDelay);
                    }
                    else
                    {
                        Debug.LogWarning("[DialogueController] El video no inici\u00f3; se mostrar\u00e1 el texto con la velocidad de respaldo.");
                    }
                }
            }
        }

        yield return StartCoroutine(DisplaySegments());
    }

    IEnumerator DisplaySegments()
    {
        while (currentLineIndex < currentDialogueLines.Count)
        {
            textComponent.text = "";
            int linesThisSegment = Mathf.Min(linesPerSegment, currentDialogueLines.Count - currentLineIndex);

            for (int i = 0; i < linesThisSegment; i++)
            {
                yield return StartCoroutine(TypeLine(currentDialogueLines[currentLineIndex]));
                textComponent.text += "\n";
                currentLineIndex++;
            }

            if (currentLineIndex < currentDialogueLines.Count && waitBetweenSegments > 0f)
                yield return new WaitForSecondsRealtime(waitBetweenSegments);
        }

        yield return StartCoroutine(WaitForVideoCompletion());

        if (subtitleEndReadingTime > 0f)
            yield return new WaitForSecondsRealtime(subtitleEndReadingTime);

        if (dialoguePanelCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(dialoguePanelCanvasGroup, 1f, 0f, 0.65f));

        EndDialogue();
    }

    IEnumerator TypeLine(string line)
    {
        float startTime = Time.unscaledTime;
        float timePerChar = textSpeed;
        int charIndex = 0;
        textComponent.text = "";

        while (charIndex < line.Length)
        {
            float elapsedTime = Time.unscaledTime - startTime;
            int charsToShow = Mathf.FloorToInt(elapsedTime / timePerChar);

            while (charIndex < charsToShow && charIndex < line.Length)
            {
                textComponent.text += line[charIndex];
                charIndex++;
            }

            yield return null;
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        isDialoguePlaying = false;
        OnDialogueFinished?.Invoke();

        if (dialogueQueue.Count > 0)
        {
            int nextIndex = dialogueQueue.Dequeue();
            Debug.Log($"[DialogueController] Reproduciendo diálogo en cola: {nextIndex}");
            ShowDialogue(nextIndex);
        }
    }

    private List<string> SplitTextIntoLines(string text)
    {
        List<string> segments = new List<string>();
        MatchCollection matches = Regex.Matches(text, @"[^.?!;]+[.?!;]?");

        foreach (Match match in matches)
        {
            string trimmedLine = match.Value.Trim();
            if (!string.IsNullOrEmpty(trimmedLine))
                segments.Add(trimmedLine);
        }

        return segments;
    }

    private void UpdateTextSpeedBasedOnVideo()
    {
        if (!autoAdjustTextSpeed || videoPlayer == null || videoPlayer.length <= 0d ||
            currentDialogueLines == null || currentDialogueLines.Count == 0)
            return;

        int totalCharacters = 0;
        foreach (string line in currentDialogueLines)
            totalCharacters += line.Length;

        if (totalCharacters <= 0)
            return;

        int segmentCount = Mathf.CeilToInt((float)currentDialogueLines.Count / Mathf.Max(1, linesPerSegment));
        float pauseDuration = Mathf.Max(0, segmentCount - 1) * waitBetweenSegments;
        // Reserva el mismo margen para las animaciones de entrada y salida.
        // El texto empieza después del primer margen y termina antes del último.
        float animationMargins = subtitleStartDelay + subtitleEndDelay;
        float typingDuration = Mathf.Max(0.1f, (float)videoPlayer.length - animationMargins - pauseDuration);

        textSpeed = Mathf.Clamp(typingDuration / totalCharacters, minTextSpeed, maxTextSpeed);
        Debug.Log($"[DialogueController] Subt\u00edtulos ajustados a {videoPlayer.length:F2}s; velocidad: {textSpeed:F4}s por car\u00e1cter.");
    }

    private IEnumerator WaitForVideoCompletion()
    {
        if (!videoPreparedSuccessfully || videoPlayer == null || videoPlayer.length <= 0d)
            yield break;

        float remaining = Mathf.Max(0f, (float)(videoPlayer.length - videoPlayer.time));
        float deadline = Time.realtimeSinceStartup + remaining + videoPlaybackGraceTime;

        while (!videoPlaybackError && videoPlayer.time < videoPlayer.length - 0.1d &&
               Time.realtimeSinceStartup < deadline)
        {
            // Recupera pausas inesperadas del VideoPlayer sin bloquear indefinidamente el di\u00e1logo.
            if (!videoPlayer.isPlaying)
                videoPlayer.Play();

            yield return null;
        }
    }

    /*
    private IEnumerator LoadAndPlayMultimedia(string videoPath, string audioPath)
    {
        yield return StartCoroutine(LoadAndPlayVideo(videoPath));
        yield return StartCoroutine(LoadAndPlayAudio(audioPath));

        videoPlayer.time = 0;
        audioSource.time = 0;
        videoPlayer.Play();
        audioSource.Play();
    }
    */

    private IEnumerator LoadAndPlayVideo(string relativePath)
    {
        videoPreparedSuccessfully = false;
        videoPlaybackError = false;

        if (videoPlayer == null)
            yield break;

        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");
        ConfigureVideoAudio();
        videoPlayer.url = fullPath;
        videoPlayer.Prepare();

        float deadline = Time.realtimeSinceStartup + videoPrepareTimeout;
        while (!videoPlayer.isPrepared && !videoPlaybackError && Time.realtimeSinceStartup < deadline)
            yield return null;

        videoPreparedSuccessfully = videoPlayer.isPrepared && !videoPlaybackError;
        if (videoPreparedSuccessfully)
            ConfigureVideoAudio();

        if (!videoPreparedSuccessfully)
            Debug.LogError($"[DialogueController] No se pudo preparar el video dentro de {videoPrepareTimeout:F1}s: {fullPath}");
    }

    private void ConfigureVideoAudio()
    {
        if (videoPlayer == null)
            return;

        if (videoAudioOutput == null)
        {
            GameObject audioGO = GameObject.Find("AudioSourceVideo");
            if (audioGO != null)
                videoAudioOutput = audioGO.GetComponent<AudioSource>();
        }

        if (videoAudioOutput == null)
        {
            Debug.LogError("[DialogueController] No se encontr\u00f3 el AudioSourceVideo para reproducir el audio del Bar\u00f3n.");
            return;
        }

        if (!videoPlayer.isPrepared)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
        }

        videoPlayer.SetTargetAudioSource(0, videoAudioOutput);
    }

    /*
    private IEnumerator LoadAndPlayAudio(string relativePath)
    {
        if (audioSource == null) yield break;

        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");

#if UNITY_WEBGL
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.MPEG))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load audio: " + request.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip != null)
            {
                while (clip.loadState != AudioDataLoadState.Loaded)
                    yield return null;
                audioSource.clip = clip;
            }
        }
#else
        string url = "file://" + fullPath;
        using (WWW www = new WWW(url))
        {
            yield return www;
            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError("Failed to load audio (Editor/PC): " + www.error);
                yield break;
            }

            AudioClip clip = www.GetAudioClip(false, true, AudioType.MPEG);
            if (clip != null)
            {
                audioSource.clip = clip;
                UpdateTextSpeedBasedOnAudio(clip);
            }
        }
#endif
    }

    private void UpdateTextSpeedBasedOnAudio(AudioClip clip)
    {
        if (!autoAdjustTextSpeed || currentDialogueLines == null || currentDialogueLines.Count == 0)
            return;

        string fullText = string.Join(" ", currentDialogueLines);
        int totalCharacters = fullText.Replace("\n", "").Length;
        float adjustedDuration = clip.length * 0.59f;

        if (totalCharacters > 0)
        {
            textSpeed = Mathf.Max(minTextSpeed, adjustedDuration / totalCharacters);
            Debug.Log($"[DialogueController] Auto-adjusted textSpeed: {textSpeed} (adjusted duration {adjustedDuration}s, {totalCharacters} chars)");
        }
    }
    */

    private void OnVideoError(VideoPlayer source, string message)
    {
        videoPlaybackError = true;
        Debug.LogError("VideoPlayer error: " + message);
    }

    private void StopMediaPlayback()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        // if (audioSource != null && audioSource.isPlaying)
        //     audioSource.Stop();
    }

    public void StopAllDialogueAndMedia()
    {
        StopAllCoroutines();
        textComponent.text = string.Empty;
        isDialogueActive = false;
        dialoguePanel.SetActive(false);

        StopMediaPlayback();

        OnDialogueFinished?.Invoke();

        Debug.Log("[DialogueController] Diálogo, audio y video detenidos.");
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
{
    float time = 0f;
    canvasGroup.alpha = startAlpha;

    while (time < duration)
    {
        canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
        time += Time.unscaledDeltaTime;
        yield return null;
    }

    canvasGroup.alpha = endAlpha;
}
}
