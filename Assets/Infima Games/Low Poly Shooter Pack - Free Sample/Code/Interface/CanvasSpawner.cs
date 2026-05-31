using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack.Interface
{
    public class CanvasSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject canvasPrefab;

        private void Start()
        {
            // CRITICAL NETWORK GUARD: Only run for the player window that actually owns this object.
            var characterBehaviour = GetComponent<CharacterBehaviour>();
            if (characterBehaviour != null && !characterBehaviour.isOwner)
            {
                return;
            }

            if (canvasPrefab == null)
            {
                Debug.LogError("CanvasPrefab not assigned in CanvasSpawner.");
                return;
            }

            // Spawn the UI Canvas layout
            GameObject uiCanvas = Instantiate(canvasPrefab);

            // 1. LINK HEALTH UI
            CharacterHealth health = GetComponent<CharacterHealth>();
            if (health != null)
            {
                CharacterHealthUI healthUI = uiCanvas.GetComponentInChildren<CharacterHealthUI>();
                if (healthUI != null)
                {
                    healthUI.Initialize(health);
                }
            }

            // 2. LINK INFIMA WEAPON / AMMO / CROSSHAIR ELEMENTS
            if (characterBehaviour != null)
            {
                // Find all UI scripts utilizing our custom Element base class
                Element[] uiElements = uiCanvas.GetComponentsInChildren<Element>(true);
                foreach (Element element in uiElements)
                {
                    // Pass this client's unique character component instance down to the UI element
                    element.SetupNetworkPlayer(characterBehaviour);
                }
                Debug.Log($"[CanvasSpawner] Successfully injected local client references into {uiElements.Length} HUD elements.");
            }
        }
    }
}