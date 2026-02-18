using UnityEngine;

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

        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        var ch = other.GetComponentInParent<InfimaGames.LowPolyShooterPack.CharacterHealth>();
        if (ch != null)
        {
            ch.TakeDamage((float)damage);
            Destroy(gameObject);
            return;
        }

        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        Destroy(gameObject);
    }
}
