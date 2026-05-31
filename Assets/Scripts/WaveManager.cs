using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PurrNet; // IMPORT PURRNET CORE

namespace InfimaGames.LowPolyShooterPack
{
    public class WaveManager : NetworkBehaviour
    {
        [Header("Intro Delay Settings")]
        [SerializeField] private float gameStartDelay = 10f; // CoD Zombies style intro countdown

        [Header("Boss Settings")] 
        public GameObject bossPrefab;
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
        private bool loopInitialized = false;

        private void Awake()
        {
            if (spawners == null || spawners.Length == 0)
                spawners = FindObjectsOfType<EnemySpawner>();
        }

        private void Update()
        {
            // CRITICAL NETWORK GUARD: The Wave loop management runs ONLY on the Server/Host window
            if (!isServer) return;

            // Don't kick off anything until a player is verified in the network space
            if (!loopInitialized)
            {
                // Look for any active instantiated instances of our character
                Character localPlayer = FindObjectOfType<Character>();
                if (localPlayer != null)
                {
                    loopInitialized = true;
                    StartCoroutine(InitialStartAndWaveRoutine());
                }
            }
        }

        private IEnumerator InitialStartAndWaveRoutine()
        {
            Debug.Log($"[WaveManager] Player detected! Spawning round loop will begin in {gameStartDelay} seconds...");
            
            // CoD Intro Delay
            yield return new WaitForSeconds(gameStartDelay);

            while (true)
            {
                currentWave++;
                bool isBossWave = (currentWave % 5 == 0);
                
                int totalEnemiesForWave = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(countMultiplier, currentWave - 1)) + (currentWave * 2);
                float currentHealthBoost = Mathf.Pow(healthMultiplier, Mathf.Min(currentWave, 20) - 1);

                Debug.Log($"<color=red>Network Wave {currentWave} Started!</color> {(isBossWave ? "<b>BOSS WAVE!</b>" : "")}");

                // Update UI on all players
                SyncWaveNumberToClients(currentWave);

                int enemiesPerSpawner = totalEnemiesForWave / spawners.Length;

                for (int i = 0; i < spawners.Length; i++)
                {
                    if (spawners[i] != null)
                    {
                        GameObject bossToSpawn = (isBossWave && i == 0) ? bossPrefab : null;
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
            {
                // PurrNet automatically intercepts standard Instantiate and spawns it globally
                activeTrader = Instantiate(traderPrefab, traderSpawnPoint.position, traderSpawnPoint.rotation);
            }
        }

        private void DespawnTrader() 
        { 
            if (activeTrader) 
            { 
                // PurrNet automatically intercepts standard Destroy and despawns it globally
                Destroy(activeTrader);
                activeTrader = null; 
            } 
        }

        [ObserversRpc]
        private void SyncWaveNumberToClients(int waveNumber)
        {
            Debug.Log($"[Client UI] Current Game Round Updated to: {waveNumber}");
        }
    }
}