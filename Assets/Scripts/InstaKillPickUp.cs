using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Renderer))]
    public class InstaKillPickup : NetworkBehaviour
    {
        public float buffDuration = 30f;
        public Vector3 rotationSpeed = new Vector3(0f, 180f, 0f);
        public Color pickupColor = Color.red;
        public float despawnTime = 15f;

        private bool isCollected = false;

        private void Awake()
        {
            Renderer objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null) objectRenderer.material.color = pickupColor;
        }

        public void Start()
        {
            // Only the server manages despawning to prevent network desyncs
            if (isServer) Destroy(gameObject, despawnTime);
        }

        private void Update() => transform.Rotate(rotationSpeed * Time.deltaTime);

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer || isCollected) return;

            CharacterBehaviour cb = other.GetComponent<CharacterBehaviour>() ?? other.GetComponentInParent<CharacterBehaviour>();
            if (cb != null)
            {
                isCollected = true;
                if (GlobalBuffManager.Instance != null)
                {
                    GlobalBuffManager.Instance.ActivateInstaKill(buffDuration);
                }
                Destroy(gameObject);
            }
        }
    }
}