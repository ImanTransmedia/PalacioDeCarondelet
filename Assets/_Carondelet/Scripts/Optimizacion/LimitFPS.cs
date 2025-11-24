using UnityEngine;

public class LimitFPS : MonoBehaviour
{
    public static LimitFPS Instance { get; private set; }
    public int targetFPS = 30;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Application.targetFrameRate = targetFPS;
        DontDestroyOnLoad(gameObject);
    }
}
