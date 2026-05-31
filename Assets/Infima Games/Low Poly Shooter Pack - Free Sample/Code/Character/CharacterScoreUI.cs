using UnityEngine;
using TMPro;
using PurrNet;
using InfimaGames.LowPolyShooterPack; // Added so we can find the Character

public class CharacterScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private int myPlayerId = -1;
    private NetworkBehaviour playerNetwork;

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (scoreText == null)
            Debug.LogError("No TextMeshProUGUI found in ScoreUI.");
    }

    private void Start()
    {
        TryFindPlayerId();
    }

    private void Update()
    {
        // 1. If we haven't found our ID yet, keep trying! (Fixes timing issues)
        if (myPlayerId == -1)
        {
            TryFindPlayerId();
        }

        // 2. Once we have the ID, update the text normally
        if (ScoreManager.Instance != null && myPlayerId != -1)
        {
            scoreText.text = $"$ {ScoreManager.Instance.GetScore(myPlayerId)}";
        }
    }

    private void TryFindPlayerId()
    {
        // Attempt 1: Is the UI a child of the player?
        if (playerNetwork == null)
        {
            playerNetwork = GetComponentInParent<NetworkBehaviour>();
        }

        // Attempt 2: If the UI is detached, find the local player in the scene
        if (playerNetwork == null)
        {
            Character[] allCharacters = FindObjectsOfType<Character>();
            foreach (var character in allCharacters)
            {
                // We only want to link this screen's UI to the character WE control
                if (character.isOwner) 
                {
                    playerNetwork = character;
                    break;
                }
            }
        }

        // If we successfully found the network script, extract the ID
        if (playerNetwork != null && playerNetwork.owner.HasValue)
        {
            myPlayerId = (int)(ulong)playerNetwork.owner.Value.id; 
            Debug.Log($"[Score UI] Successfully linked UI to Player ID: {myPlayerId}");
        }
    }
}