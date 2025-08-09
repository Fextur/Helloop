using UnityEngine;
using Helloop.StateMachines;

namespace Helloop.Player.States
{
    public class PlayerWalkingState : IState<PlayerMovement>
    {
        public void OnEnter(PlayerMovement player)
        {
            if (player.Animator != null)
            {
                player.Animator.SetFloat("Speed", 0f);
            }

            player.SwitchToFPSCamera();

            if (player.PlayerModel != null)
                player.PlayerModel.SetActive(false);

            if (player.Animator != null)
                player.Animator.gameObject.SetActive(false);

            if (player.TryGetComponent<WeaponManager>(out var weaponManager))
            {
                weaponManager.SetWeaponsActive(true);
                weaponManager.CanUseWeapons = true;
            }
        }

        public void Update(PlayerMovement player)
        {
            HandleLook(player);
            HandleNormalMovement(player);
            HandleGravity(player);
        }

        public void OnExit(PlayerMovement player)
        {
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

        private void HandleNormalMovement(PlayerMovement player)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 forward = player.FPSCamera.transform.forward;
            Vector3 right = player.FPSCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * v + right * h).normalized;
            player.Controller.Move(moveDirection * player.moveSpeed * Time.deltaTime);
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
    }
}