using UnityEngine;
using PurrNet; // REQUIRED to check the global server state

public class SimpleProjectile : MonoBehaviour
{
    [Tooltip("Speed in units/second")]
    public float speed = 25f;

    [Tooltip("Damage (integer) applied to CharacterHealth or via SendMessage fallback.")]
    public int damage = 20;

    [Tooltip("How long until the projectile auto-destroys.")]
    public float lifeTime = 6f;

    [Tooltip("The GameObject that fired this projectile (optional).")]
    public GameObject owner;

    private float spawnTime;

    private void Start()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        if (Time.time - spawnTime > lifeTime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // 1. Existing check: Don't hit the person who fired the shot
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        // 2. NEW: If the owner is an ENEMY, ignore other ENEMIES
        if (owner != null && owner.CompareTag("Enemy") && other.CompareTag("Enemy"))
        {
            return; // Pass through allies like a ghost
        }

        // ==========================================
        // MULTIPLAYER FIX: CLIENT VISUAL DESTRUCTION
        // ==========================================
        // If we are a client, we ONLY destroy the visual effect on impact. 
        // We DO NOT calculate damage, otherwise the player takes 2x, 3x, or 4x damage!
        if (NetworkManager.main != null && !NetworkManager.main.isServer)
        {
            Destroy(gameObject); 
            return;
        }

        // ==========================================
        // SERVER ONLY DAMAGE LOGIC
        // ==========================================
        
        // 3. Check if we hit the Player
        var ch = other.GetComponentInParent<InfimaGames.LowPolyShooterPack.CharacterHealth>();
        if (ch != null)
        {
            ch.TakeDamage((float)damage);
            Destroy(gameObject);
            return;
        }

        // 4. Hit everything else (Zombies hit by player, or Boss hitting walls/player)
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        Destroy(gameObject);
    }
}