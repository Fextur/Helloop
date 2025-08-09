using UnityEngine;
using System.Collections;

namespace Helloop.Rewards
{
    public class RewardVisualEffects : MonoBehaviour
    {
        [Header("Core Animation")]
        public AnimationCurve bobCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float bobHeight = 0.5f;
        public float bobSpeed = 2f;
        public bool useRandomBobOffset = true;

        [Header("Rotation Settings")]
        public bool enableRotation = true;
        public Vector3 rotationAxis = Vector3.up;
        public float rotationSpeed = 45f;
        public AnimationCurve rotationSpeedCurve = AnimationCurve.Constant(0f, 1f, 1f);

        [Header("Glow & Emission")]
        public bool enableGlow = true;
        public Color baseGlowColor = new Color(1f, 0.8f, 0.2f, 1f);
        public float glowIntensity = 3f;
        public float pulseSpeed = 1.5f;
        public AnimationCurve glowPulseCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f);
        public bool useHDRColors = true;

        [Header("Scale Effects")]
        public bool enableScalePulse = true;
        public float scaleAmount = 0.15f;
        public float scaleSpeed = 2f;
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

        [Header("Spawn Animation")]
        public bool playSpawnAnimation = true;
        public float spawnDuration = 0.8f;
        public AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve spawnAlphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Particle Effects")]
        public bool enableParticles = true;
        public GameObject particleEffectPrefab;
        public int particleCount = 8;
        public float particleOrbitRadius = 1.2f;
        public float particleOrbitSpeed = 30f;

        [Header("Audio Integration")]
        public AudioClip spawnSound;
        public AudioClip ambientLoopSound;
        [Range(0f, 1f)] public float audioVolume = 0.7f;

        [Header("Magnetism Effect")]
        public bool enableMagnetism = true;
        public float magnetismRange = 3f;
        public float magnetismStrength = 0.5f;
        public AnimationCurve magnetismCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Performance Settings")]
        public float updateInterval = 0.016f; // ~60fps
        public bool usePooledParticles = true;

        private Vector3 startPosition;
        private Vector3 originalScale;
        private Quaternion originalRotation;
        private float randomTimeOffset;
        private float spawnTime;
        private bool isSpawning;

        private Renderer[] objectRenderers;
        private Material[] originalMaterials;
        private Material[] effectMaterials;

        private ParticleSystem attachedParticleSystem;
        private Transform[] orbitingParticles;

        private AudioSource audioSource;
        private AudioSource ambientAudioSource;

        private Transform playerTransform;
        private bool playerInRange;

        private float lastUpdateTime;
        private WaitForSeconds updateWait;

        void Start()
        {
            InitializeComponents();
            SetupVisualEffects();
            SetupAudio();
            SetupParticleEffects();

            if (playSpawnAnimation)
            {
                StartCoroutine(PlaySpawnAnimation());
            }
        }

        void InitializeComponents()
        {
            startPosition = transform.position;
            originalScale = transform.localScale;
            originalRotation = transform.rotation;
            randomTimeOffset = useRandomBobOffset ? Random.Range(0f, Mathf.PI * 2f) : 0f;
            spawnTime = Time.time;
            updateWait = new WaitForSeconds(updateInterval);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        void SetupVisualEffects()
        {
            objectRenderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[objectRenderers.Length];
            effectMaterials = new Material[objectRenderers.Length];

            for (int i = 0; i < objectRenderers.Length; i++)
            {
                if (objectRenderers[i] != null)
                {
                    originalMaterials[i] = objectRenderers[i].material;

                    if (enableGlow)
                    {
                        effectMaterials[i] = new Material(originalMaterials[i]);
                        SetupGlowMaterial(effectMaterials[i]);
                        objectRenderers[i].material = effectMaterials[i];
                    }
                }
            }
        }

        void SetupGlowMaterial(Material material)
        {
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                if (useHDRColors)
                {
                    Color hdrColor = baseGlowColor * Mathf.Pow(2f, glowIntensity);
                    material.SetColor("_EmissionColor", hdrColor);
                }
                else
                {
                    material.SetColor("_EmissionColor", baseGlowColor * glowIntensity);
                }
            }
        }

