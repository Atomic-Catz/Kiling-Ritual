using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class PowerUpPickup_TripleScore : MonoBehaviour
    {
        [Header("Visual")]
        public float floatAmplitude = 0.25f;
        public float floatSpeed = 1.0f;
        public float rotationSpeed = 60f;

        [Header("Pickup")]
        [Tooltip("If you tag the player in the scene, set it here. Leave empty to skip tag check.")]
        public string playerTag = "Player";

        [Tooltip("How long the triple-score buff lasts (seconds)")]
        public float buffDuration = 5f;

        Vector3 startPos;

        void Awake()
        {
            startPos = transform.position;
            var rend = GetComponent<Renderer>();
            if (rend != null) rend.material.color = Color.green;
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

            TripleScoreBuff buff = other.GetComponent<TripleScoreBuff>() ?? other.GetComponentInParent<TripleScoreBuff>();
            if (buff == null)
            {
                CharacterBehaviour cb = other.GetComponent<CharacterBehaviour>() ?? other.GetComponentInParent<CharacterBehaviour>();
                if (cb != null)
                {
                    buff = cb.gameObject.AddComponent<TripleScoreBuff>();
                }
                else
                {
                    buff = other.gameObject.AddComponent<TripleScoreBuff>();
                }
            }

            if (buff != null)
            {
                buff.Grant(buffDuration);
                Destroy(gameObject);
            }
        }
    }
}
