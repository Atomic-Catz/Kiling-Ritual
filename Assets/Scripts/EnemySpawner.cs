using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] enemyPrefabs;
    public float spawnRadius = 5f;
    public int enemiesPerWave = 10;
    public float spawnDelay = 0.5f;

    private List<GameObject> activeEnemies = new List<GameObject>();

    public bool HasActiveEnemies => activeEnemies.Count > 0;

    public void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            Vector3 spawnPos = GetRandomNavMeshPoint(transform.position, spawnRadius);
            if (spawnPos != Vector3.zero)
            {
                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
                activeEnemies.Add(enemy);

                SpawnerEnemy spawnerEnemy = enemy.GetComponent<SpawnerEnemy>();
                if (spawnerEnemy == null)
                    spawnerEnemy = enemy.AddComponent<SpawnerEnemy>();

                spawnerEnemy.OnEnemyDestroyed += () =>
                {
                    activeEnemies.Remove(enemy);
                };
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return Vector3.zero;
    }
}

public class SpawnerEnemy : MonoBehaviour
{
    public delegate void EnemyDestroyed();
    public event EnemyDestroyed OnEnemyDestroyed;

    private void OnDestroy()
    {
        OnEnemyDestroyed?.Invoke();
    }
}
