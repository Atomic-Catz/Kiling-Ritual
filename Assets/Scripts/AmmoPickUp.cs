using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class AmmoPickUp : MonoBehaviour, IInteractable
    {
        [Header("Pickup Amount")]
        public int ammoAmount = 30;

        [Header("Pickup Settings")]
        public bool destroyOnPickup = true;

        public string GetInteractText()
        {
            return $"Pick up {ammoAmount} ammo";
        }

        public void Interact(Character user)
        {
            var inventory = user.GetInventory();
            if (inventory == null) return;

            var weapons = inventory.GetAllWeapons();
            if (weapons == null || weapons.Length == 0) return;

            bool addedAmmo = false;

            foreach (var weaponBehaviour in weapons)
            {
                if (weaponBehaviour is Weapon weapon)
                {
                    // Only add ammo if reserve is not full
                    if (weapon.GetReserveAmmunition() < weapon.GetReserveAmmunitionMax())
                    {
                        weapon.AddReserveAmmunition(ammoAmount);
                        addedAmmo = true;
                    }
                }
            }

            // Only destroy pickup if ammo was actually added
            if (addedAmmo && destroyOnPickup)
                Destroy(gameObject);
        }
    }
}