using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AutoLoadNextScene : MonoBehaviour
{
    [Header("Referencias")]
    public UIManager uimanager;

    [Header("Tiempo de espera botón")]
    public float btntimer = 5f;
    public GameObject btn;

    private bool hasActivated = false;

    private LoadSceneAddressable loadSceneAddressable;
    private string sceneKey;
    public DialogueController dialogueController;
    public GameObject baronPanel;

    void Start()
    {
        loadSceneAddressable = GetComponent<LoadSceneAddressable>();

        if (loadSceneAddressable == null)
        {
            Debug.LogError("[AutoLoadNextScene] No se encontró el componente LoadSceneAddressable en este GameObject.");
            return;
        }

        sceneKey = "SceneLoaded_" + SceneManager.GetActiveScene().name;

      /*   if (DoorManager.Instance.ContainsString(sceneKey))
        {
            loadSceneAddressable.LoadScene();
            enabled = false;
        }
        else
        {
            DoorManager.Instance.StoreString(sceneKey);
        } */

        StartCoroutine(ActivateBaronPanelAfterDelay());

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished += OnDialogueFinished;
        }
    }

    private void OnDialogueFinished()
    {
       // uimanager.ClosePanel(baronPanel);
        uimanager.FadeIn();
        StartCoroutine(LoadSceneAfterDelay(0f));
    }

    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        loadSceneAddressable.LoadScene();
        enabled = false;
    }

    IEnumerator ActivateBaronPanelAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        baronPanel.SetActive(true);
        dialogueController.ShowDialogue(0);
    }

    void Update()
    {
        btntimer -= Time.deltaTime;

        if (btntimer <= 0f)
        {
            uimanager.OpenPanel(btn);
        }
    }
}
