using UnityEngine;

public class PLayerLlimter : MonoBehaviour
{

    private FirstPersonMovement firstPersonMovementScript;

    void Start()
    {
        firstPersonMovementScript = GetComponent<FirstPersonMovement>();

        if (firstPersonMovementScript == null)
        {
            firstPersonMovementScript = GetComponentInChildren<FirstPersonMovement>();

            if (firstPersonMovementScript == null)
            {
                Debug.LogError("PlayerMovementLimiter: No se encontró el componente FirstPersonMovement en este GameObject ni en sus hijos.");
            }
        }
    }

    public void RestrictMovement()
    {
        if (firstPersonMovementScript != null && firstPersonMovementScript.enabled)
        {
            firstPersonMovementScript.enabled = false;

            Debug.Log("Movimiento del jugador restringido.");
        }
    }


    public void AllowMovement()
    {
        if (firstPersonMovementScript != null && !firstPersonMovementScript.enabled)
        {
            firstPersonMovementScript.enabled = true;

            Debug.Log("Movimiento del jugador permitido.");
        }
    }


    void Update()
    {
        // Presiona la tecla 'P' para restringir el movimiento
        if (Input.GetKeyDown(KeyCode.J))
        {
            RestrictMovement();
        }

        // Presiona la tecla 'O' para permitir el movimiento
        if (Input.GetKeyDown(KeyCode.O))
        {
            AllowMovement();
        }
    }
    
}