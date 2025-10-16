using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.LowLevel;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public Transform attackPoint;

    public LayerMask isGround;
    public LayerMask isPlayer;

    public float health = 100f;
    public float timeBetweenAttacks = 1.5f;
    private bool alreadyAttacked;
    public int meleeDamage = 10;

    public Vector3 walkPoint;
    private bool walkPointSet;
    public float walkPointRange = 10f;

    public float sightRange = 15f;
    public float attackRange = 2f;
    public bool playerInSight;
    public bool playerInAttack;

    private void Awake()
    {
        if (player == null)
        {
            var found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
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

    private void Patroling()
    {
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        Vector3 randomPoint = transform.position + new Vector3(randomX, 0f, randomZ);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, walkPointRange, NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
        }
        else
        {
            walkPointSet = false;
        }
    }

    private void ChasePlayer()
    {
        if (agent.isStopped) agent.isStopped = false;
        if (player != null)
            agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        agent.ResetPath();
        agent.isStopped = true;

        if (player == null) return;

        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);

        if (!alreadyAttacked)
        {
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

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
        if (agent != null && agent.enabled) 
            agent.isStopped = false;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0f)
            Invoke(nameof(DestroyEnemy), 0f);
    }

    private void DestroyEnemy()
    {
        if (agent != null)
            agent.enabled = false;

        this.enabled = false;

        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider != null)
            rootCollider.enabled = false;
        
        var ragdoll = GetComponent<RagdollController>();
        if (ragdoll != null)
            ragdoll.SetRagdoll(true);

        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");

        foreach (Transform child in transform)
            child.gameObject.layer = LayerMask.NameToLayer("DeadEnemy");
        
        Destroy(gameObject, 5f);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
