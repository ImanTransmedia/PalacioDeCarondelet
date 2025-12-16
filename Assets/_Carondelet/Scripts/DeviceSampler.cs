using TMPro;
using UnityEngine;
using System.Runtime.InteropServices;

public class DeviceSampler : MonoBehaviour
{
    [SerializeField] private TMP_Text deviceInfoText;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string DS_GetDeviceString();

    [DllImport("__Internal")]
    private static extern int DS_IsTouchDevice();
#endif

    private void Start()
    {
        string info = GetDeviceInfoSafe();
        if (deviceInfoText != null)
            deviceInfoText.text = info;
    }

    private string GetDeviceInfoSafe()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string device = DS_GetDeviceString();
            bool touch = DS_IsTouchDevice() == 1;

            string category = device.Contains("Android") || device.Contains("iOS") || device == "Mobile"
                ? "Mobile"
                : "PC";

            return $"{device}\nTouch: {(touch ? "Yes" : "No")}\nCategory: {category}";
        }
        catch
        {
            return "PC";
        }
#else
        if (Application.isMobilePlatform)
            return "Mobile";
        return "PC";
#endif
    }
}
