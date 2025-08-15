using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Rooms;

namespace Helloop.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Animator))]
    public class Enemy : MonoBehaviour
    {
        [Header("Enemy Data")]
        public EnemyData enemyData;

        [Header("Instance Settings")]
        [HideInInspector] public Transform returnPoint;
        [HideInInspector] public List<Transform> patrolPoints = new List<Transform>();

        [Header("System References")]
        public PlayerSystem playerSystem;
        public ProgressionSystem progressionSystem;
        public ScalingSystem scalingSystem;

        [Header("Line of Sight Settings")]
        public LayerMask wallLayers = 8;

        [Header("Patrol Settings")]
        public float patrolDelay = 7.5f;

        public NavMeshAgent Agent { get; private set; }
        public Animator Animator { get; private set; }
        public Transform Player { get; private set; }
        public EnemyData EnemyData => enemyData;
        public PlayerSystem PlayerSystem => playerSystem;
        public ScalingSystem ScalingSystem => scalingSystem;
        public RoomController AssignedRoomController { get; private set; }

        [Header("Animation")]
        [Tooltip("Optional: assign an Animator component (e.g., a child like 'joints'). If null, uses Animator on this GameObject.")]
        [SerializeField] private Animator animatorReference;
        public bool IsDead { get; private set; }
        public bool HasSeenPlayer { get; set; }
        public bool IsReturning { get; set; }
        public bool HasReturnedToBase { get; set; }
        public bool IsCurrentlyAttacking { get; private set; }
        public float LastSeenPlayerTime { get; set; } = -100f;
        public float PatrolDelay => patrolDelay;
        public List<Transform> PatrolPoints => patrolPoints;
        public Transform ReturnPoint => returnPoint;

        public int EnemyLevel => progressionSystem?.GetCurrentCircleNumber() ?? 1;
        public float ScaledDamage => scalingSystem.GetScaledEnemyDamage(enemyData.damage, EnemyLevel);
        public float ScaledMoveSpeed => scalingSystem.GetScaledEnemySpeed(enemyData.moveSpeed, EnemyLevel);

        private EnemyStateMachine stateMachine;
        private float currentHealth;
        private bool cachedCanSeePlayer;
        private float lastLineOfSightCheck;
        private float lineOfSightCheckInterval = 0.2f;
        private bool stealthCacheValid = true;
        private AudioSource audioSource;
        private float hitSfxEndsAt = 0f;

        public float AttackDetectionRange => enemyData?.attackDetectionRange ?? 0f;
        public float DamageRange => enemyData?.attackRange ?? 0f;

        private bool wasDetectingPlayer = false;

        [SerializeField] private float angularTurnSpeed = 200f;


        void Start()
        {
            InitializeComponents();
            InitializeStateMachine();
            ApplyEnemyData();
            SubscribeToPlayerEvents();
        }

        void Update()
        {
            stateMachine?.Update();

            if (Agent != null && Agent.isActiveAndEnabled)
            {
                if (IsCurrentlyAttacking || IsDead)
                {
                    if (!Agent.isStopped) Agent.isStopped = true;
                    Agent.updateRotation = false;
                }
                else
                {
                    if (Agent.isStopped) Agent.isStopped = false;
                    Agent.updateRotation = true;
                }
            }
        }

        private void InitializeComponents()
        {
            Agent = GetComponent<NavMeshAgent>();
            Animator = animatorReference != null ? animatorReference : GetComponent<Animator>();
            SetReturnPoint();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;

            RandomizeAnimationTiming(); // ADD THIS LINE
        }

        private void RandomizeAnimationTiming()
        {
            if (Animator != null)
            {
                float speedVariation = Random.Range(0.9f, 1.1f);
                Animator.speed = speedVariation;

                Invoke(nameof(ApplyRandomOffset), 0.1f);
            }
        }

        private void ApplyRandomOffset()
        {
            if (Animator != null)
            {
                float randomOffset = Random.Range(0f, 1f);
                AnimatorStateInfo currentState = Animator.GetCurrentAnimatorStateInfo(0);
                Animator.Play(currentState.fullPathHash, 0, randomOffset);
            }
        }

        private void InitializeStateMachine()
        {
            stateMachine = new EnemyStateMachine(this);
            stateMachine.Initialize();
        }

        private void ApplyEnemyData()
        {
            if (enemyData == null) return;

            currentHealth = enemyData.maxHealth;

            if (Agent != null)
            {
                Agent.speed = ScaledMoveSpeed;
                Agent.angularSpeed = angularTurnSpeed;
            }
        }

        private void SubscribeToPlayerEvents()
        {
            if (PlayerSystem != null)
            {
                PlayerSystem.OnPlayerRespawned?.Subscribe(UpdatePlayerReference);
                PlayerSystem.OnPlayerDied?.Subscribe(ClearPlayerReference);
                PlayerSystem.OnStealthChanged?.Subscribe(InvalidateStealthCache);

                UpdatePlayerReference();
            }
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (PlayerSystem != null)
            {
                PlayerSystem.OnPlayerRespawned?.Unsubscribe(UpdatePlayerReference);
                PlayerSystem.OnPlayerDied?.Unsubscribe(ClearPlayerReference);
                PlayerSystem.OnStealthChanged?.Unsubscribe(InvalidateStealthCache);
            }
        }

        private void UpdatePlayerReference()
        {
            Player = PlayerSystem?.GetPlayer();
        }

        private void ClearPlayerReference()
        {
            Player = null;
        }

        private void InvalidateStealthCache()
        {
            stealthCacheValid = false;
            lastLineOfSightCheck = 0f;
        }
        public bool IsInDamageRange()
        {
            return Player != null && Vector3.Distance(transform.position, Player.position) <= DamageRange;
        }

        private void SetReturnPoint()
        {
            if (returnPoint == null)
            {
                GameObject returnObj = new GameObject($"{gameObject.name}_ReturnPoint");
                returnObj.transform.position = transform.position;
                returnObj.transform.parent = transform.parent;
                returnPoint = returnObj.transform;
            }
        }

        public void SetAttackingStatus(bool attacking)
        {
            IsCurrentlyAttacking = attacking;
        }

        public bool ShouldStartPatrolling()
        {
            return Time.time > 1f;
        }

        public bool CanSeePlayer()
        {
            UpdateCachedCanSeePlayer();
            return cachedCanSeePlayer;
        }

        public void UpdateCachedCanSeePlayer()
        {
            bool shouldRecheck = Time.time - lastLineOfSightCheck >= lineOfSightCheckInterval
                                || !stealthCacheValid;

            if (shouldRecheck)
            {
                lastLineOfSightCheck = Time.time;
                stealthCacheValid = true;

                bool previousCanSee = cachedCanSeePlayer;
                cachedCanSeePlayer = HasLineOfSightToPlayer();

                if (previousCanSee != cachedCanSeePlayer)
                {
                    UpdatePlayerDetection();
                }
            }
        }

        public bool IsInAttackRange()
        {
            return Player != null && Vector3.Distance(transform.position, Player.position) <= enemyData.attackDetectionRange;
        }

        public bool ShouldReturn()
        {
            return HasSeenPlayer &&
                   enemyData.pursueDistance > 0 &&
                   Player != null &&
                   Vector3.Distance(transform.position, Player.position) > enemyData.pursueDistance &&
                   Time.time - LastSeenPlayerTime > 2f;
        }

        public bool HasLineOfSightToPlayer()
        {
            if (Player == null || enemyData == null || !PlayerSystem.HasPlayer())
                return false;

            float distance = Vector3.Distance(transform.position, Player.position);
            if (distance > enemyData.sightRange) return false;

            if (PlayerSystem.isStealth)
            {
                float stealthDetectionRange = enemyData.sightRange * 0.3f;
                if (distance > stealthDetectionRange) return false;

                if (distance > 3f) return false;
            }

            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            Vector3 playerPosition = Player.position + Vector3.up * 1.0f;
            Vector3 direction = (playerPosition - eyePosition).normalized;

            if (Physics.Raycast(eyePosition, direction, out RaycastHit hit, distance, wallLayers))
            {
                return hit.collider.CompareTag("Player");
            }

            return true;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            currentHealth -= damage;

            PlayHitAudio();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void PlayAudio(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private void PlayHitAudio()
        {
            if (enemyData?.gettingHitSound == null || audioSource == null)
                return;

            if (Time.time >= hitSfxEndsAt)
            {
                audioSource.PlayOneShot(enemyData.gettingHitSound);
                hitSfxEndsAt = Time.time + enemyData.gettingHitSound.length - 0.5f;
            }
        }

        private void Die()
        {
            IsDead = true;

            if (enemyData?.deathSound != null)
            {
                PlayAudio(enemyData.deathSound);
            }

            Destroy(gameObject, 2f);
        }

        public void SetRoomController(RoomController room)
        {
            AssignedRoomController = room;
        }

        public float GetHealthPercentage() => currentHealth / enemyData.maxHealth;

        public void UpdatePlayerDetection()
        {
            bool currentlyDetecting = HasLineOfSightToPlayer();

            if (currentlyDetecting != wasDetectingPlayer)
            {
                if (currentlyDetecting)
                {
                    if (PlayerSystem != null)
                    {
                        PlayerSystem.RegisterDetection();
                    }
                }
                else
                {
                    if (PlayerSystem != null)
                    {
                        PlayerSystem.UnregisterDetection();
                    }
                }

                wasDetectingPlayer = currentlyDetecting;
            }
        }

        void OnDestroy()
        {
            if (wasDetectingPlayer && PlayerSystem != null)
            {
                PlayerSystem.UnregisterDetection();
            }

            UnsubscribeFromPlayerEvents();
        }
    }
}