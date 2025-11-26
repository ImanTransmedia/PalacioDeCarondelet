using UnityEngine;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {
        [Header("Output")]
        public FirstPersonMovement playerMovement;

        private void Awake()
        {
            if (playerMovement == null)
            {
                playerMovement = FindAnyObjectByType<FirstPersonMovement>();
            }
        }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if (playerMovement == null) return;
            playerMovement.SetMoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            if (playerMovement == null) return;
            playerMovement.SetLookInput(virtualLookDirection);
        }
    }
}
