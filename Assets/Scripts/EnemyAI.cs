using UnityEngine;
using UnityEngine.AI;

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

    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float sightRange = 15f;
    public float attackCooldown = 1.5f;

    [Header("Patrol Settings")]
    public float walkPointRange = 10f;
    private Vector3 walkPoint;
    private bool walkPointSet;

    [Header("Layers")]
    public LayerMask isPlayer;

    private bool playerInSight;
    private bool playerInAttack;
    private float lastAttackTime = -999f;

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

        playerInSight = Physics.CheckSphere(transform.position, sightRange, isPlayer);
        playerInAttack = Physics.CheckSphere(transform.position, attackRange, isPlayer);

        if (playerInAttack && playerInSight)
        {
            AttackPlayer();
        }
        else if (playerInSight && !playerInAttack)
        {
            ChasePlayer();
        }
        else
        {
            Patroling();
        }
    }

    #region Patrol
    private void Patroling()
    {
        if (agent == null || !agent.isOnNavMesh) return;

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

    #region Chase
    private void ChasePlayer()
    {
        if (agent == null || !agent.isOnNavMesh || player == null) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);

        if (animator != null)
            animator.SetBool("IsWalking", true);
    }
    #endregion

    #region Attack
    private void AttackPlayer()
    {
        if (agent == null || !agent.isOnNavMesh || player == null) return;

        // Face the player
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);

        // Stop moving while attacking
        agent.ResetPath();
        agent.isStopped = true;

        // Check cooldown
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;

        // Play attack animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.ResetTrigger("IsAttacking");
            animator.SetTrigger("IsAttacking");
        }

        // Deal damage
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

    #region Damage & Death
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0f)
            DestroyEnemy();
    }

    private void DestroyEnemy()
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

        Destroy(gameObject, 5f);
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
    #endregion
}
