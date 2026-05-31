using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Collider))]
    public class AmmoPowerUpPickup : NetworkBehaviour
    {
        public int ammoAmount = 300; // Increased amount since it's Max Ammo!
        public Vector3 rotationSpeed = new Vector3(0f, 180f, 0f);
        public float despawnTime = 15f;

        private bool isCollected = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        public void Start()
        {
            if (isServer) Destroy(gameObject, despawnTime);
        }

        private void Update() => transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer || isCollected) return;

            CharacterBehaviour cb = other.GetComponent<CharacterBehaviour>() ?? other.GetComponentInParent<CharacterBehaviour>();
            if (cb != null)
            {
                isCollected = true;
                if (GlobalBuffManager.Instance != null)
                {
                    GlobalBuffManager.Instance.ActivateMaxAmmo(ammoAmount);
                }
                Destroy(gameObject);
            }
        }
    }
}