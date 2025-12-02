using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class PowerUpPickup_OneShot : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("Float amplitude in world units.")]
        public float floatAmplitude = 0.25f;
        [Tooltip("Float speed (cycles per second).")]
        public float floatSpeed = 1.0f;
        [Tooltip("Rotation speed in degrees/sec.")]
        public float rotationSpeed = 60f;

        [Header("Pickup")]
        [Tooltip("Optional tag that the player uses. If empty, script will search for OneShotKillBuff on the colliding object.")]
        public string playerTag = "Player";

        private Vector3 startPos;

        void Awake()
        {
            startPos = transform.position;
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.blue;
            }
        }

        void Update()
        {
            transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * Mathf.PI * 2f * floatSpeed) * floatAmplitude);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(playerTag) && other.gameObject.tag != playerTag)
                return;

            OneShotKillBuff buff = other.GetComponent<OneShotKillBuff>() ?? other.GetComponentInParent<OneShotKillBuff>();
            if (buff == null)
            {
                CharacterBehaviour cb = other.GetComponent<CharacterBehaviour>() ?? other.GetComponentInParent<CharacterBehaviour>();
                if (cb != null)
                    buff = cb.gameObject.AddComponent<OneShotKillBuff>();
            }

            if (buff != null)
            {
                buff.Grant(5f);
                Destroy(gameObject);
                return;
            }
        }
    }
}
