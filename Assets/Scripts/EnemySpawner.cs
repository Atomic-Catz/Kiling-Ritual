using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] enemyPrefabs; // Multiple enemy prefabs
    public float spawnRadius = 5f;    // The radius around the spawner to spawn enemies
    public int maxEnemies = 10;       // Maximum number of active enemies
    public float spawnInterval = 5f;  // Time between spawns

    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isSpawning = true;

    public float waveDuration = 120f;
    public float breakDuration = 90f;

    private bool waveInProgress = false;
    private bool spawningAllowed = false;
    private int enemiesSpawnedThisWave = 0;
    private int waveNumber = 0;

    void Start()
    {
        StartCoroutine(SpawnEnemies());
        StartCoroutine(WaveRoutine());
    }

    IEnumerator WaveRoutine()
    {
        while (isSpawning)
        {
            waveNumber++;
            enemiesSpawnedThisWave = 0;
            waveInProgress = true;
            spawningAllowed = true;
            float elapsed = 0f;

            while (elapsed < waveDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            spawningAllowed = false;

            if (enemiesSpawnedThisWave > 0)
            {
                while (activeEnemies.Count > 0)
                {
                    yield return null;
                }
            }

            waveInProgress = false;

            float breakElapsed = 0f;
            while (breakElapsed < breakDuration && isSpawning)
            {
                breakElapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator SpawnEnemies()
    {
        while (isSpawning)
        {
            if (!spawningAllowed)
            {
                yield return null;
                continue;
            }
            
            yield return new WaitForSeconds(spawnInterval);

            if (!spawningAllowed)
                continue;

            if (activeEnemies.Count < maxEnemies)
            {
                Vector3 spawnPosition = GetRandomNavMeshPoint(transform.position, spawnRadius);

                if (spawnPosition != Vector3.zero)
                {
                    // Pick a random prefab from the list
                    GameObject prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

                    GameObject enemy = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
                    activeEnemies.Add(enemy);
                    enemiesSpawnedThisWave++;

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
        public delegate void EnemyDestroyed();
        public event EnemyDestroyed OnEnemyDestroyed;

        void OnDestroy()
        {
            OnEnemyDestroyed?.Invoke();
        }
    }
}
