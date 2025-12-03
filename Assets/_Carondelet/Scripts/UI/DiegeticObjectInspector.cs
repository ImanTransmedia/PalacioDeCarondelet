using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class DiegeticObjectInspector : MonoBehaviour
{
    [Header("Camera Inspecting configuration")]
    public Transform cameraTransform;
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float zoomDistance = 2f;

    [Header("FOV Zoom Settings")]
    public float zoomSmoothSpeed = 5f;
    public float normalZoomSmoothSpeed = 5f;
    public float inspectZoomSmoothSpeed = 3f;
    public float minNormalFOV = 40f;
    public float minInspectFOV = 20f;
    public float initialFOV = 60f;
    public float zoomSensitivity = 10f;
    public float inspectZoomSensitivity = 15f;
    public float fovTransitionDuration = 0.5f;

    private float targetFOV;
    private Camera cam;

    [Header("Diorama Movimiento con Click Derecho")]
    public float dragSpeed = 0.1f;
    public float smoothMoveSpeed = 5f;
    public float minX = -1.5f;
    public float maxX = 1.5f;
    public float minY = -1.5f;
    public float maxY = 1.5f;

    private Vector3 targetDioramaPosition;
    private Vector3 lastMousePosition;

    [Header("Configuracion entrada")]
    public float duration = 2f;
    public float speed = 0.25f;

    [Header("Diorama configuration")]
    private bool isInspecting = false;
    public GameObject diorama;

    [Header("Tooltips activos que deben apagarse al resetear")]
    public List<IntereactiveTooltip> interactiveTooltips;

    [Header("UI Panels")]
    public CanvasGroup panelAreaExterior;
    public CanvasGroup panelAreaSeleccionada;
    public float fadeDuration = 0.4f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private Vector3 initialDioramaPosition;
    private Quaternion initialDioramaRotation;
    public bool isReturning = false;

    private Transform lastInspectedTarget;

    private Vector3 inspectStartPosition;
    private Coroutine dioramaRotationCoroutine;

    [Header("Opciones de control")]
    public bool enableZoom = true;   


    void Start()
    {
       
        initialDioramaPosition = diorama.transform.position;
        initialDioramaRotation = diorama.transform.rotation;

        if (cameraTransform == null)
            return;

        cam = cameraTransform.GetComponent<Camera>();
        if (cam == null)
        {
            cam = cameraTransform.GetComponentInChildren<Camera>();
        }

        if (cam != null)
        {
            cam.fieldOfView = initialFOV;
            targetFOV = initialFOV;
        }

        initialPosition = cameraTransform.position;
        initialRotation = cameraTransform.rotation;            
    }

    
    public void InspectObject(Transform target)
    {

        if (isReturning || cameraTransform == null)
            return;



        previousPosition = cameraTransform.position;
        previousRotation = cameraTransform.rotation;

        IntereactiveTooltip inspectable = target.GetComponent<IntereactiveTooltip>();

        float zoom = (inspectable != null) ? inspectable.zoomDistance : zoomDistance;
            
        StopAllCoroutines();
        StartCoroutine(MoveToTarget(target, zoom));
        StartCoroutine(SwitchPanelsWithFade(panelAreaExterior, panelAreaSeleccionada));

        Debug.Log("Inspecting Object: " + target.name);
        Debug.Log("Last" + lastInspectedTarget);
        Debug.Log("Current" + target);
        if (isInspecting && lastInspectedTarget == target)
        {
            Debug.Log("Reset Camera");
            ResetCamera();
            lastInspectedTarget = null;
            isInspecting = false;
            return;
        }

        /*  if (cam != null)
             StartCoroutine(ChangeFOV(cam.fieldOfView, minInspectFOV, fovTransitionDuration)); */

        isInspecting = true;

        if (inspectable != null && diorama != null)
        {
            if (dioramaRotationCoroutine != null)
                StopCoroutine(dioramaRotationCoroutine);
            dioramaRotationCoroutine = StartCoroutine(
                SmoothRotateDiorama(inspectable.dioramaTargetRotation, 1f)
            );
        }
        lastInspectedTarget = target;
    }

    public void ResetCamera()
    {
        if (cameraTransform == null || diorama == null)
            return;
        StartCoroutine(SwitchPanelsWithFade(panelAreaSeleccionada, panelAreaExterior));
        StopAllCoroutines();

        StartCoroutine(SmoothResetCameraAndDiorama());
        lastInspectedTarget = null;
        if (cam != null)
            StartCoroutine(ChangeFOV(cam.fieldOfView, initialFOV, fovTransitionDuration));

        isInspecting = false;
        foreach (IntereactiveTooltip tooltip in interactiveTooltips)
        {
            if (tooltip != null)
                tooltip.state = false;
        }

        gameObject.GetComponent<ToolTipOrchestrator>().ResetAll();

    }

    private IEnumerator MoveToTarget(Transform target, float zoomDistance)
    {
        if (cameraTransform == null)
            yield break;

        Vector3 targetPosition = target.position - target.forward * zoomDistance;
        Quaternion lookRotation = Quaternion.LookRotation(target.position - targetPosition);
        Vector3 targetEuler = lookRotation.eulerAngles;
        targetEuler.y = 0f;
        Quaternion targetRotation = Quaternion.Euler(targetEuler);

        while (
            Vector3.Distance(cameraTransform.position, targetPosition) > 0.01f
            || Quaternion.Angle(cameraTransform.rotation, targetRotation) > 0.1f
        )
        {
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
            yield return null;
        }

        cameraTransform.position = targetPosition;
        cameraTransform.rotation = targetRotation;
        inspectStartPosition = cameraTransform.position;
    }

    private IEnumerator ReturnToStart()
    {
        if (cameraTransform == null)
            yield break;

        isReturning = true;

        Vector3 start = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        float elapsed = 0f;
        float totalDuration = 1f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / totalDuration);
            cameraTransform.position = Vector3.Lerp(start, previousPosition, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, previousRotation, t);
            yield return null;
        }

        cameraTransform.position = previousPosition;
        cameraTransform.rotation = previousRotation;
        isReturning = false;
        isInspecting = false;
    }

    private IEnumerator SwitchPanelsWithFade(CanvasGroup fadeOutPanel, CanvasGroup fadeInPanel)
    {

        yield return StartCoroutine(FadeOut(fadeOutPanel));
        fadeOutPanel.gameObject.SetActive(false);

        fadeInPanel.gameObject.SetActive(true);
        yield return StartCoroutine(FadeIn(fadeInPanel));

    }

    private IEnumerator FadeIn(CanvasGroup canvasGroup)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }


    private IEnumerator ChangeFOV(float startFOV, float endFOV, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(startFOV, endFOV, elapsed / duration);
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        if (cam != null)
            cam.fieldOfView = endFOV;
    }

    public void MoveCameraForward()
    {
        if (cameraTransform == null)
            return;
        StopAllCoroutines();
        StartCoroutine(MoveCameraForwardCoroutine(duration, speed));
    }

    private IEnumerator MoveCameraForwardCoroutine(float duration, float speed)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            cameraTransform.position += cameraTransform.forward * speed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void Update()
    {
        HandleMouseZoom();
        HandleTouchZoom();
        HandleRightClickMovement();
    }



    void HandleMouseZoom()
    {
        if (!enableZoom || cam == null)   // <--- chequeo
            return;
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            if (isInspecting)
            {
                targetFOV -= scrollInput * inspectZoomSensitivity;
                targetFOV = Mathf.Clamp(targetFOV, minInspectFOV, initialFOV);
            }
            else
            {
                targetFOV -= scrollInput * zoomSensitivity;
                targetFOV = Mathf.Clamp(targetFOV, minNormalFOV, initialFOV);
            }
        }

        float smoothSpeed = isInspecting ? inspectZoomSmoothSpeed : normalZoomSmoothSpeed;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, smoothSpeed * Time.deltaTime);
    }

    void HandleRightClickMovement()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
            targetDioramaPosition = diorama.transform.position;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            delta *= dragSpeed * Time.deltaTime;

            targetDioramaPosition += new Vector3(delta.x, delta.y, 0f);

            targetDioramaPosition.x = Mathf.Clamp(targetDioramaPosition.x, minX, maxX);
            targetDioramaPosition.y = Mathf.Clamp(targetDioramaPosition.y, minY, maxY);

            diorama.transform.position = Vector3.Lerp(
                diorama.transform.position,
                targetDioramaPosition,
                smoothMoveSpeed * Time.deltaTime
            );
            lastMousePosition = Input.mousePosition;
        }
    }

    private IEnumerator SmoothRotateDiorama(Vector3 targetEulerAngles, float duration = 1f)
    {
        Quaternion startRotation = diorama.transform.rotation;
        Quaternion endRotation = Quaternion.Euler(targetEulerAngles);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            diorama.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }
        diorama.transform.rotation = endRotation;
    }

  private IEnumerator SmoothResetCameraAndDiorama()
{
    isReturning = true;

    Vector3 startPos = cameraTransform.position;
    Quaternion startRot = cameraTransform.rotation;
    Vector3 endPos = initialPosition;
    Quaternion endRot = initialRotation;
    
    Vector3 dioramaStartPos = diorama.transform.position;
    Quaternion dioramaStartRot = diorama.transform.rotation;
    Vector3 dioramaEndPos = initialDioramaPosition;
    Quaternion dioramaEndRot = initialDioramaRotation;

    float elapsed = 0f;
    float duration = 1f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

        cameraTransform.position = Vector3.Lerp(startPos, endPos, t);
        cameraTransform.rotation = Quaternion.Slerp(startRot, endRot, t);


        diorama.transform.position = Vector3.Lerp(dioramaStartPos, dioramaEndPos, t);
        diorama.transform.rotation = Quaternion.Slerp(dioramaStartRot, dioramaEndRot, t);
        yield return null;
    }


    cameraTransform.rotation = endRot;
    diorama.transform.position = dioramaEndPos;
    diorama.transform.rotation = dioramaEndRot;

        isReturning = false;
}

void HandleTouchZoom()
{
        if (!enableZoom || cam == null)   // <--- chequeo
            return;

        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentTouchDeltaMag = (touchZero.position - touchOne.position).magnitude;
            float deltaMagnitudeDiff = prevTouchDeltaMag - currentTouchDeltaMag;

            float sensitivity = isInspecting ? inspectZoomSensitivity * 0.7f : zoomSensitivity * 0.2f;
            float minFOV = isInspecting ? minInspectFOV : minNormalFOV;

            targetFOV -= deltaMagnitudeDiff * sensitivity; // <--- CORREGIDO
            targetFOV = Mathf.Clamp(targetFOV, minFOV, initialFOV);

            float smoothSpeed = isInspecting ? inspectZoomSmoothSpeed : normalZoomSmoothSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, smoothSpeed * Time.deltaTime);
    }
}
}
