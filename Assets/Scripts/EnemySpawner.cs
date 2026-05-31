using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace InfimaGames.LowPolyShooterPack
{
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

        /// <summary>
        /// Starts the wave. Triggered only via Server authority from WaveManager.
        /// </summary>
        public void StartWave(int count, float hpMultiplier, GameObject bossPrefab = null)
        {
            StartCoroutine(SpawnWaveRoutine(count, hpMultiplier, bossPrefab));
        }

        private IEnumerator SpawnWaveRoutine(int totalToSpawn, float hpMultiplier, GameObject bossPrefab)
        {
            isSpawning = true;

            // 1. BOSS SPAWNING
            if (bossPrefab != null)
            {
                Vector3 bossPos = GetRandomNavMeshPoint(transform.position, spawnRadius);
                if (bossPos != Vector3.zero)
                {
                    // PurrNet handles the global network spawning automatically upon instantiation
                    GameObject boss = Instantiate(bossPrefab, bossPos, Quaternion.identity);
                    
                    ApplyScaling(boss, hpMultiplier);
                    activeEnemies.Add(boss);

                    // Attach tracker
                    SpawnerEnemy tracker = boss.GetComponent<SpawnerEnemy>() ?? boss.AddComponent<SpawnerEnemy>();
                    tracker.OnEnemyDestroyed += () => 
                    { 
                        activeEnemies.Remove(boss); 
                    };
                }
            }

            // 2. REGULAR ENEMY SPAWNING
            int spawnedThisWave = 0;
            while (spawnedThisWave < totalToSpawn)
            {
                activeEnemies.RemoveAll(item => item == null);

                if (activeEnemies.Count < maxActiveAtOnce)
                {
                    Vector3 spawnPos = GetRandomNavMeshPoint(transform.position, spawnRadius);
                    if (spawnPos != Vector3.zero)
                    {
                        int randomPrefabIndex = Random.Range(0, enemyPrefabs.Length);
                        GameObject prefab = enemyPrefabs[randomPrefabIndex];
                        
                        // PurrNet handles the global network spawning automatically upon instantiation
                        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
                        
                        ApplyScaling(enemy, hpMultiplier);
                        activeEnemies.Add(enemy);
                        spawnedThisWave++;

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

    // --- HELPER CLASS (Now safely inside the namespace) ---
    public class SpawnerEnemy : MonoBehaviour
    {
        public delegate void EnemyDestroyed();
        public event EnemyDestroyed OnEnemyDestroyed;

        private bool hasReported = false;

        public void ReportDeath()
        {
            if (hasReported) return;
            hasReported = true;
            OnEnemyDestroyed?.Invoke();
        }

        private void OnDestroy()
        {
            ReportDeath();
        }
    }
}