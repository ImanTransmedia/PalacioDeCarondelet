using UnityEngine;

public class PlatformDisabler : MonoBehaviour
{
    public bool isPcElement;
    void Start()
    {
        bool esMobile = Application.isMobilePlatform;


        if (esMobile)
        {
           gameObject.SetActive(!isPcElement);
        }
        else
        {
            gameObject.SetActive(isPcElement);
        }
    }
}