        void SetupAudio()
        {
            if (spawnSound != null || ambientLoopSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.volume = audioVolume;
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;

                if (spawnSound != null)
                {
                    audioSource.PlayOneShot(spawnSound);
                }

                if (ambientLoopSound != null)
                {
                    ambientAudioSource = gameObject.AddComponent<AudioSource>();
                    ambientAudioSource.clip = ambientLoopSound;
                    ambientAudioSource.volume = audioVolume * 0.3f;
                    ambientAudioSource.loop = true;
                    ambientAudioSource.spatialBlend = 1f;
                    ambientAudioSource.Play();
                }
            }
        }

        void SetupParticleEffects()
        {
            if (!enableParticles) return;

            attachedParticleSystem = GetComponent<ParticleSystem>();
            if (attachedParticleSystem == null && particleEffectPrefab == null)
            {
                CreateDefaultParticleSystem();
            }
            else if (particleEffectPrefab != null)
            {
                GameObject particles = Instantiate(particleEffectPrefab, transform);
                attachedParticleSystem = particles.GetComponent<ParticleSystem>();
            }

            if (particleCount > 0)
            {
                CreateOrbitingParticles();
            }
        }

        void CreateDefaultParticleSystem()
        {
            GameObject particleObj = new GameObject("RewardParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.zero;

            attachedParticleSystem = particleObj.AddComponent<ParticleSystem>();
            var main = attachedParticleSystem.main;
            main.startLifetime = 2f;
            main.startSpeed = 0.5f;
            main.startSize = 0.1f;
            main.startColor = baseGlowColor;
            main.maxParticles = 20;

            var emission = attachedParticleSystem.emission;
            emission.rateOverTime = 5f;

            var shape = attachedParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
        }

        void CreateOrbitingParticles()
        {
            orbitingParticles = new Transform[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                GameObject orbitParticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orbitParticle.transform.SetParent(transform);
                orbitParticle.transform.localScale = Vector3.one * 0.05f;
                orbitParticle.name = $"OrbitParticle_{i}";

                Destroy(orbitParticle.GetComponent<Collider>());

                Renderer particleRenderer = orbitParticle.GetComponent<Renderer>();
                Material particleMat = new Material(Shader.Find("Standard"));
                particleMat.SetFloat("_Mode", 3);
                particleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                particleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                particleMat.SetInt("_ZWrite", 0);
                particleMat.DisableKeyword("_ALPHATEST_ON");
                particleMat.EnableKeyword("_ALPHABLEND_ON");
                particleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                particleMat.renderQueue = 3000;

                Color particleColor = baseGlowColor;
                particleColor.a = 0.7f;
                particleMat.color = particleColor;

                if (enableGlow)
                {
                    particleMat.EnableKeyword("_EMISSION");
                    particleMat.SetColor("_EmissionColor", baseGlowColor * (glowIntensity * 0.5f));
                }

                particleRenderer.material = particleMat;
                orbitingParticles[i] = orbitParticle.transform;
            }
        }

        void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;

            CheckPlayerProximity();
            UpdateAnimations();
        }

        void CheckPlayerProximity()
        {
            if (playerTransform == null || !enableMagnetism) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            playerInRange = distance <= magnetismRange;
        }

        void UpdateAnimations()
        {
            float gameTime = Time.time + randomTimeOffset;

            UpdateBobbing(gameTime);
            UpdateRotation(gameTime);
            UpdateGlowPulse(gameTime);
            UpdateScalePulse(gameTime);
            UpdateOrbitingParticles(gameTime);
            UpdateMagnetismEffect();
        }

