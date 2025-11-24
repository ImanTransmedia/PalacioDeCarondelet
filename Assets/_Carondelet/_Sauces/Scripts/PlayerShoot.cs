using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Configuración de disparo")]
    public Camera playerCamera;
    public float shootDistance = 1000f;          
    public float projectileSpeed = 30f;
    public float projectileLifetime = 10f;
    public float spawnOffsetFromCamera = 0.25f;  

    [Header("Pool")]
    public PooledBullet bulletPrefab;           
    public int poolSize = 20;                    

    [SerializeField]private  Queue<PooledBullet> _pool = new Queue<PooledBullet>();
    private readonly HashSet<PooledBullet> _inUse = new HashSet<PooledBullet>();

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (bulletPrefab == null)
        {
            Debug.LogError("[PlayerShoot] Falta asignar 'bulletPrefab' en el inspector.");
            enabled = false;
            return;
        }

        // Crear pool fijo de 20
        for (int i = 0; i < poolSize; i++)
        {
            var b = Instantiate(bulletPrefab, Vector3.zero, Quaternion.identity, gameObject.transform);
            b.gameObject.name = $"Bullet_{i:00}";
            b.gameObject.SetActive(false);
            _pool.Enqueue(b);
        }

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && GameManagerBDC.Instance.isBDCMode)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("No hay cámara asignada al PlayerShoot.");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 dir = ray.direction.normalized;

        Vector3 spawnPos = playerCamera.transform.position + dir * spawnOffsetFromCamera;

        var bullet = GetBulletFromPool();
        if (bullet == null)
        {
            Debug.LogWarning("[PlayerShoot] Pool agotado. Espera a que se reciclen.");
            AudioManagerBDC.I.PlaySFX("NoAmmo", volume: 0.7f);

            return;

        }
        else
        {
            AudioManagerBDC.I.PlaySFX ("PlayerShoot", volume: 0.7f);
        }



            bullet.Launch(spawnPos, dir, projectileSpeed, projectileLifetime, ReturnToPool);
    }

    PooledBullet GetBulletFromPool()
    {
        if (_pool.Count == 0) return null; 
        var b = _pool.Dequeue();
        _inUse.Add(b);
        return b;
    }

    void ReturnToPool(PooledBullet b)
    {
        if (_inUse.Contains(b))
        {
            _inUse.Remove(b);
            _pool.Enqueue(b);
        }
    }
}
