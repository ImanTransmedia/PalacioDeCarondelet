using UnityEngine;
using UnityEngine.EventSystems;

public class PinchZoom : MonoBehaviour
{
    private float zoomScale = 2f;
    private float zoomSpeed = 8f;
    public float scrollSensitivity = 0.1f;
    public bool enablePCZoom = false; 

    private float initialTouchDistance = -1f;

    private RectTransform rect;
    private Vector3 originalScale;
    private Vector2 originalPosition;
    private int originalSiblingIndex;

    private float currentScale = 1f;
    private bool isZoomed = false;
    private bool releasedAtOriginalSize = false;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        originalScale = rect.localScale;
        originalPosition = rect.anchoredPosition;
    }

void Update()
{
    HandleTouchZoom();

    bool zoomedNow = !IsScaleApproximatelyEqual(rect.localScale, originalScale);

    if (zoomedNow && !isZoomed)
    {
        rect.SetAsLastSibling();
        isZoomed = true;
    }
    else if (!zoomedNow && isZoomed)
    {
        rect.SetSiblingIndex(originalSiblingIndex);
        isZoomed = false;
    }

    if (releasedAtOriginalSize && !isZoomed)
    {
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, originalPosition, Time.deltaTime * zoomSpeed);
    }
}

    void HandleTouchZoom()
{
    if (Input.touchCount == 2)
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (!RectTransformUtility.RectangleContainsScreenPoint(rect, touch0.position) ||
            !RectTransformUtility.RectangleContainsScreenPoint(rect, touch1.position))
            return;

        float currentDistance = Vector2.Distance(touch0.position, touch1.position);

        if (initialTouchDistance < 0f)
        {
            initialTouchDistance = currentDistance; 
            return; 
        }

        float delta = currentDistance - initialTouchDistance;
        float scaleDelta = delta * 0.005f;

        if (Mathf.Abs(scaleDelta) > 0.001f) 
        {
            currentScale = Mathf.Clamp(currentScale + scaleDelta, 1f, zoomScale);
            rect.localScale = Vector3.Lerp(rect.localScale, originalScale * currentScale, Time.deltaTime * zoomSpeed);

            Vector2 midpoint = (touch0.position + touch1.position) / 2f;
            MoveRectToScreenPoint(midpoint);
        }

        releasedAtOriginalSize = false;
    }
    else
    {
        initialTouchDistance = -1f; 

        if (!IsScaleApproximatelyEqual(rect.localScale, originalScale, 0.01f))
        {
            releasedAtOriginalSize = false;
        }
        else
        {
            releasedAtOriginalSize = true;
        }
    }
}

    void HandleMouseZoom()
    {
        if (!IsMouseOverUI(rect)) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float scaleDelta = scroll * scrollSensitivity;
            currentScale = Mathf.Clamp(currentScale + scaleDelta, 1f, zoomScale);
            rect.localScale = Vector3.Lerp(rect.localScale, originalScale * currentScale, Time.deltaTime * zoomSpeed);
        }
    }

    bool IsMouseOverUI(RectTransform target)
    {
        Vector2 mousePosition = Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(target, mousePosition, null);
    }

    bool IsScaleApproximatelyEqual(Vector3 a, Vector3 b, float epsilon = 0.06f)
    {
        return Mathf.Abs(a.x - b.x) < epsilon &&
               Mathf.Abs(a.y - b.y) < epsilon &&
               Mathf.Abs(a.z - b.z) < epsilon;
    }

    void OnEnable()
    {
    rect ??= GetComponent<RectTransform>(); 
    originalScale = rect.localScale;
    originalPosition = rect.anchoredPosition;
    originalSiblingIndex = rect.GetSiblingIndex();
        ResetZoom();
    }

    void OnDisable()
    {
        ResetZoom();
    }

    private void ResetZoom()
    {
        currentScale = 1f;
        rect.localScale = originalScale;
        rect.anchoredPosition = originalPosition;
        rect.SetSiblingIndex(originalSiblingIndex);
        isZoomed = false;
        releasedAtOriginalSize = false;
    }

    private void MoveRectToScreenPoint(Vector2 screenPoint)
    {
        RectTransform parentRect = rect.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, null, out Vector2 localPoint))
        {
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, localPoint, Time.deltaTime * zoomSpeed);
        }
    }
}
