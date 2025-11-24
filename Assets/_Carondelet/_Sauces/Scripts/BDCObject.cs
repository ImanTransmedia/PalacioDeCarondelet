using UnityEngine;

public class BDCObject : MonoBehaviour
{
    void Start()
    {
        if (BDCMode.Instance != null)
        {
            BDCMode.Instance.Registrar(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (BDCMode.Instance != null)
        {
            BDCMode.Instance.Desregistrar(gameObject);
        }
    }
}
