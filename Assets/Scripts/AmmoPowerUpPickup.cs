using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Collider))]
    public class AmmoPowerUpPickup : MonoBehaviour
    {
        [Header("Ammo Settings")]
        [Tooltip("How much reserve ammo to give per weapon.")]
        public int ammoAmount = 30;

        [Header("Visuals")]
        public Vector3 rotationSpeed = new Vector3(0f, 180f, 0f);
        //public Color pickupColor = Color.cyan;

        [Header("Pickup Settings")]
        [Tooltip("Seconds before the pickup disappears if not collected.")]
        public float despawnTime = 15f;

        private Renderer rend;

        private void Awake()
        {
            // Ensure trigger
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            // Color
            //rend = GetComponentInChildren<Renderer>();
            //if (rend != null)
                //rend.material.color = pickupColor;

            // Auto-despawn
            Destroy(gameObject, despawnTime);
        }

        private void Update()
        {
            // Spin
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Get player character
            CharacterBehaviour character =
                other.GetComponent<CharacterBehaviour>() ??
                other.GetComponentInParent<CharacterBehaviour>();

            if (character == null)
                return;

            GiveAmmo(character);
            Destroy(gameObject);
        }

        private void GiveAmmo(CharacterBehaviour character)
        {
            WeaponBehaviour[] weapons = character.GetInventory().GetAllWeapons();
            if (weapons == null)
                return;

            foreach (WeaponBehaviour wb in weapons)
            {
                Weapon weapon = wb as Weapon;
                if (weapon == null)
                    continue;

                if (!weapon.IsReserveFull())
                {
                    weapon.AddReserveAmmunition(ammoAmount);
                }
            }

            Debug.Log("AMMO POWER-UP COLLECTED!");
        }
    }
}
