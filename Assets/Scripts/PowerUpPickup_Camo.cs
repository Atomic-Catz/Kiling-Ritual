using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class PowerUpPickup_Camo : MonoBehaviour
    {
        [Header("Visual")]
        public float floatAmplitude = 0.25f;
        public float floatSpeed = 1.0f;
        public float rotationSpeed = 60f;

        [Header("Pickup")]
        public string playerTag = "Player";
        public float buffDuration = 5f;

        Vector3 startPos;

        void Awake()
        {
            startPos = transform.position;
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.yellow;
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

            CamoBuff buff = other.GetComponent<CamoBuff>() ?? other.GetComponentInParent<CamoBuff>();
            if (buff == null)
            {
                CharacterBehaviour cb = other.GetComponent<CharacterBehaviour>() ?? other.GetComponentInParent<CharacterBehaviour>();
                if (cb != null)
                    buff = cb.gameObject.AddComponent<CamoBuff>();
                else
                    buff = other.gameObject.AddComponent<CamoBuff>();
            }

            if (buff != null)
            {
                buff.Grant(buffDuration);
                Destroy(gameObject);
            }
        }
    }
}
