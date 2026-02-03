using UnityEngine;
using UnityEngine.EventSystems;

public class PanZoom : MonoBehaviour
{
    [Header("References")]
    public DiegeticObjectInspector diegetic;

    [Header("Enable")]
    public bool ignoreWhenOverUI = true;

    [Header("Mobile detection")]
    [SerializeField] private UIManager uiManager;

    [Header("Mobile Pan")]
    public bool enableMobilePan = true;
    public float panDeadzonePixels = 6f;

    [Header("Mobile Zoom")]
    public float zoomDeadzonePixels = 2f;

    int panFingerId = -1;
    bool isPanning = false;
    Vector2 lastPanPos;

    bool isPinching = false;
    float lastPinchDist = 0f;

    void Update()
    {
        if (diegetic == null)
            return;

        // PC / Editor (debe quedar igual que antes)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        diegetic.UpdateMouseZoom(scroll);

        if (Input.GetMouseButtonDown(1))
            diegetic.BeginMousePan(Input.mousePosition);

        if (Input.GetMouseButton(1))
            diegetic.UpdateMousePan(Input.mousePosition);

        if (Input.GetMouseButtonUp(1))
            diegetic.EndMousePan();

        // Mobile
        if (!IsMobileRuntime())
            return;

        if (Input.touchCount == 0)
        {
            ResetGesture();
            return;
        }

        if (Input.touchCount == 1)
        {
            if (!enableMobilePan)
            {
                ResetGesture();
                return;
            }

            Touch t = Input.GetTouch(0);

            if (ignoreWhenOverUI && IsTouchOverUI(t.fingerId))
            {
                ResetGesture();
                return;
            }

            if (isPinching)
            {
                ResetPanOnly();
                return;
            }

            HandlePanOneFinger(t);
            return;
        }

        if (Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (ignoreWhenOverUI && (IsTouchOverUI(t0.fingerId) || IsTouchOverUI(t1.fingerId)))
            {
                ResetGesture();
                return;
            }

            HandlePinchTwoFingers(t0, t1);
        }
    }

    bool IsMobileRuntime()
    {
        if (uiManager != null)
            return uiManager.isMobile;

        return Application.isMobilePlatform;
    }

    void HandlePanOneFinger(Touch t)
    {
        if (t.phase == TouchPhase.Began)
        {
            panFingerId = t.fingerId;
            lastPanPos = t.position;
            isPanning = false;
            return;
        }

        if (t.fingerId != panFingerId)
            return;

        Vector2 delta = t.position - lastPanPos;

        if (!isPanning)
        {
            if (delta.sqrMagnitude < panDeadzonePixels * panDeadzonePixels)
            {
                lastPanPos = t.position;
                return;
            }
            isPanning = true;
        }

        diegetic.ApplyTouchPanDelta(delta);
        lastPanPos = t.position;

        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            ResetPanOnly();
    }

    void HandlePinchTwoFingers(Touch t0, Touch t1)
    {
        float dist = Vector2.Distance(t0.position, t1.position);

        if (!isPinching || t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            isPinching = true;
            isPanning = false;
            panFingerId = -1;
            lastPinchDist = dist;
            return;
        }

        float deltaDist = dist - lastPinchDist;

        if (Mathf.Abs(deltaDist) < zoomDeadzonePixels)
        {
            lastPinchDist = dist;
            return;
        }

        diegetic.ApplyTouchZoomDelta(deltaDist);
        lastPinchDist = dist;

        if (t0.phase == TouchPhase.Ended || t0.phase == TouchPhase.Canceled ||
            t1.phase == TouchPhase.Ended || t1.phase == TouchPhase.Canceled)
        {
            ResetGesture();
        }
    }

    bool IsTouchOverUI(int fingerId)
    {
        if (!ignoreWhenOverUI) return false;
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    void ResetPanOnly()
    {
        isPanning = false;
        panFingerId = -1;
    }

    void ResetGesture()
    {
        ResetPanOnly();
        isPinching = false;
        lastPinchDist = 0f;
    }
}
