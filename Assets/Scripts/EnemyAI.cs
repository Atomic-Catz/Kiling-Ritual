using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;
using System.Collections.Generic;
using System.Collections;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : NetworkBehaviour
    {
        [Header("References")]
        public NavMeshAgent agent;
        public Transform player; // Serves as the dynamic target calculated on Server
        public Transform attackPoint;
        public Animator animator;

        [Header("Stats")]
        public float health = 100f;
        public int meleeDamage = 10;

        [Header("Score Settings")]
        public int pointsOnDeath = 10;

        [Header("Attack Settings")]
        public float attackRange = 2f;
        public float sightRange = 15f;
        public float attackCooldown = 1.5f;

        [Header("Movement Settings")]
        public float walkSpeed = 2f;   // Patrol speed
        public float chaseSpeed = 4f;  // Chase speed

        [Header("Patrol Settings")]
        public float walkPointRange = 10f;
        private Vector3 walkPoint;
        private bool walkPointSet;

        [Header("Grab Settings")] 
        public bool canGrab = true;
        public float grabRadius = 3f;
        public int grabCountdownSeconds = 10;
        [Range(0f, 1f)]
        public float grabChancePerSecond = 0.2f;
        public float grabDuration = 3f;

        [Header("Layers")]
        public LayerMask isPlayer;

        [Header("Powerup Drop Settings")]
        [Tooltip("Powerups that can drop when this enemy dies.")]
        public List<PowerupDrop> powerupDrops = new List<PowerupDrop>();

        private bool playerInSight;
        private bool playerInAttack;
        private float lastAttackTime = -999f;
        private Coroutine grabCountdownCoroutine = null;
        private Coroutine activeGrabCoroutine = null;
        private bool isGrabbed = false;
        private bool isDead = false;

        #region UNITY

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                enabled = false;
                Debug.LogWarning($"{name} AI disabled: NavMeshAgent not on NavMesh!");
            }
        }

        private void Update()
        {
            // CRITICAL NETWORK GUARD: AI decision loops must remain strictly Server Authoritative
            if (!isServer || isDead) return;
            if (agent == null || !agent.isOnNavMesh) return;

            // Recalculate which player window is closer (ignoring downed players!)
            FindClosestPlayer();

            if (GlobalBuffManager.Instance != null && GlobalBuffManager.Instance.isCamoActive)
            {
                agent.ResetPath();
                agent.isStopped = true;
                SyncWalkingAnimation(false);
                return;
            }

            if (canGrab && player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                bool inside = dist <= grabRadius;

                if (inside)
                {
                    if (grabCountdownCoroutine == null && !isGrabbed)
                        grabCountdownCoroutine = StartCoroutine(GrabCountdownRoutine());
                }
                else
                {
                    if (grabCountdownCoroutine != null)
                    {
                        StopCoroutine(grabCountdownCoroutine);
                        grabCountdownCoroutine = null;
                    }
                }
            }

            // Only check for attack/sight if we actually have a valid player target!
            if (player != null)
            {
                playerInSight = Physics.CheckSphere(transform.position, sightRange, isPlayer);
                playerInAttack = Physics.CheckSphere(transform.position, attackRange, isPlayer);
            }
            else
            {
                playerInSight = false;
                playerInAttack = false;
            }

            if (!isGrabbed && playerInAttack && playerInSight && player != null)
            {
                AttackPlayer();
            }
            else if (playerInSight && !playerInAttack && player != null)
            {
                if (!isGrabbed)
                {
                    ChasePlayer();
                }
                else
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                    SyncWalkingAnimation(false);
                }
            }
            else
            {
                if (!isGrabbed)
                {
                    Patroling();
                }
                else
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                    SyncWalkingAnimation(false);
                }
            }
        }

        #endregion

        #region TARGET SELECTION

        private void FindClosestPlayer()
        {
            CharacterHealth[] allPlayers = FindObjectsOfType<CharacterHealth>();
            if (allPlayers == null || allPlayers.Length == 0)
            {
                player = null;
                return;
            }

            CharacterHealth closestCharacter = null;
            float shortestDistance = Mathf.Infinity;

            foreach (CharacterHealth p in allPlayers)
            {
                if (p == null) continue;

                // THE FILTER: Skip this player if they are downed or dead!
                if (p.isDowned.value || p.GetCurrentHealth() <= 0)
                {
                    continue;
                }

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
            else
            {
                // If every single player is downed, clear the target completely!
                player = null;
            }
        }

        #endregion

        #region PATROL

        private void Patroling()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            agent.speed = walkSpeed;

            if (!walkPointSet) SearchWalkPoint();

            if (walkPointSet)
                agent.SetDestination(walkPoint);

            Vector3 distanceToWalkPoint = transform.position - walkPoint;

            SyncWalkingAnimation(walkPointSet);

            if (distanceToWalkPoint.sqrMagnitude < 1f)
                walkPointSet = false;
        }

        private void SearchWalkPoint()
        {
            float randomZ = Random.Range(-walkPointRange, walkPointRange);
            float randomX = Random.Range(-walkPointRange, walkPointRange);
            Vector3 randomPoint = transform.position + new Vector3(randomX, 0f, randomZ);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, walkPointRange, NavMesh.AllAreas))
            {
                walkPoint = hit.position;
                walkPointSet = true;
            }
            else
            {
                walkPointSet = false;
            }
        }

        #endregion

        #region CHASE

        private void ChasePlayer()
        {
            if (agent == null || !agent.isOnNavMesh || player == null) return;

            agent.speed = chaseSpeed;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            SyncWalkingAnimation(true);
        }

        #endregion

        #region ATTACK

        private void AttackPlayer()
        {
            if (GlobalBuffManager.Instance != null && GlobalBuffManager.Instance.isCamoActive)
            {
                agent.ResetPath();
                agent.isStopped = true;
                SyncWalkingAnimation(false);
                return;
            }

            if (agent == null || !agent.isOnNavMesh || player == null) return;

            agent.ResetPath();
            agent.isStopped = true;

            Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookPos);

            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;

            SyncAttackAnimation();

            Transform origin = (attackPoint != null) ? attackPoint : transform;
            Collider[] hits = Physics.OverlapSphere(origin.position, attackRange, isPlayer);
            foreach (var hit in hits)
            {
                if (hit.transform == player)
                {
                    player.SendMessage("TakeDamage", meleeDamage, SendMessageOptions.DontRequireReceiver);
                    break;
                }
            }
        }

        #endregion

        #region REPLICATED ANIMATION SYNC

        [ObserversRpc]
        private void SyncWalkingAnimation(bool state)
        {
            if (animator != null)
                animator.SetBool("IsWalking", state);
        }

        [ObserversRpc]
        private void SyncAttackAnimation()
        {
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.ResetTrigger("IsAttacking");
                animator.SetTrigger("IsAttacking");
            }
        }

        #endregion

        #region DAMAGE & DEATH
        
        private int lastAttackerId = 0;

        public void TakeDamage(float amount, int attackerId)
        {
            if (isDead) return;
            
            lastAttackerId = attackerId;
            health -= amount;

            if (health <= 0)
            {
                DestroyEnemy();
            }
        }

        private void DestroyEnemy()
        {
            if (isDead) return;
            isDead = true;

            if (grabCountdownCoroutine != null) StopCoroutine(grabCountdownCoroutine);
            if (activeGrabCoroutine != null) StopCoroutine(activeGrabCoroutine);
            
            ForceReleaseGrab();
            
            int multiplier = 1;
            if (GlobalBuffManager.Instance != null && GlobalBuffManager.Instance.isTripleScoreActive)
            {
                multiplier = 3;
            }

            Debug.Log(
                $"Enemy died! Attacker ID is: {lastAttackerId}. ScoreManager exists: {ScoreManager.Instance != null}");
            
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddPoints(lastAttackerId, pointsOnDeath * multiplier);

            GetComponent<SpawnerEnemy>()?.ReportDeath();

            SyncDeathVisuals();

            TrySpawnPowerups();

            Destroy(gameObject, 5f);
        }

        [ObserversRpc]
        private void SyncDeathVisuals()
        {
            if (agent != null) agent.enabled = false;
            if (animator != null) animator.enabled = false;

            Collider rootCollider = GetComponent<Collider>();
            if (rootCollider != null) rootCollider.enabled = false;

            var ragdoll = GetComponent<RagdollController>();
            if (ragdoll != null) ragdoll.SetRagdoll(true);

            gameObject.layer = LayerMask.NameToLayer("DeadEnemy");
            foreach (Transform child in transform)
                child.gameObject.layer = LayerMask.NameToLayer("DeadEnemy");
        }

        #endregion

        #region GRAB LOGIC

        private void ForceReleaseGrab()
        {
            isGrabbed = false;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            if (player != null)
            {
                var movement = player.GetComponentInChildren<Movement>();
                if (movement != null)
                    movement.enabled = true;
            }

            activeGrabCoroutine = null;
            grabCountdownCoroutine = null;
        }

        private IEnumerator GrabCountdownRoutine()
        {
            if (!canGrab) yield break;
            
            int seconds = Mathf.Max(1, grabCountdownSeconds);
            for (int i = 0; i < seconds; i++)
            {
                yield return new WaitForSeconds(1f);

                if (player == null) break;
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist > grabRadius) break;

                float roll = Random.value;
                if (roll <= grabChancePerSecond)
                {
                    grabCountdownCoroutine = null;
                    
                    if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                        yield break;
                    
                    if (activeGrabCoroutine != null)
                        StopCoroutine(activeGrabCoroutine);
                    
                    activeGrabCoroutine = StartCoroutine(PerformGrab());
                    yield break;
                }
            }

            grabCountdownCoroutine = null;
        }

        private IEnumerator PerformGrab()
        {
            if (!canGrab) yield break;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) yield break;
            
            isGrabbed = true;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
            else
            {
                yield break;
            }

            SyncWalkingAnimation(false);

            SyncPlayerGrabbedState(true);

            yield return new WaitForSeconds(Mathf.Max(0.01f, grabDuration));

            SyncPlayerGrabbedState(false);

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                ForceReleaseGrab();
                yield break;
            }
            
            isGrabbed = false;
            ForceReleaseGrab();
        }

        [ObserversRpc]
        private void SyncPlayerGrabbedState(bool state)
        {
            if (player == null) return;

            var playerMovement = player.GetComponentInChildren<Movement>();
            if (playerMovement != null) 
                playerMovement.enabled = !state;

            var playerRb = player.GetComponentInChildren<Rigidbody>();
            if (state && playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }
        }

        #endregion

        #region POWERUP DROPS

        [System.Serializable]
        public class PowerupDrop
        {
            [Tooltip("Powerup prefab to spawn")]
            public GameObject prefab;

            [Range(0f, 100f)]
            [Tooltip("Chance (%) this powerup will drop on death")]
            public float dropChance = 10f;
        }

        private void TrySpawnPowerups()
        {
            if (powerupDrops.Count == 0) return;

            var drop = powerupDrops[Random.Range(0, powerupDrops.Count)];
            if (drop.prefab == null) return;

            float roll = Random.Range(0f, 100f);
            if (roll <= drop.dropChance)
            {
                Instantiate(drop.prefab, transform.position + Vector3.up, Quaternion.identity);
            }
        }

        #endregion

        #region GIZMOS

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sightRange);
        }

        #endregion
    }
}