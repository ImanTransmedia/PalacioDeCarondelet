using UnityEngine;
using UnityEngine.Events;

public class LanternController : MonoBehaviour
{
    [SerializeField] private float drainRate = 5f; 
    public  static UnityAction OnLanternOver;


    void Update()
    {
        HandleInput();
        DrainLantern();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButton(1))
        {
            GameManagerBDC.Instance.isUsingLantern = true;
        }
        else
        {
            GameManagerBDC.Instance.isUsingLantern = false;
        }
    }

    private void DrainLantern()
    {
        if (!GameManagerBDC.Instance.isUsingLantern || GameManagerBDC.Instance.LanternCharge <= 0f)
            return;

        GameManagerBDC.Instance.LanternCharge -= drainRate * Time.deltaTime;
        GameManagerBDC.Instance.LanternCharge = Mathf.Max(0f, GameManagerBDC.Instance.LanternCharge);

        if (GameManagerBDC.Instance.LanternCharge <= 0f)
        {
            OnLanternOver?.Invoke();
            GameManagerBDC.Instance.isUsingLantern = false;
        }
    }
}
