using UnityEngine;
using System;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class CharacterHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        
        [Header("Downed Settings")]
        [SerializeField] private float maxBleedOutTime = 45f;
        [SerializeField] private float revivedHealth = 50f;

        // UI references assigned automatically at runtime by DownedUIRegister.cs
        [HideInInspector] public GameObject downedUIContainer;
        [HideInInspector] public TMPro.TextMeshProUGUI bleedOutTimerText;

        // --- NETWORK VARIABLES ---
        [SerializeField] private SyncVar<float> currentHealth = new SyncVar<float>(100f);
        public SyncVar<bool> isDowned = new SyncVar<bool>(false);
        public SyncVar<bool> isBeingRevived = new SyncVar<bool>(false);
        public SyncVar<float> bleedOutTime = new SyncVar<float>(45f);

        public event Action OnDeath;
        public event Action<bool> OnDownedStateChanged;
        public event Action<float, float> OnHealthChanged;

        private bool isDead = false;
        
        // Memorize exactly where the player spawned at the start of the match
        private Vector3 initialSpawnPosition;

        private void Awake()
        {
            currentHealth.onChanged += OnHealthSyncChanged;
            isDowned.onChanged += OnDownedSyncChanged;
        }

        private void Start()
        {
            // Save the original spawn point!
            initialSpawnPosition = transform.position;

            if (isOwner) 
            {
                OnDeath += HandleDeath;
                // Ensure the grey screen is off when we spawn (if it linked fast enough)
                if (downedUIContainer != null) downedUIContainer.SetActive(false);
            }
            
            if (isServer)
            {
                currentHealth.value = maxHealth;
                bleedOutTime.value = maxBleedOutTime;
            }
            
            OnHealthChanged?.Invoke(currentHealth.value, maxHealth);
        }

        private void Update()
        {
            // SERVER: Manages the actual bleed-out clock
            if (isServer && isDowned.value && !isDead)
            {
                // Pause the timer if a teammate is actively reviving them
                if (!isBeingRevived.value)
                {
                    bleedOutTime.value -= Time.deltaTime;
                    if (bleedOutTime.value <= 0f)
                    {
                        Die();
                    }
                }
            }

            // CLIENT (Owner): Updates the UI Timer on their screen
            if (isOwner && isDowned.value && !isDead)
            {
                if (bleedOutTimerText != null)
                {
                    bleedOutTimerText.text = $"BLEEDING OUT: {Mathf.CeilToInt(bleedOutTime.value)}";
                }
            }
        }

        private void OnHealthSyncChanged(float newHealth)
        {
            OnHealthChanged?.Invoke(newHealth, maxHealth);
        }

        private void OnDownedSyncChanged(bool downed)
        {
            OnDownedStateChanged?.Invoke(downed);

            // Toggle the grey screen on/off only for the player who owns this body
            if (isOwner && downedUIContainer != null)
            {
                downedUIContainer.SetActive(downed);
            }
        }

        public void TakeDamage(float amount)
        {
            // Don't take damage if they are already down or dead
            if (!isServer || isDead || isDowned.value) return; 
            if (amount <= 0) return;

            currentHealth.value -= amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);

            Debug.Log($"[Server Health] {gameObject.name} took {amount} damage. Current health: {currentHealth.value}");

            if (currentHealth.value <= 0)
            {
                EnterDownedState();
            }
        }

        public void Heal(float amount)
        {
            if (!isServer || isDead || isDowned.value) return; 
            if (amount <= 0) return;

            currentHealth.value += amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);

            Debug.Log($"[Server Health] Healed {amount}. Health: {currentHealth.value}");
        }

        private void EnterDownedState()
        {
            // NEW: Check if this is a solo game. 
            // If there is only 1 player, skip the downed state and die instantly!
            if (FindObjectsOfType<CharacterHealth>().Length <= 1)
            {
                Die();
                return;
            }

            // Normal Multiplayer Logic
            isDowned.value = true;
            bleedOutTime.value = maxBleedOutTime;
            Debug.Log($"[Server Health] {gameObject.name} is DOWNED!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckGameOverCondition();
            }
        }

        public void RevivePlayer()
        {
            if (!isServer || isDead) return;

            isDowned.value = false;
            isBeingRevived.value = false;
            currentHealth.value = revivedHealth; 
            Debug.Log($"[Server Health] {gameObject.name} was REVIVED!");

            ObserverFixReviveVisuals();
        }

        private void Die()
        {
            if (isDead) return; 
            isDead = true;
            isDowned.value = false; // Triggers OnDownedSyncChanged(false), hiding the grey screen

            Debug.Log($"[CharacterHealth] {gameObject.name} died completely!");
            
            // Turn the player into a ghost on ALL screens so they can't shoot or move
            ObserverHandleDeath();
            
            // Start Spectating immediately
            GetComponent<PlayerSpectator>()?.StartSpectating();
            
            OnDeath?.Invoke();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckGameOverCondition();
            }
        }

        [ObserversRpc]
        private void ObserverHandleDeath()
        {
            // --- Freeze physics so we don't fall through the floor! ---
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Disable movement and shooting
            var charScript = GetComponent<Character>();
            if (charScript != null) charScript.enabled = false;

            var kinScript = GetComponent<CharacterKinematics>();
            if (kinScript != null) kinScript.enabled = false;

            // Turn off all physical presence (body, guns, UI, colliders)
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = false;

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = false;

            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases) canvas.enabled = false;
        }

        private void HandleDeath()
        {
            FindObjectOfType<DeathMenu>()?.Show();
        }

        public float GetCurrentHealth() => currentHealth.value;
        public float GetMaxHealth() => maxHealth;

        // ==========================================
        // END OF ROUND RESPAWN LOGIC
        // ==========================================
        
        public void RespawnPlayer(Vector3 spawnPosition = default)
        {
            if (!isServer) return;

            isDead = false;
            isDowned.value = false;
            isBeingRevived.value = false;
            currentHealth.value = maxHealth;

            // TODO: Reset Points here when economy is built

            Debug.Log($"[Server Health] {gameObject.name} was fully respawned!");

            // ALWAYS use the initialSpawnPosition instead of whatever the WaveManager says
            ObserverHandleFullRespawn(initialSpawnPosition);
        }

        [ObserversRpc]
        private void ObserverHandleFullRespawn(Vector3 correctSpawnPosition)
        {
            GetComponent<PlayerSpectator>()?.StopSpectating();
            
            // Add a small vertical boost (+ 1.5f on the Y axis) to prevent clipping through floor
            transform.position = correctSpawnPosition + (Vector3.up * 1.5f);

            // Unfreeze physics so the player can walk again!
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // --- FIX 1: VISIBLE WEAPONS ---
            // Turn ALL renderers back on, no exceptions. 
            // The Inventory turns GameObjects on/off, so having the renderer enabled is safe and required!
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) 
            {
                r.enabled = true;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = true;
            
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases) canvas.enabled = true;

            var charScript = GetComponent<Character>();
            if (charScript != null) charScript.enabled = true;

            var kinScript = GetComponent<CharacterKinematics>();
            if (kinScript != null) kinScript.enabled = true;

            var inventory = GetComponent<Inventory>();
            if (inventory != null) 
            {
                // Reset the inventory back to its starting state
                inventory.Init(); 
                
                // Force the inventory to officially equip the current weapon
                int currentIndex = inventory.GetEquippedIndex();
                inventory.Equip(currentIndex);
            }

            ObserverFixReviveVisuals();
            
            // --- FIX 2 & 3: UI AND CURSOR FIXES FOR THE OWNER ---
            // We only want to hide the UI and lock the mouse for the person who actually respawned
            if (isOwner)
            {
                // Find the Death UI and turn it off
                var deathMenu = FindObjectOfType<DeathMenu>(true);
                if (deathMenu != null)
                {
                    // Most Infima menus can just be deactivated
                    deathMenu.gameObject.SetActive(false); 
                }

                // Re-lock the mouse cursor so you can aim
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // ==========================================
        // ANIMATION FIX
        // ==========================================
        
        [ObserversRpc]
        private void ObserverFixReviveVisuals()
        {
            Animator characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator != null)
            {
                characterAnimator.Rebind();
                characterAnimator.Update(0f);
            }

            InfimaGames.LowPolyShooterPack.Weapon[] weapons = GetComponentsInChildren<InfimaGames.LowPolyShooterPack.Weapon>(true);
            foreach (var weapon in weapons)
            {
                if (weapon.gameObject.activeInHierarchy)
                {
                    weapon.transform.localRotation = Quaternion.identity;
                    
                    if (characterAnimator != null)
                    {
                        characterAnimator.Play("Draw", 0, 0f);
                    }
                    break;
                }
            }
        }
    }
}