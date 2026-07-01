using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PurrNet; // IMPORT PURRNET CORE

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CommanderAI : NetworkBehaviour
    {
        [Header("References")]
        public NavMeshAgent agent;
        public Transform player; // This acts as our current runtime target
        public Animator animator;

        // --- UPDATED: AUDIO SETTINGS ---
        [Header("Audio")]
        public AudioSource audioSource;
        [Tooltip("The single sound that plays randomly while the commander is alive.")]
        public AudioClip occasionalSound;
        
        [Tooltip("The sound that plays when launching a projectile.")]
        public AudioClip attackSound;
        
        [Tooltip("How often (in seconds) the commander should randomly play the occasional sound.")]
        public float minSoundDelay = 4f;
        public float maxSoundDelay = 10f;
        private float nextSoundTime = 0f;
        // -------------------------------

        [Header("Buff (aura)")]
        public float buffRadius = 10f;
        [Range(1f, 4f)]
        public float buffMultiplier = 1.5f;
        public float buffUpdateInterval = 0.5f;

        [Header("Tactical Movement")]
        public float desiredDistance = 12f;
        public float maxDistance = 18f;
        public float moveSpeed = 3.5f;
        [Tooltip("How much he weaves left/right while moving.")]
        public float strafeIntensity = 5f;
        [Tooltip("How fast he weaves.")]
        public float strafeSpeed = 2f;

        [Header("Health & Death")]
        public float health = 500f;
        public int pointsOnDeath = 250;

        [Header("Powerup Drop")]
        public List<GameObject> powerupDrops = new List<GameObject>();
        [Range(0f, 100f)]
        public float powerupDropChance = 10f;

        [Header("Ranged Attack")]
        public GameObject projectilePrefab;
        public Transform projectileSpawn;
        public float projectileSpeed = 25f;
        public int projectileDamage = 20;
        public float fireRange = 25f;
        public float fireRate = 1.0f;
        public LayerMask losMask = ~0;

        [Header("Misc")]
        public float updateRate = 0.1f;

        private bool isDead = false;
        private float lastFireTime = -999f;
        private float strafeTimer;
        private Dictionary<EnemyAI, int> buffedOriginals = new Dictionary<EnemyAI, int>();
        private Coroutine buffCoroutine;
        private Coroutine aiCoroutine;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponent<Animator>();

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.updateRotation = false; 
            }
        }

        private void Start()
        {
            // Network frameworks need a split-second to assign "isServer". 
            // By the time Start() runs, the server authority is securely established.
            if (isServer)
            {
                // Set the first random sound timer
                nextSoundTime = Time.time + Random.Range(minSoundDelay, maxSoundDelay);
                
                buffCoroutine = StartCoroutine(BuffUpdateRoutine());
                aiCoroutine = StartCoroutine(AIUpdateRoutine());
            }
        }

        private void OnDisable()
        {
            if (buffCoroutine != null) StopCoroutine(buffCoroutine);
            if (aiCoroutine != null) StopCoroutine(aiCoroutine);
            RevertAllBuffs();
        }

        private void OnDestroy() { RevertAllBuffs(); }

        private IEnumerator AIUpdateRoutine()
        {
            var wait = new WaitForSecondsRealtime(updateRate);
            while (true)
            {
                FindClosestPlayer();
                TickAI();
                yield return wait;
            }
        }

        private IEnumerator BuffUpdateRoutine()
        {
            var wait = new WaitForSecondsRealtime(buffUpdateInterval);
            while (true)
            {
                UpdateBuffAura();
                yield return wait;
            }
        }

        /// <summary>
        /// Scans all active networked player scripts in the environment and selects the nearest target
        /// </summary>
        private void FindClosestPlayer()
        {
            Character[] allPlayers = FindObjectsOfType<Character>();
            if (allPlayers == null || allPlayers.Length == 0)
            {
                player = null;
                return;
            }

            Character closestCharacter = null;
            float shortestDistance = Mathf.Infinity;

            foreach (Character p in allPlayers)
            {
                if (p == null) continue;
                
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    closestCharacter = p;
                }
            }

            if (closestCharacter != null)
            {
                player = closestCharacter.transform;
            }
        }

        private void TickAI()
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

            if (Time.time >= nextSoundTime)
            {
                SyncOccasionalSoundRpc();
                nextSoundTime = Time.time + Random.Range(minSoundDelay, maxSoundDelay);
            }

            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);
            strafeTimer += Time.deltaTime;

            // 1. ALWAYS FACE THE PLAYER
            RotateTowardsPlayer();

            // 2. TACTICAL MANEUVERING
            if (dist < desiredDistance)
            {
                Vector3 awayDir = (transform.position - player.position).normalized;
                Vector3 sideDir = Vector3.Cross(awayDir, Vector3.up);
                float weave = Mathf.Sin(strafeTimer * strafeSpeed) * strafeIntensity;
                
                Vector3 targetPos = player.position + (awayDir * desiredDistance) + (sideDir * weave);
                MoveToPoint(targetPos);
            }
            else if (dist > maxDistance)
            {
                MoveToPoint(player.position);
            }
            else
            {
                if (Mathf.Repeat(strafeTimer, 1.5f) > 1.2f)
                {
                    Vector3 jigglePos = transform.position + (transform.right * Random.Range(-2f, 2f));
                    MoveToPoint(jigglePos);
                }
                else
                {
                    agent.isStopped = true;
                }
            }

            // 3. SHOOTING LOGIC
            HandleAttackLogic(dist);
        }

        private void RotateTowardsPlayer()
        {
            Vector3 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
            }
        }

        private void MoveToPoint(Vector3 point)
        {
            // SetDestination handles nearest-valid-point logic automatically and perfectly.
            agent.isStopped = false;
            agent.SetDestination(point);
        }

        private void HandleAttackLogic(float dist)
        {
            if (dist <= fireRange)
            {
                Vector3 origin = (projectileSpawn != null) ? projectileSpawn.position : transform.position + Vector3.up * 1.5f;
                Vector3 dirToPlayer = (player.position + Vector3.up * 0.8f) - origin;
                
                if (Physics.Raycast(origin, dirToPlayer.normalized, out RaycastHit hit, dist, losMask))
                {
                    if (hit.transform == player || hit.transform.IsChildOf(player))
                        TryFireAtPlayer(origin, dirToPlayer.normalized);
                }
            }
        }

        private void TryFireAtPlayer(Vector3 origin, Vector3 dir)
        {
            if (Time.time - lastFireTime < (1f / Mathf.Max(0.0001f, fireRate))) return;
            lastFireTime = Time.time;

            SyncAttackAndProjectile(origin, dir);
        }

        [ObserversRpc]
        private void SyncAttackAndProjectile(Vector3 origin, Vector3 dir)
        {
            // 1. Play the animation on all screens
            if (animator != null) animator.SetTrigger("IsAttacking");

            // --- NEW: Play Attack Sound ---
            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            // ------------------------------

            // 2. Spawn a local copy of the projectile for EVERY player
            if (projectilePrefab != null)
            {
                Vector3 spawnPos = (projectileSpawn != null) ? projectileSpawn.position : origin;
                
                GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
                
                // --- ASSIGN THE OWNER! ---
                // This guarantees the fireball ignores the Commander's own colliders
                var projScript = proj.GetComponent<SimpleProjectile>();
                if (projScript != null) projScript.owner = gameObject;

                var rb = proj.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = dir * projectileSpeed;
            }
        }

        private void UpdateBuffAura()
        {
            var allEnemies = FindObjectsOfType<EnemyAI>();
            HashSet<EnemyAI> inRange = new HashSet<EnemyAI>();
            
            foreach (var e in allEnemies)
            {
                if (e == null || e.gameObject == gameObject || e.health <= 0f) continue;
                if (Vector3.Distance(transform.position, e.transform.position) <= buffRadius)
                    inRange.Add(e);
            }

            List<EnemyAI> toRemove = new List<EnemyAI>();
            foreach (var kv in buffedOriginals)
            {
                if (kv.Key == null || !inRange.Contains(kv.Key))
                {
                    if (kv.Key != null) kv.Key.meleeDamage = kv.Value;
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var r in toRemove) buffedOriginals.Remove(r);

            foreach (var e in inRange)
            {
                if (buffedOriginals.ContainsKey(e)) continue;
                buffedOriginals[e] = e.meleeDamage;
                e.meleeDamage = Mathf.CeilToInt(e.meleeDamage * buffMultiplier);
            }
        }

        private void RevertAllBuffs()
        {
            foreach (var kv in buffedOriginals)
                if (kv.Key != null) kv.Key.meleeDamage = kv.Value;
            buffedOriginals.Clear();
        }

        private int lastAttackerId = 0;
        
        public void TakeDamage(float amount, int attackerId)
        {
            if (isDead) return;
            
            lastAttackerId = attackerId;
            health -= amount;

            if (health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (health > 0) health = 0;
        
            GetComponent<SpawnerEnemy>()?.ReportDeath();
            RevertAllBuffs();

            if (ScoreManager.Instance != null) 
                ScoreManager.Instance.AddPoints(0, pointsOnDeath);

            SyncDeathVisuals();
            Destroy(gameObject, 5f);
        }

        [ObserversRpc]
        private void SyncDeathVisuals()
        {
            if (agent != null) agent.enabled = false;
            if (animator != null) animator.enabled = false;

            var ragdoll = GetComponent<RagdollController>();
            if (ragdoll != null) ragdoll.SetRagdoll(true);

            gameObject.layer = LayerMask.NameToLayer("DeadEnemy");
        }

        [ObserversRpc]
        private void SyncOccasionalSoundRpc()
        {
            if (audioSource != null && occasionalSound != null)
            {
                audioSource.PlayOneShot(occasionalSound);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, buffRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, desiredDistance);
        }
    }
}