        void UpdateBobbing(float time)
        {
            if (bobHeight <= 0f || isSpawning) return;

            float bobValue = bobCurve.Evaluate((Mathf.Sin(time * bobSpeed) + 1f) * 0.5f);
            float yOffset = bobValue * bobHeight;
            transform.position = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);
        }

        void UpdateRotation(float time)
        {
            if (!enableRotation) return;

            float speedMultiplier = rotationSpeedCurve.Evaluate((time * 0.1f) % 1f);
            float rotationAmount = rotationSpeed * speedMultiplier * Time.deltaTime;
            transform.Rotate(rotationAxis, rotationAmount);
        }

        void UpdateGlowPulse(float time)
        {
            if (!enableGlow || effectMaterials == null) return;

            float pulseValue = glowPulseCurve.Evaluate((Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f);

            for (int i = 0; i < effectMaterials.Length; i++)
            {
                if (effectMaterials[i] != null && effectMaterials[i].HasProperty("_EmissionColor"))
                {
                    Color emissionColor;
                    if (useHDRColors)
                    {
                        emissionColor = baseGlowColor * Mathf.Pow(2f, glowIntensity * pulseValue);
                    }
                    else
                    {
                        emissionColor = baseGlowColor * (glowIntensity * pulseValue);
                    }

                    effectMaterials[i].SetColor("_EmissionColor", emissionColor);
                }
            }
        }

        void UpdateScalePulse(float time)
        {
            if (!enableScalePulse || isSpawning) return;

            float scaleValue = scaleCurve.Evaluate((Mathf.Sin(time * scaleSpeed) + 1f) * 0.5f);
            float scaleMultiplier = 1f + (scaleValue * scaleAmount);
            transform.localScale = originalScale * scaleMultiplier;
        }

        void UpdateOrbitingParticles(float time)
        {
            if (orbitingParticles == null) return;

            for (int i = 0; i < orbitingParticles.Length; i++)
            {
                if (orbitingParticles[i] != null)
                {
                    float angle = (time * particleOrbitSpeed) + (i * 360f / particleCount);
                    float radian = angle * Mathf.Deg2Rad;

                    Vector3 offset = new Vector3(
                        Mathf.Cos(radian) * particleOrbitRadius,
                        Mathf.Sin(radian * 0.5f) * 0.2f,
                        Mathf.Sin(radian) * particleOrbitRadius
                    );

                    orbitingParticles[i].localPosition = offset;
                    orbitingParticles[i].Rotate(Vector3.up, particleOrbitSpeed * 2f * Time.deltaTime);
                }
            }
        }

        void UpdateMagnetismEffect()
        {
            if (!enableMagnetism || !playerInRange || playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            float magnetismFactor = magnetismCurve.Evaluate(1f - (distance / magnetismRange));

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Vector3 magneticOffset = direction * (magnetismStrength * magnetismFactor * Time.deltaTime);

            if (attachedParticleSystem != null)
            {
                var velocityOverLifetime = attachedParticleSystem.velocityOverLifetime;
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
                velocityOverLifetime.x = magneticOffset.x * 2f;
                velocityOverLifetime.z = magneticOffset.z * 2f;
            }
        }

        IEnumerator PlaySpawnAnimation()
        {
            isSpawning = true;
            float elapsedTime = 0f;

            transform.localScale = Vector3.zero;

            while (elapsedTime < spawnDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / spawnDuration;

                float scaleValue = spawnScaleCurve.Evaluate(normalizedTime);
                transform.localScale = originalScale * scaleValue;

                if (effectMaterials != null)
                {
                    float alphaValue = spawnAlphaCurve.Evaluate(normalizedTime);

                    for (int i = 0; i < effectMaterials.Length; i++)
                    {
                        if (effectMaterials[i] != null)
                        {
                            Color color = effectMaterials[i].color;
                            color.a = originalMaterials[i].color.a * alphaValue;
                            effectMaterials[i].color = color;
                        }
                    }
                }

                yield return null;
            }

            transform.localScale = originalScale;
            isSpawning = false;
        }

        void OnDestroy()
        {
            if (effectMaterials != null)
            {
                for (int i = 0; i < effectMaterials.Length; i++)
                {
                    if (effectMaterials[i] != null)
                    {
                        Destroy(effectMaterials[i]);
                    }
                }
            }

            if (orbitingParticles != null)
            {
                for (int i = 0; i < orbitingParticles.Length; i++)
                {
                    if (orbitingParticles[i] != null)
                    {
                        Destroy(orbitingParticles[i].gameObject);
                    }
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (enableMagnetism)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, magnetismRange);
            }
        }
    }
}