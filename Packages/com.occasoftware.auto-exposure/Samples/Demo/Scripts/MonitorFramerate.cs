using UnityEngine;

namespace OccaSoftware.AutoExposure.Demo
{
    /// <summary>
    /// Monitors frame rate and displays it on screen using IMGUI rendering.
    /// </summary>
    [AddComponentMenu("OccaSoftware/Auto Exposure/")]
    public class MonitorFramerate : MonoBehaviour
    {
        float tLast;
        float trailingT;
        public float factor = 0.005f;

        void Start()
        {
            tLast = Time.time;
            trailingT = 0.0166f;
        }

        void Update()
        {
            float tDelta = Time.time - tLast;
            trailingT = Mathf.Lerp(trailingT, tDelta, factor);
            tLast = Time.time;
        }

        private void OnGUI()
        {
            GUIStyle s = GUI.skin.GetStyle("label");
            s.fontSize = 22;
            float msTiming = trailingT * 1000f;
            int w = Screen.width / 10;
            int h = Screen.height / 10;
            GUI.Label(new Rect(w, h, Screen.width, Screen.height), $"{msTiming:0.0}ms");
        }
    }
}
