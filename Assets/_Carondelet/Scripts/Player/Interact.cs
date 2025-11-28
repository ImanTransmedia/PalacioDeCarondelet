using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;

public class Interact : MonoBehaviour
{
    [SerializeField] private InputSystem_Actions inputActions;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f;

    [Header("Layers")]
    [SerializeField] private LayerMask layer3D;
    [SerializeField] private LayerMask layerTexture;
    [SerializeField] private LayerMask layerPainting;
    [SerializeField] private LayerMask layerDoor;
    [SerializeField] private LayerMask layerVideo;  
    [SerializeField] private LayerMask occlusionLayer;

    // --- DEBUGGING VISUAL ---
    private GameObject debugLineInstance;
    private LineRenderer occlusionDebugLine;
    // ------------------------

    [Header("Prefabs & Visuals")]
    public GameObject interactPrefab;
    public GameObject doorInteractPrefab;

    [SerializeField] private DoorNameDisplay doorNameDisplay;
    private DoorSceneLoader lastSeenDoor;

    private Transform currentTarget;
    private GameObject currentInstance;

    public FirstPersonMovement firstPerson;
    public bool wasLookingAtDoor = false;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Interact.started += OnInteractPerformed;
    }

    private void Start()
    {
        GameObject hud = GameObject.Find("HUD_Manager");
        if (hud != null)
            doorNameDisplay = hud.GetComponent<DoorNameDisplay>();
        else
            Debug.LogWarning("No se encontro el objeto 'HUD_manager'");
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.started -= OnInteractPerformed;
        inputActions.Player.Disable();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    public void TryInteract()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int combinedLayerMask = layer3D | layerTexture | layerPainting | layerDoor | layerVideo;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, combinedLayerMask))
        {
            Vector3 finalPosition;
            bool isDoor = (1 << hit.collider.gameObject.layer) == layerDoor.value;
            bool isVideo = (1 << hit.collider.gameObject.layer) == layerVideo.value;

            if (TryGetOffsetWorld(hit.collider, isDoor || isVideo, out finalPosition))
            {
                if (IsOccluded(finalPosition, hit.collider))
                {
                    //AudioManagerBDC.I.PlaySFX("InteractFail");
                    return;
                }
            }

            int hitLayerMask = 1 << hit.collider.gameObject.layer;

            if ((hitLayerMask & layer3D) != 0)
            {
                firstPerson.isInteracting = true;
                AudioManagerBDC.I.PlaySFX("Interact");
                hit.collider.GetComponent<ItemDisplay>()?.OnInteract();
            }
            else if ((hitLayerMask & layerTexture) != 0)
            {
                firstPerson.isInteracting = true;
                AudioManagerBDC.I.PlaySFX("Interact");
                hit.collider.GetComponent<textureDisplay>()?.OnInteract();
            }
            else if ((hitLayerMask & layerPainting) != 0)
            {
                firstPerson.isInteracting = true;
                AudioManagerBDC.I.PlaySFX("Interact");
                hit.collider.GetComponent<paintingDisplay>()?.OnInteract();
            }
            else if ((hitLayerMask & layerVideo) != 0)
            {
                AudioManagerBDC.I.PlaySFX("Interact");
                hit.collider.GetComponent<VideoPlayerController>()?.OnInteract();
            }
            else if ((hitLayerMask & layerDoor) != 0)
            {
                AudioManagerBDC.I.PlaySFX("OpenDoor");
                hit.collider.GetComponent<DoorSceneLoader>()?.LoadNewScene();
            }
        }
    }

    private void Update()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int combinedLayerMask = layer3D | layerTexture | layerPainting | layerDoor | layerVideo;
        bool foundVisibleTarget = false;
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, combinedLayerMask))
        {
            Vector3 finalPosition;
            bool isDoor = (1 << hit.collider.gameObject.layer) == layerDoor.value;
            bool isVideo = (1 << hit.collider.gameObject.layer) == layerVideo.value;

            if (TryGetOffsetWorld(hit.collider, isDoor, out finalPosition))
            {
                if (!IsOccluded(finalPosition, hit.collider))
                {
                    foundVisibleTarget = true;

                    wasLookingAtDoor = isDoor;

                    if (isDoor)
                    {
                        UIIngameManager.Instance.ShowInteractPrompt(true);
                        UIIngameManager.Instance.HideInteractPrompt(false);
                        DoorSceneLoader door = hit.collider.GetComponent<DoorSceneLoader>();
                        if (door != null)
                        {
                            doorNameDisplay.UpdateDoorName(door.nombreEscenario);
                            if (door != lastSeenDoor) lastSeenDoor = door;
                        }
                    }
                    else
                    {
                        UIIngameManager.Instance.ShowInteractPrompt(false);
                        UIIngameManager.Instance.HideInteractPrompt(true);
                    }

                    if (hit.transform != currentTarget)
                    {
                        DestroyCurrentInstance();
                        currentTarget = hit.transform;
                        GameObject prefabToInstantiate = isDoor ? doorInteractPrefab : interactPrefab;
                        if (prefabToInstantiate != null)
                            currentInstance = Instantiate(prefabToInstantiate);
                    }

                    if (currentInstance != null)
                    {
                        currentInstance.transform.position = finalPosition;
                    }
                }
            }
        }

        if (!foundVisibleTarget)
        {
            UIIngameManager.Instance.HideInteractPrompt(true);
            UIIngameManager.Instance.HideInteractPrompt(false);
            DestroyCurrentInstance();
            wasLookingAtDoor = false;
            if (lastSeenDoor != null)
                lastSeenDoor = null;

            //HideDebugLine(); 
        }
    }

    private LineRenderer GetDebugLine()
    {
        if (debugLineInstance == null)
        {
            debugLineInstance = new GameObject("OcclusionDebugLine");

            debugLineInstance.transform.SetParent(Camera.main.transform);

            occlusionDebugLine = debugLineInstance.AddComponent<LineRenderer>();
            occlusionDebugLine.startWidth = 0.05f; 
            occlusionDebugLine.endWidth = 0.01f; 
            occlusionDebugLine.positionCount = 2;

            occlusionDebugLine.material = new Material(Shader.Find("Sprites/Default"));

            debugLineInstance.SetActive(false);
        }

        return occlusionDebugLine;
    }

    private void HideDebugLine()
    {
        if (debugLineInstance != null)
        {
            debugLineInstance.SetActive(false);
        }
    }
    private bool IsOccluded(Vector3 targetPosition, Collider targetCollider)
    {
        //LineRenderer debugLine = GetDebugLine();
        //debugLine.gameObject.SetActive(true);

        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = (targetPosition - origin).normalized;
        float distance = Vector3.Distance(origin, targetPosition) - 0.01f;

        Color visibleColor = Color.cyan;
        Color occludedColor = Color.magenta;

        //if (distance <= 0.01f)
        //{
        //    debugLine.SetPosition(0, origin);
        //    debugLine.SetPosition(1, targetPosition);
        //    debugLine.startColor = visibleColor;
        //    debugLine.endColor = visibleColor;
        //    return false;
        //}

        RaycastHit hit;
        bool isBlocked = false;
        Vector3 rayEndPosition = targetPosition; 

        if (Physics.Raycast(origin, direction, out hit, distance, occlusionLayer))
        {
            if (hit.collider != targetCollider && !hit.transform.IsChildOf(targetCollider.transform))
            {
                isBlocked = true;
                rayEndPosition = hit.point; 

                Debug.Log($"OCLUSIÓN DETECTADA: El rayo fue bloqueado por: {hit.collider.name}. Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
        }

        //if (isBlocked)
        //{
        //    debugLine.SetPosition(0, origin);
        //    debugLine.SetPosition(1, rayEndPosition);
        //    debugLine.startColor = occludedColor;
        //    debugLine.endColor = occludedColor;
        //}
        //else
        //{
        //    debugLine.SetPosition(0, origin);
        //    debugLine.SetPosition(1, rayEndPosition);
        //    debugLine.startColor = visibleColor;
        //    debugLine.endColor = visibleColor;

        //    Debug.Log($"VISIBLE: El rayo llega a {targetCollider.name}");
        //}

        return isBlocked;
    }


    private bool TryGetOffsetWorld(Collider col, bool isDoor, out Vector3 worldPos)
    {
        worldPos = col.bounds.center;

        if (!isDoor)
        {
            if (col.TryGetComponent<VideoPlayerController>(out var video))
            {
                worldPos = video.transform.TransformPoint(video.eyeOffset);
                return true;
            }
            if (col.TryGetComponent<ItemDisplay>(out var item))
            {
                worldPos = item.transform.TransformPoint(item.eyeOffset);
                return true;
            }
            if (col.TryGetComponent<textureDisplay>(out var texture))
            {
                worldPos = texture.transform.TransformPoint(texture.eyeOffset);
                return true;
            }
            if (col.TryGetComponent<paintingDisplay>(out var painting))
            {
                worldPos = painting.transform.TransformPoint(painting.eyeOffset);
                return true;
            }
        }
        else
        {
            if (col.TryGetComponent<DoorSceneLoader>(out var door))
            {
                worldPos = col.bounds.center + door.doorIconOffset;
                return true;
            }
        }

        return false;
    }

    private void DestroyCurrentInstance()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }
        currentTarget = null;
    }

    private Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);

        foreach (Renderer rend in renderers)
            bounds.Encapsulate(rend.bounds);

        return bounds;
    }
}