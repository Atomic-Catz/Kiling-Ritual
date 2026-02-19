// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class Inventory : InventoryBehaviour
    {
        #region FIELDS
        
        private WeaponBehaviour[] weapons;
        private WeaponBehaviour equipped;
        private int equippedIndex = -1;
        
        #endregion
        
        #region METHODS
        
        public override void Init(int equippedAtStart = 0)
        {
            // Cache weapons
            weapons = GetComponentsInChildren<WeaponBehaviour>(true);
    
            // Disable everything
            foreach (WeaponBehaviour weapon in weapons)
                weapon.gameObject.SetActive(false);

            // Find the starting gun (Pistol)
            int firstValid = -1;
            for (int i = 0; i < weapons.Length; i++)
            {
                var w = weapons[i] as Weapon;
                if (w != null && w.isPurchased)
                {
                    firstValid = i;
                    break;
                }
            }

            int indexToEquip = firstValid != -1 ? firstValid : equippedAtStart;

            // FORCE INITIALIZATION:
            // We set the references BEFORE activating so the UI sees them immediately
            equippedIndex = indexToEquip;
            equipped = weapons[equippedIndex];
    
            // Now activate it. This triggers Weapon.Awake()
            equipped.gameObject.SetActive(true);
        }
        
        public override WeaponBehaviour Equip(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length) return equipped;

            var weaponScript = weapons[index] as Weapon;
    
            // Only allow equipping if it's actually purchased
            if (weaponScript != null && !weaponScript.isPurchased)
                return equipped;

            // Standard swap logic...
            if (equipped != null) equipped.gameObject.SetActive(false);
            equippedIndex = index;
            equipped = weapons[equippedIndex];
            equipped.gameObject.SetActive(true);

            return equipped;
        }
        
        #endregion

        #region Getters

        public override int GetLastIndex()
        {
            // Look backwards for the next purchased weapon
            int checkIndex = equippedIndex;
            for (int i = 0; i < weapons.Length; i++)
            {
                checkIndex--;
                if (checkIndex < 0)
                    checkIndex = weapons.Length - 1;

                var w = weapons[checkIndex] as Weapon;
                if (w != null && w.isPurchased)
                    return checkIndex;
            }

            return equippedIndex;
        }

        public override int GetNextIndex()
        {
            // Look forwards for the next purchased weapon
            int checkIndex = equippedIndex;
            for (int i = 0; i < weapons.Length; i++)
            {
                checkIndex = (checkIndex + 1) % weapons.Length;

                var w = weapons[checkIndex] as Weapon;
                if (w != null && w.isPurchased)
                    return checkIndex;
            }

            return equippedIndex;
        }

        public override WeaponBehaviour GetEquipped() => equipped;
        public override int GetEquippedIndex() => equippedIndex;
        public override WeaponBehaviour[] GetAllWeapons() => weapons;

        #endregion
    }
}