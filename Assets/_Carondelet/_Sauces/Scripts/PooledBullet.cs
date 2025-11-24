using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PooledBullet : MonoBehaviour
{
    private Rigidbody _rb;
    private Coroutine _lifeCo;
    private Action<PooledBullet> _returnToPool;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;              
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var col = GetComponent<Collider>();
        col.isTrigger = false;                
    }

    public void Launch(Vector3 position, Vector3 direction, float speed, float lifeSeconds, Action<PooledBullet> returnToPool)
    {
        _returnToPool = returnToPool;
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        gameObject.SetActive(true);

        _rb.linearVelocity = direction * speed;

        if (_lifeCo != null) StopCoroutine(_lifeCo);
        _lifeCo = StartCoroutine(LifeTimer(lifeSeconds));
    }

    IEnumerator LifeTimer(float t)
    {
        yield return new WaitForSeconds(t);
        Despawn();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.useGravity = true;
        if (collision.collider.CompareTag("Enemy"))
        {   
            var death = collision.collider.GetComponent<HitBoxNotifier>();
            if (death != null) death.NotifyHit();
        }

        //Despawn();
    }

    private void Despawn()
    {
        if (_lifeCo != null) { StopCoroutine(_lifeCo); _lifeCo = null; }
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        gameObject.SetActive(false);
        _returnToPool?.Invoke(this);
        AudioManagerBDC.I.PlaySFX("BulletPop", volume: 0.3f);
    }
}
