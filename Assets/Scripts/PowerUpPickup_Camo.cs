using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class PowerUpPickup_Camo : NetworkBehaviour
    {
        public float floatAmplitude = 0.25f;
        public float floatSpeed = 1.0f;
        public float rotationSpeed = 60f;
        public float buffDuration = 10f; 
        public float despawnTime = 15f;
        
        private Vector3 startPos;
        private bool isCollected = false;

        void Awake()
        {
            startPos = transform.position;
            var rend = GetComponent<Renderer>();
            if (rend != null) rend.material.color = Color.purple;
        }

        private void Start()
        {
            if (isServer) Destroy(gameObject, despawnTime);
        }

        void Update()
        {
            transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * Mathf.PI * 2f * floatSpeed) * floatAmplitude);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!isServer || isCollected) return;

            CharacterBehaviour cb = other.GetComponent<CharacterBehaviour>() ?? other.GetComponentInParent<CharacterBehaviour>();
            if (cb != null)
            {
                isCollected = true;
                if (GlobalBuffManager.Instance != null)
                {
                    GlobalBuffManager.Instance.ActivateCamo(buffDuration);
                }
                Destroy(gameObject);
            }
        }
    }
}