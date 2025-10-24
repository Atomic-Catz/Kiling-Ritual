using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Spawners")]
    public EnemySpawner[] spawners;  // Assign all spawners in the scene

    [Header("Wave Settings")]
    public float breakDuration = 10f; // Time between waves

    private int currentWave = 0;

    private void Awake()
    {
        // Auto-find spawners if none assigned
        if (spawners == null || spawners.Length == 0)
        {
            spawners = FindObjectsOfType<EnemySpawner>();
        }
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
            Debug.Log($"Wave {currentWave} started.");

            // Start wave on all valid spawners
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                    spawner.StartWave();
            }

            // Wait until all spawners have no active enemies
            bool allClear = false;
            while (!allClear)
            {
                allClear = true;
                foreach (var spawner in spawners)
                {
                    if (spawner != null && spawner.HasActiveEnemies)
                    {
                        allClear = false;
                        break;
                    }
                }
                yield return null;
            }

            Debug.Log($"Wave {currentWave} completed. Break for {breakDuration} seconds.");

            // Wait for break before next wave
            yield return new WaitForSeconds(breakDuration);
        }
    }
}