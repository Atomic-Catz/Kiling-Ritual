using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class OneShotProjectileMarker : MonoBehaviour
    {
        public float maxLifetime = 10f;

        void Start()
        {
            Destroy(gameObject, maxLifetime);
        }

        void OnCollisionEnter(Collision collision)
        {
            TryInstantKill(collision.collider);
        }

        void OnTriggerEnter(Collider other)
        {
            TryInstantKill(other);
        }

        private void TryInstantKill(Collider col)
        {
            if (col == null) return;

            CharacterHealth ch = col.GetComponent<CharacterHealth>();
            if (ch == null)
                ch = col.GetComponentInParent<CharacterHealth>();

            if (ch != null)
            {
                float maybeMax = ch.GetMaxHealth();
                ch.TakeDamage(maybeMax + 1000f);
            }

            Destroy(gameObject);
        }
    }
}
