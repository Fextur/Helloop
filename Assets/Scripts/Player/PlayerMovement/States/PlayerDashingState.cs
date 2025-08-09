using UnityEngine;
using Helloop.StateMachines;

namespace Helloop.Player.States
{
    public class PlayerDashingState : IState<PlayerMovement>
    {
        private float dashTimer;
        private Vector3 dashDirection;

        public void OnEnter(PlayerMovement player)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 inputDirection = (player.FPSCamera.transform.forward * v + player.FPSCamera.transform.right * h).normalized;

            if (inputDirection.magnitude < 0.1f)
            {
                inputDirection = player.FPSCamera.transform.forward;
            }

            inputDirection.y = 0f;
            inputDirection.Normalize();

            dashTimer = player.dashDuration;
            dashDirection = inputDirection;
            player.LastDashTime = Time.time;
            player.CurrentStamina -= player.dashStaminaCost;

            player.IsDashing = true;
        }

        public void Update(PlayerMovement player)
        {
            HandleLook(player);
            HandleDashMovement(player);
            HandleGravity(player);

            if (dashTimer <= 0f)
            {
                player.IsDashing = false;
            }
        }

        public void OnExit(PlayerMovement player)
        {
            player.IsDashing = false;
            dashTimer = 0f;
        }

        private void HandleLook(PlayerMovement player)
        {
            if (player.GameStateSystem != null && player.GameStateSystem.isPaused)
                return;

            float mouseSensitivity = player.GetMouseSensitivity();
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            float currentFrameTime = Time.unscaledTime;
            float frameDelta = currentFrameTime - player.LastFrameTime;

            if (frameDelta > 0.1f)
            {
                player.LastFrameTime = currentFrameTime;
                return;
            }

            player.LastFrameTime = currentFrameTime;

            float newXRotation = player.XRotation - mouseY;
            newXRotation = Mathf.Clamp(newXRotation, -90f, 90f);

            float rotationDelta = Mathf.Abs(newXRotation - player.PreviousXRotation);
            if (rotationDelta > 45f)
            {
                return;
            }

            player.PreviousXRotation = player.XRotation;
            player.XRotation = newXRotation;

            player.FPSCamera.transform.localRotation = Quaternion.Euler(player.XRotation, 0f, 0f);
            player.transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleDashMovement(PlayerMovement player)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                return;
            }

            float normalizedTime = 1f - (dashTimer / player.dashDuration);
            float dashIntensity = player.dashCurve.Evaluate(normalizedTime);

            Vector3 dashMovement = dashDirection * player.dashSpeed * dashIntensity * Time.deltaTime;
            player.Controller.Move(dashMovement);
        }

        private void HandleGravity(PlayerMovement player)
        {
            player.CheckGrounded();

            if (player.IsGrounded && player.Velocity.y < 0)
            {
                player.Velocity = new Vector3(player.Velocity.x, -2f, player.Velocity.z);
            }

            player.Velocity = new Vector3(player.Velocity.x, player.Velocity.y + player.gravity * Time.deltaTime, player.Velocity.z);
            player.Controller.Move(player.Velocity * Time.deltaTime);
        }

        public bool IsDashComplete()
        {
            return dashTimer <= 0f;
        }
    }
}