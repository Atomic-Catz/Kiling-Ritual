using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Renderer))]
    public class InstaKillPickup : MonoBehaviour
    {
        [Tooltip("Duration in seconds the buff lasts when picked up.")]
        public float buffDuration = 10f;

        [Header("Visuals")]
        [Tooltip("Rotation speed in degrees per second.")]
        public Vector3 rotationSpeed = new Vector3(0f, 180f, 0f); // spins around Y axis
        [Tooltip("Color of the pickup.")]
        public Color pickupColor = Color.red;

        [Header("Pickup Settings")]
        [Tooltip("Time in seconds before the pickup disappears if not collected.")]
        public float despawnTime = 15f;
        
        private Renderer objectRenderer;

        private void Awake()
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                objectRenderer.material.color = pickupColor;
            }
            
            // Start auto-despawn timer
            Destroy(gameObject, despawnTime);
        }

        private void Update()
        {
            // Spin the pickup
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            OneShotKillBuff buff = other.GetComponent<OneShotKillBuff>();
            if (buff != null)
            {
                buff.duration = buffDuration;
                buff.Activate();
                Destroy(gameObject);
            }
        }
    }
}