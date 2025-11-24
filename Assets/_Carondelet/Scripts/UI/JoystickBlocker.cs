using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class JoystickBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private FirstPersonMovement firstPersonMovement;
    [SerializeField] private float LookSpeed = 0.7f; 
    [SerializeField] private float oldLookSpeed = 0.1f;
    [SerializeField] private bool isRightJoystick = false; 
    private Canvas _canvas;

    private void OnEnable()
    {
        if (firstPersonMovement == null)
        {
            GameObject mainPlayer = GameObject.Find("Main Player");
            if (mainPlayer != null)
            {
                firstPersonMovement = mainPlayer.GetComponent<FirstPersonMovement>();
                if (firstPersonMovement == null)
                    Debug.LogError("JoystickBlocker: 'Main Player' no tiene FirstPersonMovement.");
            }
            else
            {
                Debug.LogError("JoystickBlocker: no se encontró ningún GameObject llamado 'Main Player' en la escena.");
            }
        }
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        EnableJoystickMode();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EnablePointerMode();
    }

    public void EnableJoystickMode()
    {
        firstPersonMovement.inputActions.bindingMask = InputBinding.MaskByGroup("Gamepad");
        Debug.Log("Modo Joystick activado.");
        if (isRightJoystick)
        {
            firstPersonMovement.SetSensibility(LookSpeed);
            Debug.Log("Cambiando la velocidad: " + LookSpeed);
        }
        
    }

    public void EnablePointerMode()
    {
        firstPersonMovement.inputActions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
        Debug.Log("Modo Pointer/K&M activado.");
        if (isRightJoystick)
        {
            firstPersonMovement.SetSensibility(oldLookSpeed);
            Debug.Log("Cambiando la velocidad: " + oldLookSpeed);
        }

    }
}
