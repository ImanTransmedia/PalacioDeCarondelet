using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("Objetos del tutorial")]
    public List<GameObject> tutorialObjects;

    [Header("Movimiento del personaje")]
    public FirstPersonMovement playerMovement;
    [SerializeField] private UIManager uiManager;

    void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        bool alreadySeen = false;

        if (DoorManager.Instance != null)
            alreadySeen = DoorManager.Instance.ContainsString(currentSceneName);

        bool isFirstTime = !alreadySeen;

        ActivateTutorial(isFirstTime);

        if (isFirstTime && DoorManager.Instance != null)
            DoorManager.Instance.StoreString(currentSceneName);

        if (playerMovement != null)
            playerMovement.isInteracting = false;
    }

    private void ActivateTutorial(bool isFirstTime)
    {
        foreach (var obj in tutorialObjects)
        {
            if (obj != null)
                obj.SetActive(isFirstTime);
        }

        if (playerMovement != null)
        {
            if (isFirstTime)
                uiManager.showCursor();
            else
                uiManager.hideCursor();
            playerMovement.enabled = !isFirstTime;
            playerMovement.isInteracting = isFirstTime;
        }
    }
}
