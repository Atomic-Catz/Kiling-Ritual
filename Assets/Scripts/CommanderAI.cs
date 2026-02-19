using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CommanderAI : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public Animator animator;

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
        if (player == null)
        {
            var found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            // IMPORTANT: We handle rotation manually so he can face player while moving backward
            agent.updateRotation = false; 
        }
    }

    private void OnEnable()
    {
        buffCoroutine = StartCoroutine(BuffUpdateRoutine());
        aiCoroutine = StartCoroutine(AIUpdateRoutine());
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

    private void TickAI()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        strafeTimer += Time.deltaTime;

        // 1. ALWAYS FACE THE PLAYER
        RotateTowardsPlayer();

        // 2. TACTICAL MANEUVERING
        if (dist < desiredDistance)
        {
            // RETREAT & WEAVE
            Vector3 awayDir = (transform.position - player.position).normalized;
            Vector3 sideDir = Vector3.Cross(awayDir, Vector3.up);
            float weave = Mathf.Sin(strafeTimer * strafeSpeed) * strafeIntensity;
            
            Vector3 targetPos = player.position + (awayDir * desiredDistance) + (sideDir * weave);
            MoveToPoint(targetPos);
        }
        else if (dist > maxDistance)
        {
            // APPROACH
            MoveToPoint(player.position);
        }
        else
        {
            // COMBAT JIGGLE (Stays in range but shuffles to be a harder target)
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
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
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

        if (projectilePrefab == null || projectileSpawn == null) return;

        GameObject proj = Instantiate(projectilePrefab, projectileSpawn.position, Quaternion.LookRotation(dir));
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = dir * projectileSpeed;

        if (animator != null) animator.SetTrigger("IsAttacking");
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

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        health -= amount;
        if (health <= 0f) Die();
    }

    private void Die()
    {
        if (isDead) return; // If already dead, stop right here!
        isDead = true;

        if (health > 0) health = 0;
    
        GetComponent<SpawnerEnemy>()?.ReportDeath();
        RevertAllBuffs();

        if (ScoreManager.Instance != null) 
            ScoreManager.Instance.AddPoints(0, pointsOnDeath);

        if (agent != null) agent.enabled = false;
        if (animator != null) animator.enabled = false;

        var ragdoll = GetComponent<RagdollController>();
        if (ragdoll != null) ragdoll.SetRagdoll(true);

        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");
        Destroy(gameObject, 5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, buffRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, desiredDistance);
    }
}