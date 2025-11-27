using UnityEngine;

public class JoystickHidder : MonoBehaviour
{
    public GameObject joystick;

    public void Hide()
    {
        joystick.SetActive(false);
    }
}
