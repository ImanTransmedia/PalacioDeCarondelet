using UnityEngine;
using UnityEngine.Events;

public class CollectableItem : MonoBehaviour
{
    [Header("Animación")]
    [SerializeField] float spinSpeed = 90f;          
    [SerializeField] float floatAmplitude = 0.25f;  
    [SerializeField] float floatFrequency = 1.5f;    

    [Header("Detección")]
    [SerializeField] string playerTag = "Player";

    [Header("Evento")]
    public static UnityAction OnPickedUp;

    Vector3 _startPos;
    float _time;

    void Awake()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        _time += Time.deltaTime;
        float offset = Mathf.Sin(_time * Mathf.PI * 2f * floatFrequency) * floatAmplitude;
        var p = _startPos;
        p.y += offset;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            AudioManagerBDC.I.PlaySFX("LanternUp");
            OnPickedUp?.Invoke();
            Destroy(gameObject);
        }
    }
}