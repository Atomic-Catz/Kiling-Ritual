using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Boss Settings")] 
    public GameObject bossPrefab;
    // We can decide how many spawners should spawn a boss
    public int bossesPerWave = 1; 
    
    [Header("Trader Settings")]
    public GameObject traderPrefab;
    public Transform traderSpawnPoint;
    private GameObject activeTrader;
    
    [Header("Spawners")]
    public EnemySpawner[] spawners;

    [Header("Zombies Scaling (CoD Style)")]
    public float breakDuration = 15f; 
    public int baseEnemyCount = 6;
    public float countMultiplier = 1.15f;
    public float healthMultiplier = 1.1f;

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
            // Check if this is a multiple of 5
            bool isBossWave = (currentWave % 5 == 0);
            
            int totalEnemiesForWave = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(countMultiplier, currentWave - 1)) + (currentWave * 2);
            float currentHealthBoost = Mathf.Pow(healthMultiplier, Mathf.Min(currentWave, 20) - 1);

            Debug.Log($"<color=red>Wave {currentWave} Started!</color> {(isBossWave ? "<b>BOSS WAVE!</b>" : "")}");

            // Distribute regular enemies
            int enemiesPerSpawner = totalEnemiesForWave / spawners.Length;

            // We only want the boss to spawn once, so we'll pick the first spawner to handle it
            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i] != null)
                {
                    // If it's a boss wave, give the bossPrefab to the first spawner (index 0)
                    GameObject bossToSpawn = (isBossWave && i == 0) ? bossPrefab : null;
                    
                    // We need to update the StartWave call in EnemySpawner to accept this!
                    spawners[i].StartWave(enemiesPerSpawner, currentHealthBoost, bossToSpawn);
                }
            }

            // Wait until all spawners report 0 active enemies
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