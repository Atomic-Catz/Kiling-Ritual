using UnityEngine;
using TMPro;
using InfimaGames.LowPolyShooterPack;

public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshPro element that displays the prompt.")]
    public TextMeshProUGUI promptText;

    private Character localPlayer;

    private void Start()
    {
        // Hide it by default
        if (promptText != null) promptText.gameObject.SetActive(false);

        // Find the local player
        Character[] players = FindObjectsOfType<Character>();
        foreach (var p in players)
        {
            if (p.isOwner)
            {
                localPlayer = p;
                break;
            }
        }
    }

    private void Update()
    {
        // Safety checks
        if (localPlayer == null || promptText == null) return;

        // Don't show prompts if the player is dead, downed, or in the pause menu
        CharacterHealth health = localPlayer.GetComponent<CharacterHealth>();
        if (localPlayer.isMenuOpen || (health != null && health.isDowned.value))
        {
            promptText.gameObject.SetActive(false);
            return;
        }

        // Ask the Character script what we are currently looking at
        IInteractable target = localPlayer.GetCurrentInteractable();

        if (target != null)
        {
            // Get the text from the object and show it
            promptText.text = target.GetInteractText();
            promptText.gameObject.SetActive(true);
        }
        else
        {
            // We aren't looking at anything, hide the text
            promptText.gameObject.SetActive(false);
        }
    }
}