using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
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

    #region UNITY

    private void Awake()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
        }

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
        if (agent == null || !agent.isOnNavMesh) return;

        var camo = FindObjectOfType<CamoBuff>();
        if (camo != null && camo.IsActive())
        {
            agent.ResetPath();
            agent.isStopped = true;
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.ResetTrigger("IsAttacking");
            }
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

        playerInSight = Physics.CheckSphere(transform.position, sightRange, isPlayer);
        playerInAttack = Physics.CheckSphere(transform.position, attackRange, isPlayer);

        if (!isGrabbed && playerInAttack && playerInSight)
        {
            AttackPlayer();
        }
        else if (playerInSight && !playerInAttack)
        {
            if(!isGrabbed)
                ChasePlayer();
            else
            {
                agent.ResetPath();
                agent.isStopped = true;
                if (animator != null)
                    animator.SetBool("IsWalking", false);
            }
        }
        else
        {
            if (!isGrabbed)
                Patroling();
            else
            {
                agent.ResetPath();
                agent.isStopped = true;
                if (animator != null)
                    animator.SetBool("IsWalking", false);
            }
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

        if (animator != null)
            animator.SetBool("IsWalking", walkPointSet);

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

        if (animator != null)
            animator.SetBool("IsWalking", true);
    }

    #endregion

    #region ATTACK

    private void AttackPlayer()
    {
        var camo = FindObjectOfType<CamoBuff>();
        if (camo != null && camo.IsActive())
        {
            agent.ResetPath();
            agent.isStopped = true;
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.ResetTrigger("IsAttacking");
            }
            return;
        }

        if (agent == null || !agent.isOnNavMesh || player == null) return;

        agent.ResetPath();
        agent.isStopped = true;

        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);

        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.ResetTrigger("IsAttacking");
            animator.SetTrigger("IsAttacking");
        }

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

    #region DAMAGE & DEATH

    public void TakeDamage(int damage, int attackerId = -1)
    {
        health -= damage;
        if (health <= 0f)
            DestroyEnemy();
    }

    private void DestroyEnemy()
    {
        
        if (grabCountdownCoroutine != null)
            StopCoroutine(grabCountdownCoroutine);
        
        if (activeGrabCoroutine != null)
            StopCoroutine(activeGrabCoroutine);
        
        ForceReleaseGrab();
        
        int multiplier = 1;

        var triple = FindObjectOfType<TripleScoreBuff>();
        if (triple != null && triple.IsActive())
            multiplier = 3;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddPoints(0, pointsOnDeath * multiplier);

        if (agent != null) agent.enabled = false;
        if (animator != null) animator.enabled = false;

        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider != null) rootCollider.enabled = false;

        var ragdoll = GetComponent<RagdollController>();
        if (ragdoll != null) ragdoll.SetRagdoll(true);

        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");
        foreach (Transform child in transform)
            child.gameObject.layer = LayerMask.NameToLayer("DeadEnemy");

        // Try to spawn powerups
        TrySpawnPowerups();

        Destroy(gameObject, 5f);
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
        
        if (!canGrab)
            yield break;
        
        int seconds = Mathf.Max(1, grabCountdownSeconds);
        for (int i = 0; i < seconds; i++)
        {
            yield return new WaitForSeconds(1f);

            if (player == null)
                break;
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > grabRadius) 
                break;

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
        
        if (!canGrab)
            yield break;
        
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            yield break;
        
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
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.ResetTrigger("IsAttacking");
        }

        Movement playerMovement = null;
        Rigidbody playerRb = null;
        if (player != null)
        {
            playerMovement = player.GetComponentInChildren<Movement>();
            if (playerMovement != null) playerMovement.enabled = false;

            playerRb = player.GetComponentInChildren<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0.01f, grabDuration));

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            ForceReleaseGrab();
            yield break;
        }
        
        isGrabbed = false;
        
        ForceReleaseGrab();
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

        // Pick a random powerup from the list
        var drop = powerupDrops[Random.Range(0, powerupDrops.Count)];
        if (drop.prefab == null) return;

        float roll = Random.Range(0f, 100f);
        if (roll <= drop.dropChance)
            Instantiate(drop.prefab, transform.position + Vector3.up, Quaternion.identity);
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
