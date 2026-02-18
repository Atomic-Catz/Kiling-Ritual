using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Trader Settings")]
    public GameObject traderPrefab;
    public Transform traderSpawnPoint;
    private GameObject activeTrader;
    
    [Header("Spawners")]
    public EnemySpawner[] spawners;

    [Header("Zombies Scaling (CoD Style)")]
    public float breakDuration = 15f; 
    public int baseEnemyCount = 6;      // Starting enemies at Wave 1
    public float countMultiplier = 1.15f; // +15% enemies per wave
    public float healthMultiplier = 1.1f; // +10% health per wave (until cap)

    private int currentWave = 0;

    private void Awake()
    {
        if (spawners == null || spawners.Length == 0)
            spawners = FindObjectsOfType<EnemySpawner>();
    }

    private void Start()
    {
        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        while (true)
        {
            currentWave++;
            
            // MATH: Quantity and Health scaling
            int totalEnemiesForWave = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(countMultiplier, currentWave - 1)) + (currentWave * 2);
            float currentHealthBoost = Mathf.Pow(healthMultiplier, Mathf.Min(currentWave, 20) - 1); // Health caps scaling at Wave 20

            Debug.Log($"<color=red>Wave {currentWave} Started!</color> Enemies: {totalEnemiesForWave}, HP Boost: {currentHealthBoost:F2}x");

            // Distribute total count among all spawners
            int enemiesPerSpawner = totalEnemiesForWave / spawners.Length;
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                    spawner.StartWave(enemiesPerSpawner, currentHealthBoost);
            }

            // Wait until all spawners report 0 active enemies AND 0 enemies left to spawn
            bool allClear = false;
            while (!allClear)
            {
                allClear = true;
                foreach (var spawner in spawners)
                {
                    if (spawner != null && (spawner.HasActiveEnemies || spawner.IsSpawning))
                    {
                        allClear = false;
                        break;
                    }
                }
                yield return new WaitForSeconds(1.0f);
            }

            Debug.Log($"Wave {currentWave} Clear. Break Time!");
            SpawnTrader();
            yield return new WaitForSeconds(breakDuration);
            DespawnTrader();
        }
    }
    
    private void SpawnTrader()
    {
        if (traderPrefab && traderSpawnPoint && !activeTrader)
            activeTrader = Instantiate(traderPrefab, traderSpawnPoint.position, traderSpawnPoint.rotation);
    }

    private void DespawnTrader() { if (activeTrader) { Destroy(activeTrader); activeTrader = null; } }
}