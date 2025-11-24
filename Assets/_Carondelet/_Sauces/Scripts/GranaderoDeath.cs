using UnityEngine;
using UnityEngine.Events;

public class GranaderoDeath : MonoBehaviour
{
    public ParticleSystem deathParticles;
    public UnityEvent onGranaderoDeath;
    [SerializeField] int score = 10;

    [Range(0f, 1f)]
    public float slowVersionChance = 0.3f;
    [SerializeField] float ragdollDuration = 3f;

    private bool hasBecomeSlow = false;
    private bool isDead = false;

    public void OnDeath()
    {
        if (isDead) return;

        FollowPlayer follow = GetComponent<FollowPlayer>();

        if (!hasBecomeSlow && follow != null && !follow.isSlow && Random.value < slowVersionChance)
        {
            hasBecomeSlow = true;
            follow.BecomeSlowFromDeath();
            return;
        }

        isDead = true;

        if (follow != null)
        {
            follow.DetenerMovimiento();
        }

        Ragdoll ragdoll = GetComponent<Ragdoll>();
        if (ragdoll != null)
        {
            ragdoll.EnableRagdoll();
        }

        onGranaderoDeath?.Invoke();
        AudioManagerBDC.I.PlaySFX("EnemyDeath", volume: 0.8f);
        GameManagerBDC.Instance.AddScore(score);

        float destroyTime = ragdollDuration;

        if (deathParticles != null)
        {
            deathParticles.Play();
            float particlesDuration = deathParticles.main.duration;
            if (particlesDuration > destroyTime)
            {
                destroyTime = particlesDuration;
            }
        }

        Destroy(gameObject, destroyTime);
    }
}