using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class AmmoPickUp : MonoBehaviour, IInteractable
    {
        [Header("Pickup Amount")]
        public int ammoAmount = 30;

        [Header("Pickup Settings")]
        public bool destroyOnPickup = true;

        [Header("Price")]
        public int cost = 250;

        public string GetInteractText()
        {
            return $"Buy {ammoAmount} Ammo - {cost} Points";
        }

        public void Interact(CharacterBehaviour user)
        {
            // Get the player's ID (you may change this depending on your setup)
            int playerId = 0;

            // Check score
            int score = ScoreManager.Instance.GetScore(playerId);
            if (score < cost)
            {
                Debug.Log("Not enough points!");
                return;
            }

            // Get all weapons
            WeaponBehaviour[] weapons = user.GetInventory().GetAllWeapons();
            if (weapons == null)
                return;

            bool addedAnyAmmo = false;

            foreach (WeaponBehaviour wb in weapons)
            {
                Weapon w = wb as Weapon;
                if (w == null)
                    continue;

                // Skip full weapons
                if (w.IsReserveFull())
                    continue;

                w.AddReserveAmmunition(ammoAmount);
                addedAnyAmmo = true;
            }

            if (!addedAnyAmmo)
            {
                Debug.Log("All weapons already full. Cannot buy ammo.");
                return;
            }

            // Deduct score
            ScoreManager.Instance.AddPoints(playerId, -cost);

            // Destroy object
            if (destroyOnPickup)
                Destroy(gameObject);
        }
    }
}