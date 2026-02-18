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
    [Tooltip("Radius in world units where the commander buffs other enemies.")]
    public float buffRadius = 10f;
    [Tooltip("Multiplier applied to other enemies' meleeDamage (1.5 = +50%).")]
    [Range(1f, 4f)]
    public float buffMultiplier = 1.5f;
    [Tooltip("How often (seconds) we update the buff list.")]
    public float buffUpdateInterval = 0.5f;

    [Header("Kiting / Movement")]
    [Tooltip("Target distance commander tries to keep from player.")]
    public float desiredDistance = 12f;
    [Tooltip("If the commander gets farther than this from player, it will stop moving away and idle.")]
    public float maxDistance = 18f;
    [Tooltip("How fast the commander moves (NavMeshAgent.speed).")]
    public float moveSpeed = 3.5f;
    [Tooltip("How close commander must be to a computed 'away' point to consider we've reached it.")]
    public float stopDistanceThreshold = 0.5f;

    [Header("Health & Death")]
    public float health = 10f;
    public int pointsOnDeath = 50;

    [Header("Powerup Drop (Commander)")]
    [Tooltip("Optional prefab(s) to spawn when the commander dies.")]
    public List<GameObject> powerupDrops = new List<GameObject>();
    [Tooltip("Chance (0-100) to drop one selected powerup from the list.")]
    [Range(0f, 100f)]
    public float powerupDropChance = 10f;


    [Header("Ranged Attack")]
    [Tooltip("Prefab of projectile to spawn (should contain SimpleProjectile or similar).")]
    public GameObject projectilePrefab;
    [Tooltip("Where the projectile spawns (assign a child transform).")]
    public Transform projectileSpawn;
    [Tooltip("Projectile speed (applied to its Rigidbody or movement).")]
    public float projectileSpeed = 25f;
    [Tooltip("Damage dealt by projectile (integer).")]
    public int projectileDamage = 20;
    [Tooltip("Max distance at which commander will try to fire.")]
    public float fireRange = 25f;
    [Tooltip("Shots per second.")]
    public float fireRate = 1.0f;
    [Tooltip("Optional: layer mask for line-of-sight checks (so commander won't shoot through walls).")]
    public LayerMask losMask = ~0;

    [Header("Misc")]
    public float updateRate = 0.1f;

    private float lastFireTime = -999f;

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

    private void OnDestroy()
    {
        RevertAllBuffs();
    }

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
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < desiredDistance)
        {
            Vector3 dir = (transform.position - player.position).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
            Vector3 target = player.position + dir * desiredDistance;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
            else
            {
                Vector3 fallback = transform.position + dir * desiredDistance;
                agent.isStopped = false;
                agent.SetDestination(fallback);
            }
        }
        else
        {
            if (dist > maxDistance)
            {
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                Vector3 newTarget = player.position - dirToPlayer * Mathf.Clamp(desiredDistance, 2f, maxDistance);
                if (NavMesh.SamplePosition(newTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
        }

        if (dist <= fireRange)
        {
            Vector3 origin = (projectileSpawn != null) ? projectileSpawn.position : transform.position + Vector3.up * 1.0f;
            Vector3 dirToPlayer = (player.position + Vector3.up * 0.8f) - origin;
            float distToPlayer = dirToPlayer.magnitude;
            Ray ray = new Ray(origin, dirToPlayer.normalized);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, distToPlayer, losMask))
            {
                if (hitInfo.transform == player || hitInfo.transform.IsChildOf(player))
                    TryFireAtPlayer(origin, dirToPlayer.normalized);
            }
            else
                TryFireAtPlayer(origin, dirToPlayer.normalized);
        }
    }

    private void TryFireAtPlayer(Vector3 origin, Vector3 dir)
    {
        if (Time.time - lastFireTime < (1f / Mathf.Max(0.0001f, fireRate))) return;

        lastFireTime = Time.time;

        if (projectilePrefab == null || projectileSpawn == null)
            return;

        GameObject proj = Instantiate(projectilePrefab, projectileSpawn.position, Quaternion.LookRotation(dir));
        var sp = proj.GetComponent<SimpleProjectile>();
        if (sp != null)
        {
            sp.damage = projectileDamage;
            sp.speed = projectileSpeed;
            sp.owner = gameObject;
        }
        else
        {
            Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = dir * projectileSpeed;
            }
        }

        if (animator != null)
        {
            animator.ResetTrigger("IsAttacking");
            animator.SetTrigger("IsAttacking");
        }
    }

    private void UpdateBuffAura()
    {
        var allEnemies = FindObjectsOfType<EnemyAI>();
        HashSet<EnemyAI> inRange = new HashSet<EnemyAI>();
        for (int i = 0; i < allEnemies.Length; i++)
        {
            var e = allEnemies[i];
            if (e == null) continue;
            if (e.gameObject == this.gameObject) continue;
            if (e.health <= 0f) continue;

            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d <= buffRadius)
                inRange.Add(e);
        }

        var reverted = new List<EnemyAI>();
        foreach (var kv in buffedOriginals)
        {
            var enemy = kv.Key;
            if (enemy == null || !inRange.Contains(enemy))
            {
                if (enemy != null)
                    enemy.meleeDamage = kv.Value;
                reverted.Add(enemy);
            }
        }

        foreach (var rem in reverted)
            buffedOriginals.Remove(rem);

        foreach (var enemy in inRange)
        {
            if (enemy == null) continue;
            if (enemy == this) continue;
            if (buffedOriginals.ContainsKey(enemy)) continue;

            int original = enemy.meleeDamage;
            buffedOriginals[enemy] = original;
            int newDamage = Mathf.CeilToInt(original * buffMultiplier);
            enemy.meleeDamage = newDamage;
        }
    }

    private void RevertAllBuffs()
    {
        foreach (var kv in buffedOriginals)
        {
            var enemy = kv.Key;
            if (enemy != null)
                enemy.meleeDamage = kv.Value;
        }
        buffedOriginals.Clear();
    }

    public void TakeDamage(int damage)
    {
        ApplyDamage((float)damage);
    }

    public void TakeDamage(float damage)
    {
        ApplyDamage(damage);
    }

    private void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        health -= amount;

        Debug.Log($"{name} took {amount} damage. Remaining health: {health}");

        if (health <= 0f)
            Die();
    }

    private void Die()
    {
        if (health > 0f) health = 0f;

        RevertAllBuffs();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddPoints(0, pointsOnDeath);

        if (agent != null) agent.enabled = false;
        if (animator != null) animator.enabled = false;

        var rootCollider = GetComponent<Collider>();
        if (rootCollider != null) rootCollider.enabled = false;

        var ragdoll = GetComponent<RagdollController>();
        if (ragdoll != null)
            ragdoll.SetRagdoll(true);

        gameObject.layer = LayerMask.NameToLayer("DeadEnemy");
        foreach (Transform child in transform)
            child.gameObject.layer = LayerMask.NameToLayer("DeadEnemy");

        TrySpawnPowerups();

        Destroy(gameObject, 5f);
    }

    private void TrySpawnPowerups()
    {
        if (powerupDrops == null || powerupDrops.Count == 0) return;

        var drop = powerupDrops[Random.Range(0, powerupDrops.Count)];
        if (drop == null) return;

        float roll = Random.Range(0f, 100f);
        if (roll <= powerupDropChance)
            Instantiate(drop, transform.position + Vector3.up, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.2f);
        Gizmos.DrawSphere(transform.position, buffRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, desiredDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fireRange);
    }
}
