using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiegeticObjectInspector : MonoBehaviour
{
    [Header("Camera Inspecting configuration")]
    public Transform cameraTransform;
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float zoomDistance = 2f;

    private Camera cam;
    private float initialFovStored;

    public Camera Cam => cam;
    public bool IsInspecting => isInspecting;
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
            initialFovStored = cam.fieldOfView;
        }

        initialPosition = cameraTransform.position;
        initialRotation = cameraTransform.rotation;
    }

    public void InspectObject(Transform target)
    {
        if (isReturning || cameraTransform == null)
            return;

        if (isInspecting && lastInspectedTarget == target)
        {
            Debug.Log("Reset Camera");
            ResetCamera();
            return;
        }

        previousPosition = cameraTransform.position;
        previousRotation = cameraTransform.rotation;

        IntereactiveTooltip inspectable = target.GetComponent<IntereactiveTooltip>();
        float zoom = (inspectable != null) ? inspectable.zoomDistance : zoomDistance;

        StopAllCoroutines();
        StartCoroutine(MoveToTarget(target, zoom));
        StartCoroutine(SwitchPanelsWithFade(panelAreaExterior, panelAreaSeleccionada));

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

        StopAllCoroutines();

        StartCoroutine(SwitchPanelsWithFade(panelAreaSeleccionada, panelAreaExterior));
        StartCoroutine(SmoothResetCameraAndDiorama());

        lastInspectedTarget = null;

        if (cam != null)
            cam.fieldOfView = initialFovStored;

        isInspecting = false;

        foreach (IntereactiveTooltip tooltip in interactiveTooltips)
        {
            if (tooltip != null)
                tooltip.state = false;
        }
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

    public float InitialFOV => initialFovStored;

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
}
