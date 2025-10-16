using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    public class CanvasSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject canvasPrefab;

        private void Awake()
        {
            if (canvasPrefab == null)
            {
                Debug.LogError("CanvasPrefab not assigned in CanvasSpawner.");
                return;
            }

            GameObject uiCanvas = Instantiate(canvasPrefab);

            // Ensure CharacterHealth exists
            CharacterHealth health = GetComponent<CharacterHealth>();
            if (health == null)
            {
                Debug.LogError("CharacterHealth not found on player prefab!");
                return;
            }

            // Ensure CharacterHealthUI exists
            CharacterHealthUI healthUI = uiCanvas.GetComponentInChildren<CharacterHealthUI>();
            if (healthUI == null)
            {
                Debug.LogError("CharacterHealthUI not found in canvas prefab!");
                return;
            }

            healthUI.Initialize(health);
            Debug.Log("Health UI linked successfully.");
        }
    }
}