using UnityEngine;

public class SkyboxPerspectiveSwitcher : MonoBehaviour
{
    [Header("Player")]
    public Transform playerTransform;

    [Header("Skybox Materials")]
    public Material skyboxMatLeft;
    public Material skyboxMatCenter;
    public Material skyboxMatRight;

    [Header("Transition Zones (X-axis)")]
    public float leftZoneEndX = -17f;
    public float centerZoneStartX = -17f;
    public float centerZoneEndX = 13f; 
    public float rightZoneStartX = 13f;

    [Header("Transition Settings")]
    [Tooltip("Ancho de la zona de transición entre skyboxes (ej: 2 unidades)")]
    public float transitionWidth = 2.0f;
    [Range(0.01f, 10f)]
    public float transitionSpeed = 2.0f;

    private Material currentSkyboxInstance;
    private float currentBlendFactorForLerp = 0f;

    void Start()
    {
        if (!playerTransform || !skyboxMatLeft || !skyboxMatCenter || !skyboxMatRight)
        {
            Debug.LogError("Asigna todos los Transforms y Materiales Skybox en el Inspector.");
            enabled = false;
            return;
        }

        // Validar rangos para evitar lógica incorrecta
        if (centerZoneStartX >= centerZoneEndX)
        {
            Debug.LogError("Center Zone Start X debe ser menor que Center Zone End X.");
            enabled = false;
            return;
        }
        if (transitionWidth <= 0)
        {
            Debug.LogWarning("Transition Width debería ser mayor que cero para un blend visible.");
            transitionWidth = 0.1f; // Un valor mínimo pequeño
        }


        // Determinar el skybox inicial basado en la posición del jugador
        // y crear la instancia para Lerp
        float initialPlayerX = playerTransform.position.x;
        Material initialFromMaterial = skyboxMatLeft; // Default
        Material initialToMaterial = skyboxMatCenter; // Default
        float initialBlend = 0f;

        // Puntos de inicio de las transiciones
        float transLeftToCenterStart = leftZoneEndX - (transitionWidth / 2f);
        float transLeftToCenterEnd = leftZoneEndX + (transitionWidth / 2f); // = centerZoneStartX + (transitionWidth / 2f)

        float transCenterToRightStart = centerZoneEndX - (transitionWidth / 2f);
        float transCenterToRightEnd = centerZoneEndX + (transitionWidth / 2f); // = rightZoneStartX + (transitionWidth / 2f)


        if (initialPlayerX < transLeftToCenterStart) // Totalmente a la izquierda
        {
            initialFromMaterial = skyboxMatLeft;
            initialToMaterial = skyboxMatLeft; // Lerp con sigo mismo es igual al original
            initialBlend = 0f; // o 1f, da igual si from y to son iguales
        }
        else if (initialPlayerX >= transLeftToCenterStart && initialPlayerX <= transLeftToCenterEnd) // Transición Izquierda -> Centro
        {
            initialFromMaterial = skyboxMatLeft;
            initialToMaterial = skyboxMatCenter;
            initialBlend = Mathf.InverseLerp(transLeftToCenterStart, transLeftToCenterEnd, initialPlayerX);
        }
        else if (initialPlayerX > transLeftToCenterEnd && initialPlayerX < transCenterToRightStart) // Totalmente en el Centro
        {
            initialFromMaterial = skyboxMatCenter;
            initialToMaterial = skyboxMatCenter;
            initialBlend = 0f;
        }
        else if (initialPlayerX >= transCenterToRightStart && initialPlayerX <= transCenterToRightEnd) // Transición Centro -> Derecha
        {
            initialFromMaterial = skyboxMatCenter;
            initialToMaterial = skyboxMatRight;
            initialBlend = Mathf.InverseLerp(transCenterToRightStart, transCenterToRightEnd, initialPlayerX);
        }
        else // Totalmente a la Derecha (initialPlayerX > transCenterToRightEnd)
        {
            initialFromMaterial = skyboxMatRight;
            initialToMaterial = skyboxMatRight;
            initialBlend = 0f;
        }

        currentSkyboxInstance = new Material(initialFromMaterial); // Instanciar basado en el 'from'
        currentSkyboxInstance.Lerp(initialFromMaterial, initialToMaterial, initialBlend); // Aplicar el blend inicial
        RenderSettings.skybox = currentSkyboxInstance;
        currentBlendFactorForLerp = initialBlend; // Guardar el estado de blend inicial

        Debug.Log($"Initial Skybox Setup: PlayerX={initialPlayerX}, From={initialFromMaterial.name}, To={initialToMaterial.name}, Blend={initialBlend}");
    }

    void OnDestroy()
    {
        if (currentSkyboxInstance != null)
        {
            Destroy(currentSkyboxInstance);
        }
    }

    void Update()
    {
        if (!playerTransform) return;

        float playerX = playerTransform.position.x;

        float transLeftToCenter_Start = leftZoneEndX - (transitionWidth / 2f);
        float transLeftToCenter_End   = leftZoneEndX + (transitionWidth / 2f);

        float transCenterToRight_Start = centerZoneEndX - (transitionWidth / 2f);
        float transCenterToRight_End   = centerZoneEndX + (transitionWidth / 2f);


        Material fromMaterial;
        Material toMaterial;
        float targetBlendThisFrame;

        if (playerX < transLeftToCenter_Start)
        {
            fromMaterial = skyboxMatLeft;
            toMaterial = skyboxMatLeft;
            targetBlendThisFrame = 0f;
        }
        else if (playerX >= transLeftToCenter_Start && playerX <= transLeftToCenter_End) // Transición Izquierda <-> Centro
        {
            fromMaterial = skyboxMatLeft;
            toMaterial = skyboxMatCenter;
            targetBlendThisFrame = Mathf.InverseLerp(transLeftToCenter_Start, transLeftToCenter_End, playerX);
        }
        else if (playerX > transLeftToCenter_End && playerX < transCenterToRight_Start) // Completamente en la zona central
        {
            fromMaterial = skyboxMatCenter;
            toMaterial = skyboxMatCenter;
            targetBlendThisFrame = 0f;
        }
        else if (playerX >= transCenterToRight_Start && playerX <= transCenterToRight_End) // Transición Centro <-> Derecha
        {
            fromMaterial = skyboxMatCenter;
            toMaterial = skyboxMatRight;
            targetBlendThisFrame = Mathf.InverseLerp(transCenterToRight_Start, transCenterToRight_End, playerX);
        }
        else
        {
            fromMaterial = skyboxMatRight;
            toMaterial = skyboxMatRight;
            targetBlendThisFrame = 0f;
        }

        currentBlendFactorForLerp = Mathf.Lerp(currentBlendFactorForLerp, targetBlendThisFrame, Time.deltaTime * transitionSpeed);

        currentSkyboxInstance.Lerp(fromMaterial, toMaterial, currentBlendFactorForLerp);
    }
}