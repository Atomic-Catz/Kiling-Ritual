using UnityEngine;
using PurrNet;
using System.Collections;
using System.Reflection;

namespace InfimaGames.LowPolyShooterPack
{
    public class WeaponInteractable : NetworkBehaviour, IInteractable
    {
        [Header("Weapon Data")] 
        public string weaponName = "AK-47";
        public int price = 1500;

        [Tooltip("The index of this gun in the Player's Inventory child list.")]
        public int inventoryIndex;

        // Hooked into your new UI System!
        public string GetInteractText()
        {
            return $"[E] Buy {weaponName} - $ {price}";
        }

        public void Interact(CharacterBehaviour user)
        {
            NetworkBehaviour playerNetwork = user.GetComponent<NetworkBehaviour>();

            if (playerNetwork == null || !playerNetwork.isOwner) return;

            int myPlayerId = playerNetwork.owner.HasValue ? (int)(ulong)playerNetwork.owner.Value.id : 0;
            CmdTryBuyWeapon(myPlayerId);
        }

        [ServerRpc(requireOwnership: false)]
        private void CmdTryBuyWeapon(int buyerId)
        {
            if (ScoreManager.Instance != null && ScoreManager.Instance.SpendPoints(buyerId, price))
            {
                Debug.Log($"[Server] Player {buyerId} successfully purchased {weaponName}!");
                
                GrantWeaponOwnership(buyerId);
                SyncGrantWeapon(buyerId);
            }
            else
            {
                Debug.LogWarning($"[Server] Player {buyerId} cannot afford {weaponName}!");
            }
        }

        [ObserversRpc]
        private void SyncGrantWeapon(int buyerId)
        {
            GrantWeaponOwnership(buyerId);

            Character targetPlayer = GetPlayerById(buyerId);
            if (targetPlayer != null && targetPlayer.isOwner)
            {
                StartCoroutine(SafeAutoEquip(targetPlayer, inventoryIndex));
            }
        }

        // ==========================================
        // LOGIC HELPERS
        // ==========================================

        private void GrantWeaponOwnership(int buyerId)
        {
            Character targetPlayer = GetPlayerById(buyerId);
            if (targetPlayer == null) return;

            Inventory playerInventory = targetPlayer.GetInventory() as Inventory;
            if (playerInventory == null) return;

            Weapon weaponToBuy = playerInventory.GetAllWeapons()[inventoryIndex] as Weapon;
            if (weaponToBuy == null) return;
            
            weaponToBuy.isPurchased = true;

            int gunCount = 0;
            Weapon currentlyHeld = playerInventory.GetEquipped() as Weapon;

            foreach (WeaponBehaviour wb in playerInventory.GetAllWeapons())
            {
                Weapon w = wb as Weapon;
                if (w != null && w.isPurchased)
                {
                    gunCount++;
                }
            }

            // Classic Zombies Rules. 
            // If they have more than 2 guns, drop the one they are CURRENTLY holding!
            if (gunCount > 2 && currentlyHeld != null && currentlyHeld != weaponToBuy)
            {
                currentlyHeld.isPurchased = false;
            }
        }

        private IEnumerator SafeAutoEquip(Character player, int indexToEquip)
        {
            yield return new WaitForSeconds(0.2f);
            
            if (player != null)
            {
                MethodInfo canChangeMethod = player.GetType().GetMethod("CanChangeWeapon", BindingFlags.NonPublic | BindingFlags.Instance);
                if (canChangeMethod != null)
                {
                    yield return new WaitUntil(() => (bool)canChangeMethod.Invoke(player, null) == true);
                }
                
                Inventory playerInventory = player.GetInventory() as Inventory;
                if (playerInventory == null) yield break;

                if (playerInventory.GetEquippedIndex() != indexToEquip)
                {
                    player.StartCoroutine("Equip", indexToEquip);

                    yield return new WaitForSeconds(1.5f);

                    if (playerInventory.GetEquippedIndex() != indexToEquip)
                    {
                        Debug.LogWarning("[Watchdog] Animator event swallowed! Forcefully un-sticking the Holster state.");
                        player.AnimationEndedHolster();
                    }
                }
            }
        }

        private Character GetPlayerById(int id)
        {
            foreach (Character p in FindObjectsOfType<Character>())
            {
                if (p.owner.HasValue && (int)(ulong)p.owner.Value.id == id) return p;
            }
            return null;
        }
    }
}