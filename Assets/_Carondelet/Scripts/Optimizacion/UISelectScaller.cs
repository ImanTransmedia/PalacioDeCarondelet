using UnityEngine;
using UnityEngine.EventSystems;

public class UISelectScaller : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Escalas")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.05f;     
    [SerializeField] private float selectedScale = 1.1f;  

    [Header("Animación")]
    [SerializeField] private float scaleSpeed = 10f;
    public bool useSelected = false;

    private Vector3 targetScale;

    private bool isHovered = false;   
    private bool isSelected = false; 
    private bool isPressed = false;  

    private void Awake()
    {
        targetScale = Vector3.one * normalScale;
        transform.localScale = targetScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleSpeed
        );
    }

    private void UpdateTargetScale()
    {
        if (isPressed || isSelected)
        {
            targetScale = Vector3.one * selectedScale;
        }
        else if (isHovered)
        {
            targetScale = Vector3.one * hoverScale;
        }
        else
        {
            targetScale = Vector3.one * normalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateTargetScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateTargetScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        UpdateTargetScale();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        UpdateTargetScale();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (useSelected)
        {
            isSelected = true;
            UpdateTargetScale();
        }

    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (useSelected)
        {
            isSelected = false;
            UpdateTargetScale();
        }
    }
}
