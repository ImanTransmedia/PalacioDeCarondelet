using UnityEngine;

public class CameraLookAtManager : MonoBehaviour
{
    [Header("Camera reference")]
    public Transform cameraTransform;

    [Header("LookAt Targets")]
    public Transform[] lookAtTargets;

    [Header("LookAt Settings")]
    public float rotationSpeed = 5f;

    private Transform currentTarget;

    void Update()
    {
        if (cameraTransform != null && currentTarget != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentTarget.position - cameraTransform.position);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void SetLookAtTarget(int index)
    {
        if (lookAtTargets == null || index < 0 || index >= lookAtTargets.Length)
        {
            Debug.LogWarning("Índice inválido para SetLookAtTarget.");
            return;
        }
        currentTarget = lookAtTargets[index];
    }
}
