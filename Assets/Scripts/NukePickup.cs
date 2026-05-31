using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class NukePickup : NetworkBehaviour
    {
        public float rotationSpeed = 90f;
        public Color nukeColor = Color.gold;
        public float despawnTime = 15f;
        
        private bool isCollected = false;

        private void Awake()
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.material.color = nukeColor;
        }

        public void Start()
        {
            if (isServer) Destroy(gameObject, despawnTime);
        }

        private void Update() => transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer || isCollected) return;

            CharacterBehaviour cb = other.GetComponent<CharacterBehaviour>() ?? other.GetComponentInParent<CharacterBehaviour>();
            if (cb != null)
            {
                isCollected = true;
                if (GlobalBuffManager.Instance != null)
                {
                    GlobalBuffManager.Instance.ActivateNuke();
                }
                Destroy(gameObject);
            }
        }
    }
}