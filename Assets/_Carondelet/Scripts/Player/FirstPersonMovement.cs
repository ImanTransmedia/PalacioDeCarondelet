using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMovement : MonoBehaviour
{
    [Header("Controles alternativos")]
    public float alternateLookSpeed = 100f;

    private AccessibilityManager accessibilityManager;
    private bool usingAlternateControls = false;

    [Header("Valores de control")]
    public InputSystem_Actions inputActions;

    Vector2 moveInput;
    Vector2 lookInput;

    [SerializeField] public float moveSpeed = 5f;
    [SerializeField, Range(0.1f, 50f)] private float acceleration = 30f;
    [SerializeField] private float mouseSensitivity = 25f;

    [Header("Camara")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [Header("Configuracion de Camara")]
    public float cameraFocusDuration = 0.5f;
    private Coroutine cameraMoveCoroutine;

    private CharacterController controller;
    private Transform cameraHolder;
    private float xRotation = 0f;

    public bool isInteracting;

    private Vector3 currentVelocity;

    void Start()
    {
        accessibilityManager = FindObjectOfType<AccessibilityManager>();
        controller = GetComponent<CharacterController>();
        cameraHolder = virtualCamera.transform;
        if (isInteracting == false)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Awake()
    {
        isInteracting = false;
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;
        inputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        if (!isInteracting)
        {
            bool currentAlt = accessibilityManager != null && accessibilityManager.enableAlternativeControls;
            if (currentAlt != usingAlternateControls)
            {
                usingAlternateControls = currentAlt;

                if (usingAlternateControls)
                {
                    xRotation = 0f;
                    cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                }
            }

            HandleMovement();

            if (usingAlternateControls)
                HandleKeyLook();
            else
                HandleMouseLook();
        }

        controller.Move(new Vector3(0, -0.1f, 0));
    }

    public void MoveCameraToTarget(Transform targetPivot)
    {
        isInteracting = true;

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
        }

        cameraMoveCoroutine = StartCoroutine(SmoothCameraMove(targetPivot));
    }

    private IEnumerator SmoothCameraMove(Transform targetPivot)
    {
        CinemachineVirtualCamera brain = virtualCamera.GetComponent<CinemachineVirtualCamera>();

        if (brain != null)
        {
            brain.enabled = false;
        }

        if (Camera.main == null) yield break;

        Transform cameraTransform = Camera.main.transform;

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        float elapsed = 0f;

        while (elapsed < cameraFocusDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraFocusDuration;

            float tSmooth = Mathf.SmoothStep(0f, 1f, t);

            cameraTransform.position = Vector3.Lerp(startPos, targetPivot.position, tSmooth);
            cameraTransform.rotation = Quaternion.Slerp(startRot, targetPivot.rotation, tSmooth);

            yield return null;
        }

        cameraTransform.position = targetPivot.position;
        cameraTransform.rotation = targetPivot.rotation;

        cameraTransform.SetParent(targetPivot);
    }

    public void ReturnCamera()
    {
        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
        }

        CinemachineVirtualCamera brain = virtualCamera.GetComponent<CinemachineVirtualCamera>();

        if (brain != null)
        {
            brain.enabled = true;
        }
        isInteracting = false;
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 desiredVelocity = moveDirection * moveSpeed;
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);
        controller.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleKeyLook()
    {
        float keyX = 0f;
        float keyY = 0f;

        if (Keyboard.current.iKey.isPressed) keyY = 1f;
        if (Keyboard.current.kKey.isPressed) keyY = -1f;
        if (Keyboard.current.jKey.isPressed) keyX = -1f;
        if (Keyboard.current.lKey.isPressed) keyX = 1f;

        float mouseX = keyX * alternateLookSpeed * Time.deltaTime;
        float mouseY = keyY * alternateLookSpeed * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void SetSensibility(float speed)
    {
        mouseSensitivity = speed;
    }
}
