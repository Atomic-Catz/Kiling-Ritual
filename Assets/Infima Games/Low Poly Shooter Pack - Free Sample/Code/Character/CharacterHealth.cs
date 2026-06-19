using UnityEngine;
using System;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class CharacterHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        
        [Header("Downed Settings")]
        [SerializeField] private float maxBleedOutTime = 45f;
        [SerializeField] private float revivedHealth = 50f;

        // --- NETWORK VARIABLES ---
        [SerializeField] private SyncVar<float> currentHealth = new SyncVar<float>(100f);
        public SyncVar<bool> isDowned = new SyncVar<bool>(false);
        public SyncVar<bool> isBeingRevived = new SyncVar<bool>(false);
        public SyncVar<float> bleedOutTime = new SyncVar<float>(45f);

        public event Action OnDeath;
        public event Action<bool> OnDownedStateChanged;
        public event Action<float, float> OnHealthChanged;

        private bool isDead = false;

        private void Awake()
        {
            currentHealth.onChanged += OnHealthSyncChanged;
            isDowned.onChanged += OnDownedSyncChanged;
        }

        private void Start()
        {
            if (isOwner) OnDeath += HandleDeath;
            
            if (isServer)
            {
                currentHealth.value = maxHealth;
                bleedOutTime.value = maxBleedOutTime;
            }
            
            OnHealthChanged?.Invoke(currentHealth.value, maxHealth);
        }

        private void Update()
        {
            // Only the Server manages the bleed-out clock!
            if (isServer && isDowned.value && !isDead)
            {
                // Pause the timer if a teammate is actively reviving them
                if (!isBeingRevived.value)
                {
                    bleedOutTime.value -= Time.deltaTime;
                    if (bleedOutTime.value <= 0f)
                    {
                        Die();
                    }
                }
            }
        }

        private void OnHealthSyncChanged(float newHealth)
        {
            OnHealthChanged?.Invoke(newHealth, maxHealth);
        }

        private void OnDownedSyncChanged(bool downed)
        {
            OnDownedStateChanged?.Invoke(downed);
        }

        public void TakeDamage(float amount)
        {
            // Don't take damage if they are already down or dead
            if (!isServer || isDead || isDowned.value) return; 
            if (amount <= 0) return;

            currentHealth.value -= amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);

            Debug.Log($"[Server Health] {gameObject.name} took {amount} damage. Current health: {currentHealth.value}");

            if (currentHealth.value <= 0)
            {
                EnterDownedState();
            }
        }

        public void Heal(float amount)
        {
            if (!isServer || isDead || isDowned.value) return; 
            if (amount <= 0) return;

            currentHealth.value += amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);

            Debug.Log($"[Server Health] Healed {amount}. Health: {currentHealth.value}");
        }

        private void EnterDownedState()
        {
            isDowned.value = true;
            bleedOutTime.value = maxBleedOutTime;
            Debug.Log($"[Server Health] {gameObject.name} is DOWNED!");

            // Check if the whole team just wiped!
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckGameOverCondition();
            }
        }

        public void RevivePlayer()
        {
            if (!isServer || isDead) return;

            isDowned.value = false;
            isBeingRevived.value = false;
            currentHealth.value = revivedHealth; // Give them some health back
            Debug.Log($"[Server Health] {gameObject.name} was REVIVED!");
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
            isDowned.value = false;

            Debug.Log($"[CharacterHealth] {gameObject.name} died completely!");
            OnDeath?.Invoke();

            // Check if the whole team just wiped!
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckGameOverCondition();
            }
        }
    }
}