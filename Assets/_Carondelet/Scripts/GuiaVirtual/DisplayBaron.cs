using UnityEngine;
using UnityEditor;

public class DisplayBaron : MonoBehaviour
{
    public int dialogueIndexToShow = 0;
    public DialogueController dialogueController;

    private bool hasTriggered = false;



    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        string key = "DisplayBaronTriggered_" + gameObject.scene.name + "_" + gameObject.name;

        Debug.Log("DisplayBaron triggered: " + key);

        if (!DoorManager.Instance.ContainsString(key))
        {
            dialogueController.ShowDialogue(dialogueIndexToShow);
            DoorManager.Instance.StoreString(key);
        }
    }
}
