using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [Header("Configuracion de seguimiento")]
    public string playerTag = "Player";
    public float speed = 3f;
    public float stoppingDistance = 1.5f;
    public Animator animator;

    [Header("Modo lento aleatorio")]
    [Range(0f, 1f)]
    public float slowChance = 0.5f;
    public float slowSpeedMultiplier = 0.5f;
    public bool isSlow = false;

    private Transform player;
    private bool canMove = true;
    private float baseSpeed;

    void Awake()
    {
        baseSpeed = speed;
    }

    void OnEnable()
    {
        isSlow = Random.value < slowChance;

        if (animator != null)
        {
            if (isSlow)
            {
                speed = baseSpeed * slowSpeedMultiplier;
                animator.SetFloat("Move", 1);
            }
            else
            {
                speed = baseSpeed;
                animator.SetFloat("Move", 0);
            }
        }

        canMove = true;
    }

    void Start()
    {
        if (!GameManagerBDC.Instance.isBDCMode) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("No se encontro un objeto con el tag 'Player'.");
        }

        GranaderoDeath death = GetComponent<GranaderoDeath>();
        if (death != null)
        {
            death.onGranaderoDeath.AddListener(DetenerMovimiento);
        }
    }

    void Update()
    {
        if (player == null || !GameManagerBDC.Instance.isBDCMode || !canMove) return;

        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        if (distance > stoppingDistance)
        {
            direction.Normalize();
            transform.position += direction * speed * Time.deltaTime;
            transform.forward = direction;
        }
    }

    public void BecomeSlowFromDeath()
    {
        isSlow = true;
        speed = baseSpeed * slowSpeedMultiplier;
        canMove = true;

        if (animator != null)
        {
            animator.SetFloat("Move", 1);
        }
    }

    public void DetenerMovimiento()
    {
        canMove = false;
    }
}