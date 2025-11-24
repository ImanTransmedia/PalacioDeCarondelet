using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    [Header("Script References")]
    public UIManager uiManager;
    public int totalObjectives = 0;
    public int currentObjectiveIndex = 0;

    [Header("Objective Lists")]
    public List<LocalizedString> objectivesPC;
    public List<LocalizedString> objectivesMobile;

    [Header("UI")]
    public TextMeshProUGUI currentObjectiveText;
    public GameObject objectiveParent;

    private Animator animator;

    private int objectiveChangingHash = Animator.StringToHash("objectiveChanging");
    private int isClosingHash = Animator.StringToHash("isClosing");

    public bool isChanging;
    private float objectiveChangeDelay = 0.51f;

    [Header("Timing Settings")]
    public float objectiveVisibleTime = 5f;

    private Coroutine hideObjectiveCoroutine;

    private void Start()
    {
        
        animator = objectiveParent.GetComponent<Animator>();
        totalObjectives = Mathf.Max(objectivesPC.Count, objectivesMobile.Count);
        if (totalObjectives == 0)
    {
        objectiveParent.SetActive(false);
        return;
    }
    objectiveParent.SetActive(true);
        UpdateObjectiveUI();
    }

    private void Update()
    {
        CheckAnimationState();
    }

    private void CheckAnimationState()
    {
         if (animator == null || animator.runtimeAnimatorController == null)
             return; 
         if (!objectiveParent.activeInHierarchy)
             return;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.GetBool(objectiveChangingHash) && stateInfo.IsTag("ObjectiveChange") && stateInfo.normalizedTime >= 1f)
        {
            animator.SetBool(objectiveChangingHash, false);
        }

        if (animator.GetBool(isClosingHash) && stateInfo.IsTag("Close") && stateInfo.normalizedTime >= 1f)
        {
            animator.SetBool(isClosingHash, false);
        }
    }

    private void SetObjectiveTextByIndex(int index)
    {
        var list = uiManager != null && uiManager.isMobile ? objectivesMobile : objectivesPC;

        if (index >= 0 && index < list.Count)
        {
            list[index].StringChanged += SetCurrentObjectiveText;
            list[index].RefreshString();
        }
    }

    private void SetCurrentObjectiveText(string value)
    {
        if (currentObjectiveText != null)
        {
            currentObjectiveText.text = value;
        }
    }

    public void AdvanceObjective()
    {
        currentObjectiveIndex++;
        if (currentObjectiveIndex >= totalObjectives)
        {
            currentObjectiveIndex = totalObjectives - 1;
        }

        UpdateObjectiveUI();
    }

    public void UpdateObjectiveUI()
    {
        if (currentObjectiveIndex >= 0 && currentObjectiveIndex < totalObjectives)
        {
            animator.SetBool(objectiveChangingHash, true);
            isChanging = true;
            StartCoroutine(ResetObjectiveChangingFlag());

            SetObjectiveTextByIndex(currentObjectiveIndex);

            if (hideObjectiveCoroutine != null)
            {
                StopCoroutine(hideObjectiveCoroutine);
            }

            hideObjectiveCoroutine = StartCoroutine(HideObjectiveAfterDelay());
        }

        animator.SetBool(isClosingHash, false);
    }

    public void NextObjective()
    {
        if (currentObjectiveIndex < totalObjectives)
        {
            currentObjectiveIndex++;
            UpdateObjectiveUI();
        }
    }

    public void PreviousObjective()
    {
        if (currentObjectiveIndex > 0)
        {
            currentObjectiveIndex--;
            UpdateObjectiveUI();
        }
    }

    private IEnumerator ResetObjectiveChangingFlag()
    {
        yield return new WaitForSeconds(objectiveChangeDelay);

        animator.SetBool(objectiveChangingHash, false);
        isChanging = false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.IsTag("ObjectiveChange") && stateInfo.normalizedTime < 1f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        yield return new WaitForSeconds(0.1f);
        SetObjectiveTextByIndex(currentObjectiveIndex);
    }

    private IEnumerator HideObjectiveAfterDelay()
    {
        yield return new WaitForSeconds(objectiveVisibleTime);

        if (animator != null)
        {
            animator.SetBool(isClosingHash, true);
        }
    }
}
