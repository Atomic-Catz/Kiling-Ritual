using UnityEngine;
using System;

namespace InfimaGames.LowPolyShooterPack
{
    public class CharacterHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        private float currentHealth;

        public event Action OnDeath;
        private bool isDead = false;

        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return; // Already dead, ignore damage
            if (amount <= 0) return;

            currentHealth -= amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            Debug.Log($"[CharacterHealth] {gameObject.name} took {amount} damage. Current health: {currentHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return; // Can't heal a dead character
            if (amount <= 0) return;

            float before = currentHealth;

            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            Debug.Log($"[CharacterHealth] Healed {amount}. Health: {before} -> {currentHealth}");

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Start()
        {
            var health = GetComponent<CharacterHealth>();
            if (health != null)
                health.OnDeath += HandleDeath;
        }

        private void HandleDeath()
        {
            FindObjectOfType<DeathMenu>()?.Show();
        }


        public float GetCurrentHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;

        private void Die()
        {
            if (isDead) return; // Prevent multiple calls
            isDead = true;

            Debug.Log("[CharacterHealth] Character died!");

            OnDeath?.Invoke();
        }
    }
}