using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")] public GameObject enemyPrefab; // The enemy prefab to spawn
    public float spawnRadius = 5f; // The radius around the spawner to spawn enemies
    public int maxEnemies = 10; // Maximum number of active enemies
    public float spawnInterval = 5f; // Time between spawns

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawning = true;

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (activeEnemies.Count < maxEnemies)
            {
                Vector3 spawnPosition = GetRandomNavMeshPoint(transform.position, spawnRadius);

                if (spawnPosition != Vector3.zero)
                {
                    GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                    activeEnemies.Add(enemy);

                    // Register to remove the enemy when it is destroyed
                    SpawnerEnemy spawnerEnemy = enemy.GetComponent<SpawnerEnemy>();
                    if (spawnerEnemy != null)
                    {
                        spawnerEnemy.OnEnemyDestroyed += () => activeEnemies.Remove(enemy);
                    }
                }
            }
        }
    }

    Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++) // Try 10 times to find a valid point
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return Vector3.zero; // Return zero if no valid point is found
    }

    public class SpawnerEnemy : MonoBehaviour
    {
        public delegate void EnemyDestroyed(); // Declare delegate type

        public event EnemyDestroyed OnEnemyDestroyed; // Declare event

        void OnDestroy()
        {
            OnEnemyDestroyed?.Invoke(); // Invoke the event when the enemy is destroyed
        }
    }
}