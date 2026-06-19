using UnityEngine;
using PurrNet; // Added PurrNet

namespace InfimaGames.LowPolyShooterPack
{
    // 1. Changed to NetworkBehaviour
    public class AmmoPickUp : NetworkBehaviour, IInteractable 
    {
        [Header("Pickup Amount")]
        public int ammoAmount = 30;

        [Header("Pickup Settings")]
        public bool destroyOnPickup = true;

        [Header("Price")]
        public int cost = 250;

        public string GetInteractText()
        {
            // 2. Added the [E] prompt to match your UI
            return $"[E] Buy Ammo - $ {cost}";
        }

        public void Interact(CharacterBehaviour user)
        {
            NetworkBehaviour playerNetwork = user.GetComponent<NetworkBehaviour>();
            if (playerNetwork == null || !playerNetwork.isOwner) return;

            // PRE-CHECK: Let's do your local check first to see if they even need ammo!
            WeaponBehaviour[] weapons = user.GetInventory().GetAllWeapons();
            if (weapons == null) return;

            bool needsAmmo = false;
            foreach (WeaponBehaviour wb in weapons)
            {
                Weapon w = wb as Weapon;
                if (w != null && !w.IsReserveFull())
                {
                    needsAmmo = true;
                    break;
                }
            }

            if (!needsAmmo)
            {
                Debug.Log("All weapons already full. Cannot buy ammo.");
                return;
            }

            // Get ID and ask the server to process the purchase
            int myPlayerId = playerNetwork.owner.HasValue ? (int)(ulong)playerNetwork.owner.Value.id : 0;
            CmdTryBuyAmmo(myPlayerId);
        }

        // 3. SERVER LOGIC: Handle the points here
        [ServerRpc(requireOwnership: false)]
        private void CmdTryBuyAmmo(int buyerId)
        {
            // SpendPoints ensures they actually have the money
            if (ScoreManager.Instance != null && ScoreManager.Instance.SpendPoints(buyerId, cost))
            {
                // Tell clients to apply the ammo
                SyncGrantAmmo(buyerId);

                // Destroy object on the server if it's a one-time drop
                if (destroyOnPickup)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.Log("Not enough points!");
            }
        }

        // 4. SYNC LOGIC: Give the player the ammo across the network
        [ObserversRpc]
        private void SyncGrantAmmo(int buyerId)
        {
            Character targetPlayer = GetPlayerById(buyerId);
            if (targetPlayer == null) return;

            Inventory playerInventory = targetPlayer.GetInventory() as Inventory;
            if (playerInventory == null) return;

            // Apply your ammo logic!
            foreach (WeaponBehaviour wb in playerInventory.GetAllWeapons())
            {
                Weapon w = wb as Weapon;
                if (w == null) continue;

                if (!w.IsReserveFull())
                {
                    w.AddReserveAmmunition(ammoAmount);
                }
            }
        }

        // Helper to find the right player
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