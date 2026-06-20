using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class DownedUIRegister : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the grey panel here.")]
    public GameObject downedUIContainer;
    
    [Tooltip("Drag the TextMeshPro timer text here.")]
    public TMPro.TextMeshProUGUI bleedOutTimerText;

    private void Start()
    {
        // Find all players currently in the scene
        CharacterHealth[] allPlayers = FindObjectsOfType<CharacterHealth>();

        foreach (var player in allPlayers)
        {
            // Only assign this UI to the local player's screen!
            if (player.isOwner)
            {
                player.downedUIContainer = downedUIContainer;
                player.bleedOutTimerText = bleedOutTimerText;

                // Ensure the grey screen matches the player's current state
                downedUIContainer.SetActive(player.isDowned.value);
                
                Debug.Log("[DownedUIRegister] Successfully linked UI to the local player!");
                break; // We found our player, no need to keep looping
            }
        }
    }
}