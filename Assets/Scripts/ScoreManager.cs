using System.Collections.Generic;
using UnityEngine;
using PurrNet; 

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;

    private Dictionary<int, int> playerScores = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // REMOVED: DontDestroyOnLoad(gameObject); to prevent network unbinding bugs
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddPoints(int playerId, int points)
    {
        Debug.Log($"[ScoreManager] Attempting to add {points} points to Player {playerId}. IsServer: {isServer}");

        // Enforce server authority
        if (!isServer) 
        {
            Debug.LogWarning("[ScoreManager] Rejected points because this instance thinks it is NOT the server!");
            return;
        }

        if (!playerScores.ContainsKey(playerId))
            playerScores[playerId] = 0;

        playerScores[playerId] += points;
        Debug.Log($"[ScoreManager] Server successfully calculated Player {playerId} Score: {playerScores[playerId]}");

        // Broadcast the final exact score down to everyone's game instances
        SyncPointsToObservers(playerId, playerScores[playerId]);
    }

    public bool SpendPoints(int playerId, int amount)
    {
        if (!isServer) return false;

        if (!playerScores.ContainsKey(playerId))
            playerScores[playerId] = 0;

        if (playerScores[playerId] >= amount)
        {
            playerScores[playerId] -= amount;
            SyncPointsToObservers(playerId, playerScores[playerId]);
            return true; 
        }
        
        return false; 
    }

    [ObserversRpc]
    private void SyncPointsToObservers(int playerId, int exactTotalPoints)
    {
        playerScores[playerId] = exactTotalPoints;
        Debug.Log($"[Network Sync] Player {playerId} score aligned to: {playerScores[playerId]} on this client.");
    }
    
    public int GetScore(int playerId)
    {
        return playerScores.ContainsKey(playerId) ? playerScores[playerId] : 0;
    }
}