using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;

    [Header("Scale Settings")]
    public bool staticSize = false;
    public float referenceDistance = 10f; 
    private Vector3 initialScale;
    private float initialDistance;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        initialScale = transform.localScale;

    
        initialDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
        if (initialDistance == 0f) initialDistance = referenceDistance;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
           
            transform.LookAt(transform.position + mainCamera.transform.forward);

            if (staticSize)
            {
                float currentDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
                float scaleFactor = currentDistance / initialDistance;

              
                transform.localScale = new Vector3(
                    initialScale.x * scaleFactor,
                    initialScale.y * scaleFactor,
                    initialScale.z
                );
            }
        }
    }
}