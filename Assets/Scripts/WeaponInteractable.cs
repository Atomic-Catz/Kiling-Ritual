using UnityEngine;
using InfimaGames.LowPolyShooterPack;
using System.Collections.Generic;

namespace InfimaGames.LowPolyShooterPack
{
    public class WeaponInteractable : MonoBehaviour, IInteractable
    {
        [Header("Weapon Data")]
        public string weaponName = "AK-47";
        public int price = 1500;

        [Tooltip("The index of this gun in the Player's Inventory child list.")]
        public int inventoryIndex;

        public string GetInteractText()
        {
            return $"Buy {weaponName} - {price} Points";
        }

        public void Interact(CharacterBehaviour user)
        {
            int playerId = 0;
            
            // 1. Access the specific Character and Inventory
            Character playerCharacter = user as Character;
            Inventory playerInventory = user.GetInventory() as Inventory;

            if (playerCharacter == null || playerInventory == null)
                return;

            // 2. Check Score
            int score = ScoreManager.Instance.GetScore(playerId);
            if (score < price)
            {
                Debug.Log("Not enough points!");
                return;
            }

            // 3. Get all weapons to check what we currently own
            WeaponBehaviour[] allWeaponBehaviours = playerInventory.GetAllWeapons();
            List<Weapon> purchasedWeapons = new List<Weapon>();

            foreach (WeaponBehaviour wb in allWeaponBehaviours)
            {
                Weapon w = wb as Weapon;
                if (w != null && w.isPurchased)
                {
                    purchasedWeapons.Add(w);
                }
            }

            // 4. Get the weapon the player is trying to buy
            Weapon weaponToBuy = allWeaponBehaviours[inventoryIndex] as Weapon;
            if (weaponToBuy == null) return;

            // If player already has this EXACT weapon, do nothing (or refill ammo)
            if (weaponToBuy.isPurchased)
            {
                Debug.Log("You already have this weapon!");
                return;
            }

            // 5. TWO-WEAPON LIMIT LOGIC
            // If the player already has 2 weapons, we must "remove" the one in their hand
            if (purchasedWeapons.Count >= 2)
            {
                Weapon currentlyHeld = playerInventory.GetEquipped() as Weapon;
                if (currentlyHeld != null)
                {
                    currentlyHeld.isPurchased = false;
                    currentlyHeld.gameObject.SetActive(false);
                    Debug.Log($"Replacing {currentlyHeld.weaponName} with {weaponName}");
                }
            }

            // 6. Complete the Transaction
            ScoreManager.Instance.AddPoints(playerId, -price);
            
            // Unlock the new gun
            weaponToBuy.isPurchased = true;

            // 7. Swap to the new weapon immediately
            // We use the Character's Equip coroutine to handle animations/logic
            playerCharacter.StartCoroutine("Equip", inventoryIndex);

            Debug.Log($"Purchased {weaponName}. Weapons carried: " + (purchasedWeapons.Count + 1));
        }
    }
}