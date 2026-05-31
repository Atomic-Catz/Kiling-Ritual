using System.Collections;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using PurrNet; 

public class Projectile : MonoBehaviour
{
    [Range(5, 100)]
    public float destroyAfter = 10f;

    public bool destroyOnImpact = false;
    public float minDestroyTime = 0.5f;
    public float maxDestroyTime = 2f;

    [Tooltip("Damage")]
    public int damage = 25;

    [Header("Impact Effect Prefabs")]
    public Transform[] bloodImpactPrefabs;
    public Transform[] metalImpactPrefabs;
    public Transform[] dirtImpactPrefabs;
    public Transform[] concreteImpactPrefabs;

    [HideInInspector]
    public bool instaKill = false;

    private float speed = 400f; 
    private Collider ownerCollider;
    private bool isServerInstance;
    private bool isInitialized = false;

    // Tracks the unique network ID of the player who fired
    private int attackerPlayerId;

    // Receive the attacker ID when the projectile is spawned
    public void InitializeProjectile(Collider bulletOwner, int attackerId, bool isInstaKill)
    {
        ownerCollider = bulletOwner;
        attackerPlayerId = attackerId; 
        instaKill = isInstaKill;

        if (ownerCollider != null)
        {
            Physics.IgnoreCollision(ownerCollider, GetComponent<Collider>(), true);
        }
    }

    private void Start()
    {
        isServerInstance = NetworkManager.main != null && NetworkManager.main.isServer;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var gameMode = ServiceLocator.Current.Get<IGameModeService>();
        if (gameMode != null && gameMode.GetPlayerCharacter() != null)
        {
            var weapon = gameMode.GetPlayerCharacter().GetInventory().GetEquipped() as Weapon;
            if (weapon != null)
            {
                System.Reflection.FieldInfo field = typeof(Weapon).GetField("projectileImpulse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) speed = (float)field.GetValue(weapon);
            }
        }

        isInitialized = true;
        StartCoroutine(DestroyAfterTimer());
    }

    private void Update()
    {
        if (!isInitialized) return;

        float moveDistance = speed * Time.deltaTime;
        Vector3 moveDirection = transform.forward;

        if (Physics.Raycast(transform.position, moveDirection, out RaycastHit hit, moveDistance + 0.1f))
        {
            if (hit.collider.gameObject.GetComponent<Projectile>() == null && 
                (ownerCollider == null || hit.collider.gameObject != ownerCollider.gameObject))
            {
                transform.position = hit.point;
                EvaluateImpact(hit.collider.gameObject, hit);
                return;
            }
        }

        transform.Translate(Vector3.forward * moveDistance);
    }

    private void EvaluateImpact(GameObject targetHit, RaycastHit hit)
    {
        EnemyAI enemy = targetHit.GetComponent<EnemyAI>();
        CommanderAI commander = targetHit.GetComponent<CommanderAI>();

        if (enemy != null)
        {
            if (isServerInstance)
            {
                // Pass attackerPlayerId along to the enemy component
                enemy.TakeDamage(instaKill ? 999999 : damage, attackerPlayerId);
            }
            ExecuteManualHit(hit, bloodImpactPrefabs);
            return;
        }
        else if (commander != null)
        {
            if (isServerInstance)
            {
                // FIXED: Explicitly matches your Commander's float requirement for damage parameter
                float finalDamage = instaKill ? 999999f : (float)damage;
                commander.TakeDamage(finalDamage, attackerPlayerId);
            }
            ExecuteManualHit(hit, bloodImpactPrefabs);
            return;
        }

        string tag = targetHit.tag;

        if (tag == "Blood")
            ExecuteManualHit(hit, bloodImpactPrefabs);
        else if (tag == "Metal")
            ExecuteManualHit(hit, metalImpactPrefabs);
        else if (tag == "Dirt")
            ExecuteManualHit(hit, dirtImpactPrefabs);
        else if (tag == "Concrete")
            ExecuteManualHit(hit, concreteImpactPrefabs);
        else if (tag == "Target")
        {
            var target = targetHit.GetComponent<TargetScript>();
            if (target != null) target.isHit = true;
            DestroyProjectileInstance();
        }
        else if (tag == "ExplosiveBarrel")
        {
            var barrel = targetHit.GetComponent<ExplosiveBarrelScript>();
            if (barrel != null) barrel.explode = true;
            DestroyProjectileInstance();
        }
        else if (tag == "GasTank")
        {
            var tank = targetHit.GetComponent<GasTankScript>();
            if (tank != null) tank.isHit = true;
            DestroyProjectileInstance();
        }
        else
        {
            if (!destroyOnImpact)
                StartCoroutine(DestroyTimer());
            else
                DestroyProjectileInstance();
        }
    }

    private void ExecuteManualHit(RaycastHit hit, Transform[] prefabs)
    {
        if (prefabs != null && prefabs.Length > 0)
        {
            Instantiate(
                prefabs[Random.Range(0, prefabs.Length)],
                hit.point,
                Quaternion.LookRotation(hit.normal)
            );
        }
        DestroyProjectileInstance();
    }

    private void DestroyProjectileInstance()
    {
        if (isServerInstance)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator DestroyTimer()
    {
        yield return new WaitForSeconds(Random.Range(minDestroyTime, maxDestroyTime));
        DestroyProjectileInstance();
    }

    private IEnumerator DestroyAfterTimer()
    {
        yield return new WaitForSeconds(destroyAfter);
        DestroyProjectileInstance();
    }
}