using UnityEngine;

public class HitBoxNotifier : MonoBehaviour
{
    [SerializeField] private GranaderoDeath parentDeath;


    void Awake()
    {
        parentDeath = GetComponentInParent<GranaderoDeath>();
    }

    public void NotifyHit()
    {
        if (parentDeath != null)
        {
            parentDeath.OnDeath();
        }
    }

}