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
    public float minTextSpeed = 0.02f;
    private float textSpeed = 0.0f;
    private float waitBetweenSegments = 0.8f;
    private int linesPerSegment = 1;

    public List<LocalizedDialogue> dialogueList;

    [Header("Multimedia Settings")]
    public VideoPlayer videoPlayer;
    // public AudioSource audioSource;
    public AudioSource videoAudioOutput;

    private List<string> currentDialogueLines;
    private int currentLineIndex;
    private bool isDialogueActive = false;

    public bool startDialogueAutomatically = false;
    public int dialogueIndexToShow = 0;
    private float timeBeforeDisappear = 3.5f;
    private string sessionKey;
    public bool autoAdjustTextSpeed = false;

    void Start()
    {
        textComponent.text = string.Empty;
        dialoguePanelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        dialoguePanel.SetActive(false);
        dialoguePanelCanvasGroup.alpha = 0f;

        if (videoPlayer != null)
            videoPlayer.errorReceived += OnVideoError;

        GameObject audioGO = GameObject.Find("AudioSourceVideo");
        videoAudioOutput = audioGO.GetComponent<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, videoAudioOutput);

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
        yield return new WaitForSeconds(6f);
        Debug.Log("Subescenas listas y iniciando Baron");
        ShowDialogue(dialogueIndexToShow);
    }

    public void ShowDialogue(int index)
    {
        if (isDialoguePlaying)
        {
            dialogueQueue.Enqueue(index);
            return;
        }

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
        textSpeed = dialogue.finalSpeed * 1.3f;
        currentDialogueLines = SplitTextIntoLines(localizedText);
        currentLineIndex = 0;

        StartCoroutine(PrepareAndStartDialogue(index));
    }

    private IEnumerator PrepareAndStartDialogue(int index)
    {
        isDialogueActive = true;
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
                videoPlayer.time = 0;
                videoPlayer.Play();

                yield return new WaitUntil(() => videoPlayer.isPlaying /*&& audioSource.isPlaying*/);

                if (dialoguePanelCanvasGroup != null)
                    dialoguePanelCanvasGroup.alpha = 1f;

                yield return new WaitForSeconds(2f);
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

            yield return new WaitForSeconds(waitBetweenSegments);
        }

        yield return new WaitForSeconds(timeBeforeDisappear);

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
        if (videoPlayer == null) yield break;

        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");
        videoPlayer.url = fullPath;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;
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
