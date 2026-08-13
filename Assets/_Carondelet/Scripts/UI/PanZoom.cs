using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanZoom : MonoBehaviour
{
    [Header("References")]
    public DiegeticObjectInspector diegetic;

    [Header("Runtime / UI")]
    public bool ignoreWhenOverUI = true;
    [SerializeField] private UIManager uiManager;

    [Header("Targets")]
    public Camera targetCamera;
    public Transform diorama;

    [Header("Pan (PC + Mobile)")]
    public bool enablePan = true;
    public float panUnitsPerPixel = 0.0025f;
    public float panDeadzonePixels = 6f;
    public float panSmoothSpeed = 10f;

    [Header("Pan Limits")]
    public float minX = -1.5f;
    public float maxX = 1.5f;
    public float minY = -1.5f;
    public float maxY = 1.5f;

    [Header("Elastic Pan")]
    public bool useElasticPan = true;
    public float elasticExtraX = 0.35f;
    public float elasticExtraY = 0.35f;
    [Range(0.05f, 1f)] public float elasticDragFactor = 0.35f;
    public float elasticReturnSpeed = 12f;

    [Header("Zoom (PC mouse wheel)")]
    public bool enableZoom = true;
    public float zoomSensitivityWheel = 10f;

    [Header("Zoom (Mobile pinch)")]
    public float zoomSensitivityPinch = 0.08f;
    public float zoomDeadzonePixels = 2f;

    [Header("FOV Limits")]
    public float initialFOV = 60f;
    public float minNormalFOV = 40f;
    public float minInspectFOV = 20f;
    public float normalZoomSmoothSpeed = 10f;
    public float inspectZoomSmoothSpeed = 10f;

    [Header("Reset")]
    public bool resetWhenExitInspect = true;
    public bool blockInputAndSmoothing = false;

    float targetFOV;

    bool isDraggingMouse = false;
    Vector2 lastMousePos;
    Vector3 targetDioramaPos;
    Vector3 initialDioramaPos;

    int panFingerId = -1;
    bool isPanningTouch = false;
    Vector2 lastPanPos;

    bool isPinching = false;
    float lastPinchDist = 0f;

    bool lastInspecting = false;
    readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    Camera Cam
    {
        get
        {
            if (targetCamera != null) return targetCamera;
            if (diegetic != null && diegetic.Cam != null) return diegetic.Cam;
            return null;
        }
    }

    Transform Diorama
    {
        get
        {
            if (diorama != null) return diorama;
            if (diegetic != null && diegetic.diorama != null) return diegetic.diorama.transform;
            return null;
        }
    }

    void Start()
    {
        var cam = Cam;
        if (cam != null)
        {
            if (initialFOV <= 0f) initialFOV = cam.fieldOfView;
            cam.fieldOfView = initialFOV;
            targetFOV = initialFOV;
        }

        var d = Diorama;
        if (d != null)
        {
            initialDioramaPos = d.position;
            targetDioramaPos = d.position;
        }

        lastInspecting = diegetic != null && diegetic.IsInspecting;
    }

    void Update()
    {
        if (diegetic == null)
            return;

        if (blockInputAndSmoothing)
            return;

        bool inspectingNow = diegetic.IsInspecting;

        if (resetWhenExitInspect && lastInspecting && !inspectingNow)
        {
            ResetView(false);
        }

        lastInspecting = inspectingNow;

        if (IsMobileRuntime())
            UpdateMobile();
        else
            UpdatePC();
    }

    void UpdatePC()
    {
        var cam = Cam;
        var d = Diorama;
        if (cam == null || d == null) return;

        if (enableZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                ApplyZoomDelta(-scroll * zoomSensitivityWheel);
        }

        if (enablePan)
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (ignoreWhenOverUI && IsMouseOverUI())
                {
                    isDraggingMouse = false;
                }
                else
                {
                    isDraggingMouse = true;
                    lastMousePos = Input.mousePosition;
                }
            }

            if (Input.GetMouseButton(1) && isDraggingMouse)
            {
                Vector2 cur = Input.mousePosition;
                Vector2 delta = cur - lastMousePos;

                if (delta.sqrMagnitude >= panDeadzonePixels * panDeadzonePixels)
                    ApplyPanDelta(delta);

                lastMousePos = cur;
            }

            if (Input.GetMouseButtonUp(1))
                isDraggingMouse = false;
        }

        SmoothDiorama();
        SmoothFov();
    }

    void UpdateMobile()
    {
        var cam = Cam;
        var d = Diorama;
        if (cam == null || d == null) return;

        if (Input.touchCount == 0)
        {
            ResetGesture();
            SmoothDiorama();
            SmoothFov();
            return;
        }

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (ignoreWhenOverUI && IsTouchOverUI(t))
            {
                ResetGesture();
                SmoothDiorama();
                SmoothFov();
                return;
            }

            if (isPinching)
            {
                if (t.phase == TouchPhase.Began)
                    ResetGesture();
                else
                {
                    SmoothDiorama();
                    SmoothFov();
                    return;
                }
            }

            if (enablePan)
                HandlePanOneFinger(t);

            SmoothDiorama();
            SmoothFov();
            return;
        }

        if (Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (ignoreWhenOverUI && (IsTouchOverUI(t0) || IsTouchOverUI(t1)))
            {
                ResetGesture();
                SmoothDiorama();
                SmoothFov();
                return;
            }

            if (enableZoom)
                HandlePinchTwoFingers(t0, t1);

            SmoothDiorama();
            SmoothFov();
        }
    }

    void HandlePanOneFinger(Touch t)
    {
        if (t.phase == TouchPhase.Began)
        {
            panFingerId = t.fingerId;
            lastPanPos = t.position;
            isPanningTouch = false;
            return;
        }

        if (t.fingerId != panFingerId)
            return;

        Vector2 delta = t.position - lastPanPos;

        if (!isPanningTouch)
        {
            if (delta.sqrMagnitude < panDeadzonePixels * panDeadzonePixels)
            {
                lastPanPos = t.position;
                return;
            }
            isPanningTouch = true;
        }

        ApplyPanDelta(delta);
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
            ResetPanOnly();
            lastPinchDist = dist;
            return;
        }

        float deltaDist = dist - lastPinchDist;
        lastPinchDist = dist;

        if (Mathf.Abs(deltaDist) < zoomDeadzonePixels)
            return;

        ApplyZoomDelta(-deltaDist * zoomSensitivityPinch);

        if (t0.phase == TouchPhase.Ended || t0.phase == TouchPhase.Canceled ||
            t1.phase == TouchPhase.Ended || t1.phase == TouchPhase.Canceled)
        {
            ResetGesture();
        }
    }

    void ApplyPanDelta(Vector2 screenDelta)
    {
        var d = Diorama;
        if (d == null) return;

        Vector3 deltaWorld = new Vector3(screenDelta.x, screenDelta.y, 0f) * panUnitsPerPixel;

        // importante: acumular sobre el target, no sobre la posición actual
        Vector3 desired = targetDioramaPos + deltaWorld;

        if (useElasticPan)
        {
            desired.x = ApplyElasticAxis(desired.x, minX, maxX, elasticExtraX);
            desired.y = ApplyElasticAxis(desired.y, minY, maxY, elasticExtraY);
        }
        else
        {
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        targetDioramaPos = desired;
    }

    float ApplyElasticAxis(float value, float min, float max, float extraLimit)
    {
        if (value < min)
        {
            float over = min - value;
            return Mathf.Max(min - extraLimit, min - over * elasticDragFactor);
        }

        if (value > max)
        {
            float over = value - max;
            return Mathf.Min(max + extraLimit, max + over * elasticDragFactor);
        }

        return value;
    }

    void ApplyZoomDelta(float fovDelta)
    {
        var cam = Cam;
        if (cam == null) return;

        bool inspecting = diegetic != null && diegetic.IsInspecting;
        float minFov = inspecting ? minInspectFOV : minNormalFOV;

        targetFOV = Mathf.Clamp(targetFOV + fovDelta, minFov, initialFOV);
    }

    void SmoothDiorama()
    {
        var d = Diorama;
        if (d == null) return;

        bool isActivelyPanning = isDraggingMouse || isPanningTouch;

        if (!isActivelyPanning)
        {
            Vector3 clampedTarget = targetDioramaPos;
            clampedTarget.x = Mathf.Clamp(clampedTarget.x, minX, maxX);
            clampedTarget.y = Mathf.Clamp(clampedTarget.y, minY, maxY);

            targetDioramaPos = Vector3.Lerp(
                targetDioramaPos,
                clampedTarget,
                elasticReturnSpeed * Time.deltaTime
            );
        }

        d.position = Vector3.Lerp(d.position, targetDioramaPos, panSmoothSpeed * Time.deltaTime);
    }

    void SmoothFov()
    {
        var cam = Cam;
        if (cam == null) return;

        bool inspecting = diegetic != null && diegetic.IsInspecting;
        float smooth = inspecting ? inspectZoomSmoothSpeed : normalZoomSmoothSpeed;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, smooth * Time.deltaTime);
    }

    public void ResetView(bool instant = false)
    {
        var cam = Cam;
        var d = Diorama;

        ResetGesture();

        if (d != null)
        {
            targetDioramaPos = initialDioramaPos;
            if (instant)
                d.position = initialDioramaPos;
        }

        if (cam != null)
        {
            targetFOV = initialFOV;
            if (instant)
                cam.fieldOfView = initialFOV;
        }
    }

    bool IsMobileRuntime()
    {
        if (uiManager != null)
            return uiManager.isMobile;

        return Application.isMobilePlatform;
    }

    bool IsTouchOverUI(Touch touch)
    {
        if (!ignoreWhenOverUI) return false;
        if (EventSystem.current == null) return false;

        // En WebGL mobile el pointerId del EventSystem no siempre coincide con
        // Touch.fingerId. El raycast por posicion detecta el ScrollRect de forma fiable.
        var pointerData = new PointerEventData(EventSystem.current)
        {
            pointerId = touch.fingerId,
            position = touch.position
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        foreach (RaycastResult result in uiRaycastResults)
        {
            if (result.gameObject.GetComponentInParent<ScrollRect>() != null)
                return true;
        }

        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }

    bool IsMouseOverUI()
    {
        if (!ignoreWhenOverUI) return false;
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    void ResetPanOnly()
    {
        isPanningTouch = false;
        panFingerId = -1;
        isDraggingMouse = false;
    }

    void ResetGesture()
    {
        ResetPanOnly();
        isPinching = false;
        lastPinchDist = 0f;
    }
}
