using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Centraliza TODA la lógica de Paneo + Zoom (PC y Mobile) para el diorama/cámara.
/// DiegeticObjectInspector debe quedarse solo con la lógica de inspección (mover a target / reset / UI).
/// </summary>
public class PanZoom : MonoBehaviour
{
    [Header("References")]
    public DiegeticObjectInspector diegetic;

    [Header("Runtime / UI")]
    public bool ignoreWhenOverUI = true;
    [SerializeField] private UIManager uiManager; // si existe en tu proyecto, úsalo para detectar mobile en WebGL

    [Header("Targets")]
    [Tooltip("Si está vacío, se usa diegetic.Cam")]
    public Camera targetCamera;
    [Tooltip("Si está vacío, se usa diegetic.diorama")]
    public Transform diorama;

    [Header("Pan (PC + Mobile)")]
    public bool enablePan = true;
    [Tooltip("Unidades del mundo por pixel (ajusta para sentirlo tipo Sketchfab)")]
    public float panUnitsPerPixel = 0.0025f;
    public float panDeadzonePixels = 6f;
    public float panSmoothSpeed = 10f;
    public float minX = -1.5f;
    public float maxX = 1.5f;
    public float minY = -1.5f;
    public float maxY = 1.5f;

    [Header("Zoom (PC mouse wheel)")]
    public bool enableZoom = true;
    public float zoomSensitivityWheel = 10f;

    [Header("Zoom (Mobile pinch)")]
    [Tooltip("Multiplicador para convertir delta de pixeles del pinch a cambio de FOV")]
    public float zoomSensitivityPinch = 0.08f;
    public float zoomDeadzonePixels = 2f;

    [Header("FOV Limits")]
    public float initialFOV = 60f;
    public float minNormalFOV = 40f;
    public float minInspectFOV = 20f;
    public float normalZoomSmoothSpeed = 10f;
    public float inspectZoomSmoothSpeed = 10f;

    // Internal state
    float targetFOV;

    bool isDraggingMouse = false;
    Vector2 lastMousePos;
    Vector3 targetDioramaPos;

    // Mobile gesture state
    int panFingerId = -1;
    bool isPanningTouch = false;
    Vector2 lastPanPos;

    bool isPinching = false;
    float lastPinchDist = 0f;

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
            targetDioramaPos = d.position;
    }

    void Update()
    {
        if (diegetic == null)
            return;

        // IMPORTANTÍSIMO:
        // En mobile (sobre todo WebGL), los toques pueden disparar también los inputs de mouse.
        // Por eso separamos completamente el flujo PC vs Mobile.
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

        // Zoom mouse wheel
        if (enableZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                ApplyZoomDelta(-scroll * zoomSensitivityWheel);
        }

        // Pan con click derecho
        if (!enablePan) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (ignoreWhenOverUI && IsMouseOverUI())
            {
                isDraggingMouse = false;
                return;
            }

            isDraggingMouse = true;
            lastMousePos = Input.mousePosition;
            targetDioramaPos = d.position;
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

        // suavizado final
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

            if (ignoreWhenOverUI && IsTouchOverUI(t.fingerId))
            {
                ResetGesture();
                SmoothDiorama();
                SmoothFov();
                return;
            }

            // Si venimos de un pinch, exigimos que el usuario "re-inicie" el gesto para pan.
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

        // 2+ dedos => pinch zoom
        if (Input.touchCount >= 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (ignoreWhenOverUI && (IsTouchOverUI(t0.fingerId) || IsTouchOverUI(t1.fingerId)))
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

        // dist aumenta => dedos se separan => acercar (menos FOV)
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

        targetDioramaPos = d.position + deltaWorld;
        targetDioramaPos.x = Mathf.Clamp(targetDioramaPos.x, minX, maxX);
        targetDioramaPos.y = Mathf.Clamp(targetDioramaPos.y, minY, maxY);
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

    bool IsMobileRuntime()
    {
        if (uiManager != null)
            return uiManager.isMobile;

        return Application.isMobilePlatform;
    }

    bool IsTouchOverUI(int fingerId)
    {
        if (!ignoreWhenOverUI) return false;
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fingerId);
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
