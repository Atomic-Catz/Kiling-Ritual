using UnityEngine;
using System;
using PurrNet; // IMPORT PURRNET CORE

namespace InfimaGames.LowPolyShooterPack
{
    public class CharacterHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        
        // FIX: PurrNet uses SyncVar<T> instead of NetworkSync<T>
        [SerializeField] private SyncVar<float> currentHealth = new SyncVar<float>(100f);

        public event Action OnDeath;
        private bool isDead = false;

        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            // Registering to the event remains exactly the same
            currentHealth.onChanged += OnHealthSyncChanged;
        }

        private void Start()
        {
            // Only the local player who OWNS this character needs to listen to the UI Death Menu popups
            if (isOwner)
            {
                OnDeath += HandleDeath;
            }
            
            // Initialize health values on startup
            if (isServer)
            {
                currentHealth.value = maxHealth;
            }
            
            // Trigger initial UI update frame
            OnHealthChanged?.Invoke(currentHealth.value, maxHealth);
        }

        /// <summary>
        /// Automatically called by PurrNet whenever health updates on the network
        /// </summary>
        private void OnHealthSyncChanged(float newHealth)
        {
            OnHealthChanged?.Invoke(newHealth, maxHealth);

            if (newHealth <= 0f && !isDead)
            {
                Die();
            }
        }

        public void TakeDamage(float amount)
        {
            // CRITICAL GUARD: Only the server is allowed to process actual damage changes
            if (!isServer || isDead) return; 
            if (amount <= 0) return;

            currentHealth.value -= amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);

            Debug.Log($"[Server Health] {gameObject.name} took {amount} damage. Current health: {currentHealth.value}");
        }

        public void Heal(float amount)
        {
            // Healing must also be modified on the server authority layer
            if (!isServer || isDead) return; 
            if (amount <= 0) return;

            currentHealth.value += amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);

            Debug.Log($"[Server Health] Healed {amount}. Health: {currentHealth.value}");
        }

        private void HandleDeath()
        {
            FindObjectOfType<DeathMenu>()?.Show();
        }

        public float GetCurrentHealth() => currentHealth.value;
        public float GetMaxHealth() => maxHealth;

        private void Die()
        {
            if (isDead) return; 
            isDead = true;

            Debug.Log($"[CharacterHealth] {gameObject.name} died!");

            OnDeath?.Invoke();
        }
    }
}