using UnityEngine;

public class AnimatorToggle : MonoBehaviour
{
    [Header("Animator to control")]
    public Animator targetAnimator;

    public void OpenAnimation()
    {
        if (targetAnimator != null)
        {
            targetAnimator.SetBool("isOpen", true);
        }
    }

    public void CloseAnimation()
    {
        if (targetAnimator != null)
        {
            targetAnimator.SetBool("isOpen", false);
        }
    }
}
