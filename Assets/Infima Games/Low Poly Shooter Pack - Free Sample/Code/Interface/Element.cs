// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    /// <summary>
    /// Interface Element. Refactored to handle network dependency injection.
    /// </summary>
    public abstract class Element : MonoBehaviour
    {
        #region FIELDS
        
        /// <summary>
        /// Game Mode Service.
        /// </summary>
        protected IGameModeService gameModeService;
        
        /// <summary>
        /// Player Character.
        /// </summary>
        protected CharacterBehaviour playerCharacter;
        /// <summary>
        /// Player Character Inventory.
        /// </summary>
        protected InventoryBehaviour playerCharacterInventory;

        /// <summary>
        /// Equipped Weapon.
        /// </summary>
        protected WeaponBehaviour equippedWeapon;
        
        #endregion

        #region PROPERTIES

        /// <summary>
        /// Prevents Awake lookup if the CanvasSpawner has injected a specific character reference.
        /// </summary>
        private bool hasExplicitOwner;

        #endregion

        #region UNITY

        /// <summary>
        /// Awake.
        /// </summary>
        protected virtual void Awake()
        {
            // If a network spawner already manually initialized this element, skip the global lookup.
            if (hasExplicitOwner) return;

            // Get Game Mode Service. Very useful to get Game Mode references.
            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            
            // Get Player Character fallback.
            if (gameModeService != null)
            {
                playerCharacter = gameModeService.GetPlayerCharacter();
                if (playerCharacter != null)
                {
                    playerCharacterInventory = playerCharacter.GetInventory();
                }
            }
        }
        
        /// <summary>
        /// Update.
        /// </summary>
        private void Update()
        {
            // Update inventory reference dynamically if it's currently missing but we have a character
            if (playerCharacterInventory == null && playerCharacter != null)
            {
                playerCharacterInventory = playerCharacter.GetInventory();
            }

            // Ignore if we don't have an Inventory.
            if (playerCharacterInventory == null)
                return;

            // Get Equipped Weapon.
            equippedWeapon = playerCharacterInventory.GetEquipped();
            
            // Tick child script logic (text fields, crosshairs, images, etc.)
            Tick();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Explicit network initialization. Forces this UI element to read data 
        /// from the specific local character machine instead of using a global lookup.
        /// </summary>
        public void SetupNetworkPlayer(CharacterBehaviour localOwner)
        {
            hasExplicitOwner = true;
            playerCharacter = localOwner;
            
            if (playerCharacter != null)
            {
                playerCharacterInventory = playerCharacter.GetInventory();
            }
        }

        /// <summary>
        /// Tick.
        /// </summary>
        protected virtual void Tick() {}

        #endregion
    }
}