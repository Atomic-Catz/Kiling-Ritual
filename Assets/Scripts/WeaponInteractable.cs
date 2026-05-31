using UnityEngine;
using PurrNet; // IMPORT PURRNET
using System.Collections.Generic;

namespace InfimaGames.LowPolyShooterPack
{
    public class WeaponInteractable : NetworkBehaviour, IInteractable
    {
        [Header("Weapon Data")] public string weaponName = "AK-47";
        public int price = 1500;

        [Tooltip("The index of this gun in the Player's Inventory child list.")]
        public int inventoryIndex;

        public string GetInteractText()
        {
            return $"Buy {weaponName} - $ {price}";
        }

        public void Interact(CharacterBehaviour user)
        {
            // 1. Get the network component of the player who just pressed 'Interact'
            NetworkBehaviour playerNetwork = user.GetComponent<NetworkBehaviour>();

            // 2. We ONLY want the local player's computer to send this request, 
            // otherwise 4 computers will try to buy the gun at the exact same time!
            if (playerNetwork == null || !playerNetwork.isOwner) return;

            // 3. Extract their exact Network ID
            int myPlayerId = playerNetwork.owner.HasValue ? (int)(ulong)playerNetwork.owner.Value.id : 0;

            // 4. Send the purchase request up to the Server!
            CmdTryBuyWeapon(myPlayerId);
        }

        // RequireOwnership = false allows ANY player to click this wall-buy, 
        // not just the Server who spawned the Trader!
        [ServerRpc(requireOwnership: false)]
        private void CmdTryBuyWeapon(int buyerId)
        {
            // 5. The Server checks the bank account
            if (ScoreManager.Instance != null && ScoreManager.Instance.SpendPoints(buyerId, price))
            {
                Debug.Log($"[Server] Player {buyerId} successfully purchased {weaponName}!");

                // 6. Bank approved! Tell ALL clients to update this player's inventory
                SyncGrantWeapon(buyerId);
            }
            else
            {
                Debug.LogWarning($"[Server] Player {buyerId} cannot afford {weaponName}!");
                // You could add a rejection buzzer sound here!
            }
        }

        [ObserversRpc]
        private void SyncGrantWeapon(int buyerId)
        {
            // 1. Find the specific player who bought the weapon
            Character targetPlayer = null;
            Character[] allPlayers = FindObjectsOfType<Character>();

            foreach (Character p in allPlayers)
            {
                if (p.owner.HasValue && (int)(ulong)p.owner.Value.id == buyerId)
                {
                    targetPlayer = p;
                    break;
                }
            }

            if (targetPlayer == null) return;

            Inventory playerInventory = targetPlayer.GetInventory() as Inventory;
            if (playerInventory == null) return;

            // 2. Gather the player's current weapons
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

            Weapon weaponToBuy = allWeaponBehaviours[inventoryIndex] as Weapon;
            if (weaponToBuy == null || weaponToBuy.isPurchased) return;

            // 3. TWO-WEAPON LIMIT LOGIC (Fixed)
            if (purchasedWeapons.Count >= 2)
            {
                Weapon currentlyHeld = playerInventory.GetEquipped() as Weapon;
                if (currentlyHeld != null)
                {
                    // Revoke the old weapon
                    currentlyHeld.isPurchased = false;

                    // WE PUT THIS BACK: We MUST instantly hide the old gun, otherwise 
                    // the LPSP internal logic crashes and leaves you with empty hands!
                    currentlyHeld.gameObject.SetActive(false);
                }
            }

            // 4. Unlock the new gun in their inventory
            weaponToBuy.isPurchased = true;

            // 5. Equip the new gun
            if (targetPlayer.isOwner)
            {
                // If this is my player, run the smooth FPS logic
                targetPlayer.StartCoroutine("Equip", inventoryIndex);
            }
            else
            {
                // If I am looking at another player, just snap the models instantly
                Weapon currentlyHeld = playerInventory.GetEquipped() as Weapon;
                if (currentlyHeld != null && currentlyHeld != weaponToBuy)
                {
                    currentlyHeld.gameObject.SetActive(false);
                }

                weaponToBuy.gameObject.SetActive(true);
            }
        }
    }
}
