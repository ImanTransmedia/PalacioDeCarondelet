using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("Objetos del tutorial")]
    public List<GameObject> tutorialObjects;

    [Header("Movimiento del personaje")]
    public FirstPersonMovement playerMovement;

    void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (DoorManager.Instance != null && !DoorManager.Instance.ContainsString(currentSceneName))
        {
            ActivateTutorial(true);
            DoorManager.Instance.StoreString(currentSceneName);
        }

        else
        {
            ActivateTutorial(false);
        }
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
            playerMovement.enabled = !isFirstTime;
            playerMovement.isInteracting = false;
        }
    }
}
