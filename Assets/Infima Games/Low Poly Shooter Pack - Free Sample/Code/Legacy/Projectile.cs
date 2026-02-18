using System.Collections;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using Random = UnityEngine.Random;

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

    private void Start()
    {
        // Ignore collisions with player
        var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
        if (gameModeService != null && gameModeService.GetPlayerCharacter() != null)
        {
            Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());
        }

        StartCoroutine(DestroyAfterTimer());
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore other projectiles
        if (collision.gameObject.GetComponent<Projectile>() != null)
            return;

        // Try to get either script
        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
        CommanderAI commander = collision.gameObject.GetComponent<CommanderAI>();

        if (enemy != null)
        {
            // Damage the basic enemy
            enemy.TakeDamage(instaKill ? 999999 : damage);
            
            // Shared effects
            ExecuteEnemyHit(collision);
            return;
        }
        else if (commander != null)
        {
            // Damage the commander (using float for his overloaded method)
            commander.TakeDamage(instaKill ? 999999f : (float)damage);
            
            // Shared effects
            ExecuteEnemyHit(collision);
            return;
        }

        // --- If we reach here, we didn't hit an enemy ---

        if (!destroyOnImpact)
            StartCoroutine(DestroyTimer());
        else
            Destroy(gameObject);

        HandleSurfaceEffects(collision);
    }

    /// <summary>
    /// Handles the visual blood effect and destroys the bullet.
    /// Used for both Basic Enemies and Commanders.
    /// </summary>
    private void ExecuteEnemyHit(Collision collision)
    {
        if (bloodImpactPrefabs.Length > 0)
        {
            Instantiate(
                bloodImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)],
                transform.position,
                Quaternion.LookRotation(collision.contacts[0].normal)
            );
        }
        Destroy(gameObject);
    }

    private void HandleSurfaceEffects(Collision collision)
    {
        string tag = collision.transform.tag;

        if (tag == "Blood")
            SpawnImpact(bloodImpactPrefabs, collision);
        else if (tag == "Metal")
            SpawnImpact(metalImpactPrefabs, collision);
        else if (tag == "Dirt")
            SpawnImpact(dirtImpactPrefabs, collision);
        else if (tag == "Concrete")
            SpawnImpact(concreteImpactPrefabs, collision);
        else if (tag == "Target")
        {
            var target = collision.transform.gameObject.GetComponent<TargetScript>();
            if(target != null) target.isHit = true;
            Destroy(gameObject);
        }
        else if (tag == "ExplosiveBarrel")
        {
            var barrel = collision.transform.gameObject.GetComponent<ExplosiveBarrelScript>();
            if(barrel != null) barrel.explode = true;
            Destroy(gameObject);
        }
        else if (tag == "GasTank")
        {
            var tank = collision.transform.gameObject.GetComponent<GasTankScript>();
            if(tank != null) tank.isHit = true;
            Destroy(gameObject);
        }
    }

    private void SpawnImpact(Transform[] prefabs, Collision collision)
    {
        if (prefabs.Length == 0)
            return;

        Instantiate(prefabs[Random.Range(0, prefabs.Length)],
            transform.position,
            Quaternion.LookRotation(collision.contacts[0].normal));

        Destroy(gameObject);
    }

    private IEnumerator DestroyTimer()
    {
        yield return new WaitForSeconds(Random.Range(minDestroyTime, maxDestroyTime));
        if(gameObject != null) Destroy(gameObject);
    }

    private IEnumerator DestroyAfterTimer()
    {
        yield return new WaitForSeconds(destroyAfter);
        if(gameObject != null) Destroy(gameObject);
    }
}