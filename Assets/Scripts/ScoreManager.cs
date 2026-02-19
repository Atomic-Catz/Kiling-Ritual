using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private Dictionary<int, int> playerScores = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void AddPoints(int playerId, int points)
    {
        if (!playerScores.ContainsKey(playerId))
            playerScores[playerId] = 0;

        playerScores[playerId] += points;
        Debug.Log($"Player {playerId} Score: {playerScores[playerId]}");
    }

    public bool SpendPoints(int playerId, int amount)
    {
        // First, check if the player even exists in our dictionary
        if (!playerScores.ContainsKey(playerId))
            playerScores[playerId] = 0;

        // Check if they have enough money
        if (playerScores[playerId] >= amount)
        {
            playerScores[playerId] -= amount;
            Debug.Log($"Player {playerId} spent {amount}. Remaining: {playerScores[playerId]}");
            return true; // The purchase was successful!
        }
        
        Debug.Log($"Player {playerId} is too poor! Needs {amount}, has {playerScores[playerId]}");
        return false; // The purchase failed
    }
    
    public int GetScore(int playerId)
    {
        return playerScores.ContainsKey(playerId) ? playerScores[playerId] : 0;
    }
}