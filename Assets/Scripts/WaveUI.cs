using UnityEngine;
using TMPro; 
using System.Collections;

namespace InfimaGames.LowPolyShooterPack
{
    public class WaveUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI waveText;

        [Header("COD Style Effects")]
        public Color normalColor = new Color(0.6f, 0f, 0f); // Dark blood red
        public Color flashColor = Color.white;
        public int flashes = 4;
        public float flashSpeed = 0.2f;

        private void Start()
        {
            // Subscribe to your WaveManager's new event
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveChanged += UpdateWaveDisplay;
                
                // Set initial text (usually 0 until the intro delay finishes)
                if (waveText != null)
                {
                    waveText.text = WaveManager.Instance.currentWave.ToString();
                    waveText.color = normalColor;
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up the event to prevent memory leaks when disconnecting!
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnWaveChanged -= UpdateWaveDisplay;
            }
        }

        private void UpdateWaveDisplay(int newWave)
        {
            if (waveText != null)
            {
                waveText.text = newWave.ToString();
                
                StopAllCoroutines();
                StartCoroutine(FlashText());
            }
        }

        private IEnumerator FlashText()
        {
            // Flash between white and red like CoD Zombies
            for (int i = 0; i < flashes; i++)
            {
                waveText.color = flashColor;
                yield return new WaitForSeconds(flashSpeed);
                
                waveText.color = normalColor;
                yield return new WaitForSeconds(flashSpeed);
            }
        }
    }
}