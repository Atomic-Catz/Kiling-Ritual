using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject[] enemyPrefabs;
    
    [Header("Settings")]
    public float spawnRadius = 5f;
    public float spawnDelay = 1.0f;
    public int maxActiveAtOnce = 24; // Standard CoD map limit

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawning = false;

    // Properties for the WaveManager to check
    public bool HasActiveEnemies => activeEnemies.Count > 0;
    public bool IsSpawning => isSpawning;

    public void StartWave(int count, float hpMultiplier)
    {
        StartCoroutine(SpawnWaveRoutine(count, hpMultiplier));
    }

    private IEnumerator SpawnWaveRoutine(int totalToSpawn, float hpMultiplier)
    {
        isSpawning = true;
        int spawnedThisWave = 0;

        while (spawnedThisWave < totalToSpawn)
        {
            // Only spawn if we are under the map capacity
            if (activeEnemies.Count < maxActiveAtOnce)
            {
                Vector3 spawnPos = GetRandomNavMeshPoint(transform.position, spawnRadius);
                if (spawnPos != Vector3.zero)
                {
                    GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                    GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
                    
                    // Apply the wave's health scaling
                    ApplyScaling(enemy, hpMultiplier);

                    activeEnemies.Add(enemy);
                    spawnedThisWave++;

                    // Attach the tracker and subscribe to the death event
                    SpawnerEnemy tracker = enemy.GetComponent<SpawnerEnemy>() ?? enemy.AddComponent<SpawnerEnemy>();
                    tracker.OnEnemyDestroyed += () => 
                    { 
                        activeEnemies.Remove(enemy); 
                    };
                }
            }
            
            yield return new WaitForSeconds(spawnDelay);
        }
        isSpawning = false;
    }

    private void ApplyScaling(GameObject enemy, float hpMult)
    {
        var ai = enemy.GetComponent<EnemyAI>();
        if (ai != null) ai.health *= hpMult;

        var cmd = enemy.GetComponent<CommanderAI>();
        if (cmd != null) cmd.health *= hpMult;
    }

    private Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return Vector3.zero;
    }
}

// --- HELPER CLASS ---
// This sits outside the EnemySpawner class but in the same file
public class SpawnerEnemy : MonoBehaviour
{
    public delegate void EnemyDestroyed();
    public event EnemyDestroyed OnEnemyDestroyed;

    private bool hasReported = false;

    /// <summary>
    /// Call this from EnemyAI or CommanderAI when they die to clear the wave faster.
    /// </summary>
    public void ReportDeath()
    {
        if (hasReported) return;
        hasReported = true;
        OnEnemyDestroyed?.Invoke();
    }

    private void OnDestroy()
    {
        // Safety fallback: ensure the list is cleared if the object is deleted
        ReportDeath();
    }
}