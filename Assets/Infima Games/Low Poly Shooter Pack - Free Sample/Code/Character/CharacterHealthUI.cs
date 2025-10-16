using UnityEngine;
using TMPro;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    public class CharacterHealthUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI healthText;

        private CharacterHealth characterHealth;

        public void Initialize(CharacterHealth health)
        {
            characterHealth = health;

            // Auto-find TMP Text if not assigned
            if (healthText == null)
                healthText = GetComponentInChildren<TextMeshProUGUI>();

            if (healthText == null)
            {
                Debug.LogError("No TextMeshProUGUI found in CharacterHealthUI.");
                return;
            }

            characterHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(characterHealth.GetCurrentHealth(), characterHealth.GetMaxHealth());
        }

        private void UpdateHealthUI(float current, float max)
        {
            if (healthText != null)
                healthText.text = $"+{current:0}";
        }

        private void OnDestroy()
        {
            if (characterHealth != null)
                characterHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }
}