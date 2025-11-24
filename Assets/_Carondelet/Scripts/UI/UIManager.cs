using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Script References")]
    public SceneLoader sceneLoader;
    public AccessibilityManager accessibilityManager;
    public FirstPersonMovement firstPerson;

    public bool playerStartsDisable = false;

    [Header("Mobile device status")]
    public bool isMobile;

    [Header("Desktop UI Elements")]
    public List<GameObject> desktopUIObjects;

    [Header("Mobile UI Elements")]
    public List<GameObject> mobileUIObjects;

    [Header("Fade Settings")]
    public CanvasGroup fadeCanva;
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 1.5f;
    public float fadePanelsDuration = 0.15f;

    private void Start()
    {
            if (firstPerson == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    firstPerson = player.GetComponent<FirstPersonMovement>();
                       if (playerStartsDisable == true)
                        {
                            showCursor();
                        }

                        else
                        {
                            hideCursor();
                        }
                }
        

            }

     

        if (accessibilityManager == null)
        {
            accessibilityManager = GetComponent<AccessibilityManager>();
        }
        FadeOut();
        isMobile = DetectMobileWebGL();

        if (isMobile)
        {
            ShowMobileUI();
            //   accessibilityManager.enabled = false;
        }
        else
        {
            ShowDesktopUI();
            //   accessibilityManager.enabled = true;
        }
       // showCursor();
    }

    bool DetectMobileWebGL()
    {
        return Application.isMobilePlatform;
    }

    public void StartAsyncLoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneWithFade(sceneName));
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        yield return FadeInCorutine();
        if (sceneLoader != null)
        {
            sceneLoader.StartAsyncLoadScene(sceneName);
        }
    }

   public void TogglePanel(GameObject panel, bool state)
{
    if (panel != null)
    {
        Animator animator = panel.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isOpen", state);
        }
        else
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadePanel(panel, canvasGroup, state));
            }
            else
            {
                panel.SetActive(state);
            }
        }
    }
}

    public void OpenPanel(GameObject panel)
    {
        TogglePanel(panel, true);
    }

    public void ClosePanel(GameObject panel)
    {
        TogglePanel(panel, false);
    }

    private IEnumerator FadePanel(GameObject panel, CanvasGroup canvasGroup, bool fadeIn)
{
    float duration = fadePanelsDuration;
    float startAlpha = fadeIn ? 0f : 1f;
    float endAlpha = fadeIn ? 1f : 0f;

    panel.SetActive(true); 
    canvasGroup.alpha = startAlpha;

    float elapsed = 0f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
        yield return null;
    }

    canvasGroup.alpha = endAlpha;
    if (!fadeIn)
    {
        panel.SetActive(false); 
    }
}
    public IEnumerator FadeInCorutine()
    {
        fadeCanva.gameObject.SetActive(true);
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanva.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }
        fadeCanva.alpha = 1f;
    }

    public IEnumerator FadeOutCorutine()
    {
        fadeCanva.gameObject.SetActive(true);
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanva.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
        fadeCanva.alpha = 0f;
        fadeCanva.gameObject.SetActive(false);
    }

    public void FadeIn()
    {
        StartCoroutine(FadeInCorutine());
    }

    public void FadeOut()
    {
        StartCoroutine(FadeOutCorutine());
    }

    void ShowMobileUI()
    {
        SetActiveObjects(mobileUIObjects, true);
        SetActiveObjects(desktopUIObjects, false);
    }

    void ShowDesktopUI()
    {
        SetActiveObjects(mobileUIObjects, false);
        SetActiveObjects(desktopUIObjects, true);
    }

    void SetActiveObjects(List<GameObject> objects, bool state)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }

    public void showCursor()
    {
        Debug.Log("Cursor activado, player desactivado");
        if (firstPerson != null)
        {
                firstPerson.isInteracting = true;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void hideCursor()
    {
        Debug.Log("Cursor desactivado, player activado");
        if (firstPerson != null){
              firstPerson.isInteracting = false;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
