using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemDetector : MonoBehaviour
{
    [Header("Configuracion")]
    public Collider triggerCollider;
    public LayerMask detectionLayers;
    public GameObject displayPrefab;

    [Header("Oclusion")]
    public LayerMask occlusionLayers;
    [Range(0f, 0.5f)] public float wallDelta = 0.05f;
    public Transform rayOrigin;

    [Header("Debug")]
    public bool debugMode = true;
    public Color visibleColor = Color.green;
    public Color blockedColor = Color.red;

    public bool useSphereCast = false;
    [Range(0f, 0.5f)] public float sphereRadius = 0.5f;

    private Dictionary<Transform, GameObject> activeDisplays = new Dictionary<Transform, GameObject>();
    private Dictionary<Transform, Coroutine> fadeCoroutines = new Dictionary<Transform, Coroutine>();

    private void Start()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (rayOrigin == null && Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, detectionLayers))
            return;

        Transform eyeOffsetTransform = GetEyeOffset(other.gameObject, out Vector3 offsetValue);
        if (eyeOffsetTransform == null) return;

        Vector3 finalPosition = eyeOffsetTransform.TransformPoint(offsetValue);

        if (IsOccluded(finalPosition, other))
            return;

        if (activeDisplays.ContainsKey(eyeOffsetTransform))
        {
            if (fadeCoroutines.TryGetValue(eyeOffsetTransform, out Coroutine oldCoroutine))
                StopCoroutine(oldCoroutine);

            CanvasGroup cg = activeDisplays[eyeOffsetTransform].GetComponentInChildren<CanvasGroup>();
            if (cg != null)
                fadeCoroutines[eyeOffsetTransform] = StartCoroutine(FadeCanvasGroup(cg, 1f, 0.2f));
        }
        else
        {
            GameObject instance = Instantiate(displayPrefab, finalPosition, Quaternion.identity);

            CanvasGroup cg = instance.GetComponentInChildren<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                fadeCoroutines[eyeOffsetTransform] = StartCoroutine(FadeCanvasGroup(cg, 1f, 0.2f));
            }

            activeDisplays.Add(eyeOffsetTransform, instance);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, detectionLayers))
            return;

        Transform eyeOffsetTransform = GetEyeOffset(other.gameObject, out Vector3 offsetValue);
        if (eyeOffsetTransform == null) return;

        Vector3 finalPosition = eyeOffsetTransform.TransformPoint(offsetValue);

        bool occluded = IsOccluded(finalPosition, other);

        bool displayIsActive = activeDisplays.TryGetValue(eyeOffsetTransform, out GameObject instance);


        if (occluded && displayIsActive)
        {
            if (fadeCoroutines.TryGetValue(eyeOffsetTransform, out Coroutine oldCoroutine))
                StopCoroutine(oldCoroutine);

            activeDisplays.Remove(eyeOffsetTransform);
            fadeCoroutines.Remove(eyeOffsetTransform);

            if (instance != null)
            {
                Destroy(instance);
            }
        }
        else if (!occluded && !displayIsActive)
        {
            GameObject newInstance = Instantiate(displayPrefab, finalPosition, Quaternion.identity);
            CanvasGroup cg = newInstance.GetComponentInChildren<CanvasGroup>();

            if (cg != null)
            {
                cg.alpha = 0f;
                fadeCoroutines[eyeOffsetTransform] = StartCoroutine(FadeCanvasGroup(cg, 1f, 0.2f));
            }

            activeDisplays.Add(eyeOffsetTransform, newInstance);
        }
        else if (!occluded && displayIsActive)
        {
            if (instance != null)
            {
                instance.transform.position = finalPosition;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        Transform eyeOffset = GetEyeOffset(other.gameObject, out _);

        if (eyeOffset != null && activeDisplays.ContainsKey(eyeOffset))
        {
            GameObject instance = activeDisplays[eyeOffset];
            CanvasGroup cg = instance.GetComponentInChildren<CanvasGroup>();

            if (cg != null)
            {
                if (fadeCoroutines.TryGetValue(eyeOffset, out Coroutine oldCoroutine))
                    StopCoroutine(oldCoroutine);

                fadeCoroutines[eyeOffset] = StartCoroutine(FadeAndDestroy(eyeOffset, cg, 0f, 0.05f));
            }
            else
            {
                Destroy(instance);
                activeDisplays.Remove(eyeOffset);
            }
        }
    }

    private bool IsOccluded(Vector3 targetPoint, Collider targetCollider)
    {
        Vector3 origin = GetRayOrigin();
        Vector3 toTarget = targetPoint - origin;
        float dist = toTarget.magnitude;
        if (dist <= 0.0001f) return false;

        Vector3 dir = toTarget / dist;
        origin += dir * 0.01f;
        dist = Mathf.Max(0f, dist - 0.01f);

        bool blocked = false;
        bool hitSomething = false;
        RaycastHit hit;

        if (useSphereCast)
            hitSomething = Physics.SphereCast(origin, sphereRadius, dir, out hit, dist, occlusionLayers, QueryTriggerInteraction.Ignore);
        else
            hitSomething = Physics.Raycast(origin, dir, out hit, dist, occlusionLayers, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            if (!IsSameHierarchy(hit.transform, targetCollider.transform) && !IsSameHierarchy(hit.transform, transform))
            {
                if (hit.distance < dist - wallDelta)
                    blocked = true;
            }
        }

        if (debugMode)
        {
            Color c = blocked ? blockedColor : visibleColor;
            Debug.DrawRay(origin, dir * dist, c, 0.1f);
        }

        return blocked;
    }

    private Vector3 GetRayOrigin()
    {
        if (rayOrigin != null) return rayOrigin.position;
        if (triggerCollider != null) return triggerCollider.bounds.center;
        return transform.position;
    }

    private static bool IsSameHierarchy(Transform a, Transform b)
    {
        if (a == null || b == null) return false;
        return a == b || a.IsChildOf(b) || b.IsChildOf(a);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
    {
        float startAlpha = cg.alpha;
        float time = 0f;

        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        cg.alpha = targetAlpha;
    }

    private IEnumerator FadeAndDestroy(Transform key, CanvasGroup cg, float targetAlpha, float duration)
    {
        yield return FadeCanvasGroup(cg, targetAlpha, duration);

        if (activeDisplays.TryGetValue(key, out GameObject obj))
        {
            Destroy(obj);
            activeDisplays.Remove(key);
            fadeCoroutines.Remove(key);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((mask.value & (1 << layer)) != 0);
    }

    private Transform GetEyeOffset(GameObject obj, out Vector3 offset)
    {
        offset = Vector3.zero;

        if (obj.TryGetComponent<VideoPlayerController>(out var videoController))
        {
            offset = videoController.eyeOffset;
            return videoController.transform;
        } 
        if (obj.TryGetComponent<ItemDisplay>(out var item))
        {
            offset = item.eyeOffset;
            return item.transform;
        }
        if (obj.TryGetComponent<paintingDisplay>(out var painting))
        {
            offset = painting.eyeOffset;
            return painting.transform;
        }
        if (obj.TryGetComponent<textureDisplay>(out var texture))
        {
            offset = texture.eyeOffset;
            return texture.transform;
        }

        return null;
    }

    private Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);

        foreach (Renderer rend in renderers)
        {
            bounds.Encapsulate(rend.bounds);
        }

        return bounds;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        foreach (var kvp in activeDisplays)
        {
            if (kvp.Key == null) continue;

            Vector3 offsetPos = Vector3.zero;
            bool foundOffset = false;

            if (kvp.Key.TryGetComponent<VideoPlayerController>(out var videoController))
            {
                offsetPos = kvp.Key.TransformPoint(videoController.eyeOffset);
                foundOffset = true;
            }
            else if (kvp.Key.TryGetComponent<ItemDisplay>(out var item))
                offsetPos = kvp.Key.TransformPoint(item.eyeOffset);
            else if (kvp.Key.TryGetComponent<paintingDisplay>(out var painting))
                offsetPos = kvp.Key.TransformPoint(painting.eyeOffset);
            else if (kvp.Key.TryGetComponent<textureDisplay>(out var texture))
                offsetPos = kvp.Key.TransformPoint(texture.eyeOffset);

            if (foundOffset)
                Gizmos.DrawSphere(offsetPos, 0.02f);
        }
    
    }
#endif

}
