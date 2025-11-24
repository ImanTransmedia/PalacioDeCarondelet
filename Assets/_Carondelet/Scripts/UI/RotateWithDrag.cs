using UnityEngine;
using UnityEngine.EventSystems;

public class RotateWithDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float force = 10f;
    private Rigidbody rb;
    private bool isDragging = false;
    private Vector2 dragDelta;

    [Header("Rotation Limits")]
    public bool limitX = true;
    public float minX = -30f;
    public float maxX = 30f;

    public bool limitY = true;
    public float minY = -60f;
    public float maxY = 60f;

    [Header("Smooth Rotation")]
    public float rotationSmoothness = 5f; 
    private Quaternion targetRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        targetRotation = transform.localRotation;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        isDragging = false;
        dragDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        dragDelta = eventData.delta;
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            Vector3 torque = new Vector3(dragDelta.y, -dragDelta.x, 0f);
            rb.AddTorque(torque * force * Time.fixedDeltaTime);
        }

        ClampRotation();
    }

    void ClampRotation()
    {
        Vector3 currentRotation = transform.localEulerAngles;
        currentRotation.x = NormalizeAngle(currentRotation.x);
        currentRotation.y = NormalizeAngle(currentRotation.y);

        if (limitX)
            currentRotation.x = Mathf.Clamp(currentRotation.x, minX, maxX);

        if (limitY)
            currentRotation.y = Mathf.Clamp(currentRotation.y, minY, maxY);

        targetRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationSmoothness * Time.fixedDeltaTime);

        rb.angularVelocity = Vector3.zero;
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
