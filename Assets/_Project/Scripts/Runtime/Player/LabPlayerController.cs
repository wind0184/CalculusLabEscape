using UnityEngine;
using UnityEngine.InputSystem;

namespace CalculusLabEscape
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class LabPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float lookSpeed = 2.2f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        [SerializeField] private Camera playerCamera;
        private float pitch;
        private float verticalVelocity;

        public bool ControlsEnabled { get; set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if(playerCamera==null)playerCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (!ControlsEnabled || playerCamera == null || ScreenTransition.IsBusy)
                return;

            Vector2 mouseDelta = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();
            float mouseX = mouseDelta.x * lookSpeed * 0.05f;
            float mouseY = mouseDelta.y * lookSpeed * 0.05f;
            transform.Rotate(Vector3.up * mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
            playerCamera.transform.localEulerAngles = Vector3.right * pitch;

            float horizontal = 0f;
            float forward = 0f;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) forward += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) forward -= 1f;
            }

            Vector3 movement = transform.right * horizontal + transform.forward * forward;
            movement = Vector3.ClampMagnitude(movement, 1f) * moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            movement.y = verticalVelocity;
            controller.Move(movement * Time.deltaTime);
        }
    }
}
