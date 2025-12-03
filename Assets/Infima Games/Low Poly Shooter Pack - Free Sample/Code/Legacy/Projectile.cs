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
        Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());

        StartCoroutine(DestroyAfterTimer());
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore other projectiles
        if (collision.gameObject.GetComponent<Projectile>() != null)
            return;

        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            // Apply Insta-Kill if projectile marked
            enemy.TakeDamage(instaKill ? 999999 : damage);

            // Spawn blood effect
            if (bloodImpactPrefabs.Length > 0)
            {
                Instantiate(
                    bloodImpactPrefabs[Random.Range(0, bloodImpactPrefabs.Length)],
                    transform.position,
                    Quaternion.LookRotation(collision.contacts[0].normal)
                );
            }

            Destroy(gameObject);
            return;
        }

        // Handle surface impacts
        if (!destroyOnImpact)
            StartCoroutine(DestroyTimer());
        else
            Destroy(gameObject);

        HandleSurfaceEffects(collision);
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
            collision.transform.gameObject.GetComponent<TargetScript>().isHit = true;
            Destroy(gameObject);
        }
        else if (tag == "ExplosiveBarrel")
        {
            collision.transform.gameObject.GetComponent<ExplosiveBarrelScript>().explode = true;
            Destroy(gameObject);
        }
        else if (tag == "GasTank")
        {
            collision.transform.gameObject.GetComponent<GasTankScript>().isHit = true;
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
        Destroy(gameObject);
    }

    private IEnumerator DestroyAfterTimer()
    {
        yield return new WaitForSeconds(destroyAfter);
        Destroy(gameObject);
    }
}
