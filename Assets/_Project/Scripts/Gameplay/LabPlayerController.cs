using UnityEngine;

namespace CalculusLabEscape
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class LabPlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float lookSpeed = 2.2f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private Camera playerCamera;
        private float pitch;
        private float verticalVelocity;

        public bool ControlsEnabled { get; set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            if (!ControlsEnabled || playerCamera == null)
                return;

            float mouseX = Input.GetAxisRaw("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxisRaw("Mouse Y") * lookSpeed;
            transform.Rotate(Vector3.up * mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
            playerCamera.transform.localEulerAngles = Vector3.right * pitch;

            float horizontal = 0f;
            float forward = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) forward += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) forward -= 1f;

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
