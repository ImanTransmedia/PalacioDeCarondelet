using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Animator parentAnimator;
    [SerializeField] private GameObject parent;
    private Rigidbody[] rigidbodies;

    void Awake()
    {
        rigidbodies = parent.gameObject.GetComponentsInChildren<Rigidbody>();
        SetRagdoll(false);
    }

    public void SetRagdoll(bool enabled)
    {
        if (animator != null)
        {
            animator.enabled = !enabled;
            parentAnimator.enabled = !enabled;
        }

        bool isKinematic = !enabled;

        if (rigidbodies == null) return;

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] != null)
            {
                rigidbodies[i].isKinematic = isKinematic;
            }
        }
    }

    public void EnableRagdoll()
    {
        SetRagdoll(true);
    }

    public void DisableRagdoll()
    {
        SetRagdoll(false);
    }
}
