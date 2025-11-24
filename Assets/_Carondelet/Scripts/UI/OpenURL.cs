using UnityEngine;

public class OpenURL : MonoBehaviour
{
    public string url;

    public void OpenLink()
    {
        Application.OpenURL(url);
    }
}

