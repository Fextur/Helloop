using UnityEngine;
using System.Collections;
using Helloop.Systems;
using Helloop.Events;
using Helloop.Enemies;
using Helloop.Player.States;
using Helloop.StateMachines;

namespace Helloop.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(WeaponManager))]
    [RequireComponent(typeof(InteractionController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float dashSpeed = 15f;
        public float dashDuration = 0.5f;

        [Header("Dash System")]
        public float dashCooldown = 2f;
        public float dashStaminaCost = 50f;
        public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Gravity & Ground")]
        public float gravity = -20f;
        public float groundDistance = 0.4f;
        public LayerMask groundMask = -1;
        public Transform groundCheck;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float staminaRegenRate = 10f;

        [Header("Camera")]
        public Camera fpsCamera;
        public Camera tpsCamera;
        public GameObject playerModel;

        [Header("Stealth")]
        public float stealthMoveSpeed = 2f;
        public Animator animator;

        [Header("System References")]
        public PlayerSystem playerSystem;
        public GameStateSystem gameStateSystem;

        [Header("Events")]
        public GameEvent OnDashAvailable;
        public GameEvent OnDashUnavailable;

        public CharacterController Controller { get; private set; }
        public Camera FPSCamera => fpsCamera;
        public Camera TPSCamera => tpsCamera;
        public GameObject PlayerModel => playerModel;
        public Animator Animator => animator;
        public PlayerSystem PlayerSystem => playerSystem;
        public GameStateSystem GameStateSystem => gameStateSystem;

        public Vector3 Velocity { get; set; }
        public float XRotation { get; set; }
        public float PreviousXRotation { get; set; }
        public float LastFrameTime { get; set; }
        public float CurrentStamina { get; set; }
        public float LastDashTime { get; set; } = -Mathf.Infinity;
        public bool IsGrounded { get; private set; }
        public bool IsDashing { get; set; }
        public bool WantsToEnterStealth { get; set; }
        public bool WantsToExitStealth { get; set; }

        private PlayerMovementStateMachine stateMachine;
        private bool wasDashAvailable = true;
        private bool isExitingStealth = false;

        private float lastGroundCheck = 0f;
        private float groundCheckInterval = 0.1f;
        private bool cachedIsGrounded = false;

        public bool isStealth => stateMachine?.IsInState<PlayerStealthState>() ?? false;

        private float cachedMouseSensitivity;


        void Start()
        {
            InitializeComponents();
            InitializeStateMachine();
            CurrentStamina = maxStamina;
            LastFrameTime = Time.unscaledTime;
            PreviousXRotation = XRotation;

            cachedMouseSensitivity = gameStateSystem?.mouseSensitivity ?? 500f;

            if (gameStateSystem != null)
            {
                gameStateSystem.OnMouseSensitivityChanged?.Subscribe(UpdateCachedSensitivity);
            }

        }

        void OnDestroy()
        {
            if (gameStateSystem != null)
            {
                gameStateSystem.OnMouseSensitivityChanged?.Unsubscribe(UpdateCachedSensitivity);
            }
        }

        private void UpdateCachedSensitivity()
        {
            cachedMouseSensitivity = gameStateSystem.mouseSensitivity;
        }

        public float GetMouseSensitivity() => cachedMouseSensitivity;

        void Update()
        {
            if (gameStateSystem != null && gameStateSystem.isPaused)
                return;

            CheckGrounded();
            HandleDashStateEvents();
            HandleInput();
            HandleStamina();
            stateMachine?.Update();
        }

        private void InitializeComponents()
        {
            Controller = GetComponent<CharacterController>();
            SetupGroundCheck();
            SetupCameras();

            Cursor.lockState = CursorLockMode.Locked;
        }

        private void InitializeStateMachine()
        {
            stateMachine = new PlayerMovementStateMachine(this);
            stateMachine.Initialize();
        }

        private void SetupGroundCheck()
        {
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
                groundCheck = groundCheckObj.transform;
            }
        }

        private void SetupCameras()
        {
            if (fpsCamera == null || tpsCamera == null)
                return;

            SwitchToFPSCamera();

            if (playerModel != null)
                playerModel.SetActive(false);
            if (animator != null)
                animator.gameObject.SetActive(false);
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (isExitingStealth) return;

                if (!isStealth)
                {
                    bool beingObserved = false;
                    foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                    {
                        if (enemy.HasLineOfSightToPlayer())
                        {
                            beingObserved = true;
                            break;
                        }
                    }
                    if (!beingObserved)
                    {
                        WantsToEnterStealth = true;
                    }
                }
                else
                {
                    WantsToExitStealth = true;
                }
            }
        }

        private void HandleDashStateEvents()
        {
            bool canDashNow = CanDash();

            if (wasDashAvailable != canDashNow)
            {
                wasDashAvailable = canDashNow;

                if (canDashNow)
                    OnDashAvailable?.Raise();
                else
                    OnDashUnavailable?.Raise();
            }
        }

        private void HandleStamina()
        {
            if (CurrentStamina < maxStamina)
            {
                CurrentStamina += staminaRegenRate * Time.deltaTime;
                CurrentStamina = Mathf.Min(CurrentStamina, maxStamina);
            }
        }

        public bool ShouldStartDashing()
        {
            return Input.GetKeyDown(KeyCode.LeftShift) && CanDash();
        }

        public bool ShouldEndDashing()
        {
            return !IsDashing;
        }

        public bool ShouldEnterStealth()
        {
            if (WantsToEnterStealth)
            {
                WantsToEnterStealth = false;
                return true;
            }
            return false;
        }

        public bool ShouldExitStealth()
        {
            if (WantsToExitStealth)
            {
                WantsToExitStealth = false;
                return true;
            }
            return false;
        }

        public bool CanDash()
        {
            bool hasStamina = CurrentStamina >= dashStaminaCost;
            bool cooldownReady = Time.time >= LastDashTime + dashCooldown;
            bool notCurrentlyDashing = !IsDashing;

            return hasStamina && cooldownReady && notCurrentlyDashing;
        }

        public void CheckGrounded()
        {
            if (Time.time - lastGroundCheck >= groundCheckInterval)
            {
                lastGroundCheck = Time.time;
                cachedIsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }

            IsGrounded = cachedIsGrounded;
        }


        public void SwitchToFPSCamera()
        {
            if (fpsCamera != null && tpsCamera != null)
            {
                fpsCamera.gameObject.SetActive(true);
                fpsCamera.enabled = true;
                fpsCamera.tag = "MainCamera";

                tpsCamera.gameObject.SetActive(true);
                tpsCamera.enabled = false;
                tpsCamera.tag = "Untagged";
            }
        }

        public void SwitchToTPSCamera()
        {
            if (fpsCamera != null && tpsCamera != null)
            {
                tpsCamera.enabled = true;
                tpsCamera.tag = "MainCamera";
                fpsCamera.enabled = false;
                fpsCamera.tag = "Untagged";
            }
        }

        public void SetIsExitingStealth(bool exiting)
        {
            isExitingStealth = exiting;
            if (exiting)
            {
                StartCoroutine(ResetStealthExitFlag());
            }
        }

        private IEnumerator ResetStealthExitFlag()
        {
            yield return new WaitForSeconds(0.1f);
            isExitingStealth = false;
        }

        public void ForceExitStealth()
        {
            if (!isStealth || isExitingStealth) return;

            WantsToExitStealth = true;
            SetIsExitingStealth(true);
        }

        public bool HasEnoughStamina() => CurrentStamina >= dashStaminaCost;

        public float GetDashCooldownRemaining() => Mathf.Max(0f, (LastDashTime + dashCooldown) - Time.time);

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!hit.gameObject.CompareTag("Gate")) return;

            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            if (hit.moveDirection.y < -0.3f) return;

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            body.AddForce(pushDir * 300f, ForceMode.Force);
        }

        public bool IsInState<T>() where T : class, IState<PlayerMovement>
        {
            return stateMachine?.IsInState<T>() ?? false;
        }

        public string GetCurrentStateName()
        {
            return stateMachine?.CurrentState?.GetType().Name ?? "None";
        }
    }
}