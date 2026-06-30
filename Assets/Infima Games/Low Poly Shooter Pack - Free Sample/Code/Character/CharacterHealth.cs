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

        // --- UPDATED: AUDIO SETTINGS ---
        [Header("Audio Settings")]
        public AudioSource voiceSource;
        [Tooltip("The standard sounds played when taking damage.")]
        public AudioClip[] damageGrunts;
        
        [Tooltip("The rare easter egg sound.")]
        public AudioClip easterEggDamageGrunt;
        [Range(0f, 100f)] public float easterEggChance = 5f; // 5% chance by default
        
        public float painCooldown = 1.0f;
        private float lastPainTime = -10f;
        // -------------------------------

        // --- NETWORK VARIABLES ---
        [SerializeField] private SyncVar<float> currentHealth = new SyncVar<float>(100f);
        public SyncVar<bool> isDowned = new SyncVar<bool>(false);
        public SyncVar<bool> isBeingRevived = new SyncVar<bool>(false);
        public SyncVar<float> bleedOutTime = new SyncVar<float>(45f);
        
        // isDead is a SyncVar so clients know they are dead, not revived!
        public SyncVar<bool> isDead = new SyncVar<bool>(false);

        public event Action OnDeath;
        public event Action<bool> OnDownedStateChanged;
        public event Action<float, float> OnHealthChanged;

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
                // Ensure the grey screen is off when we spawn
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
            if (isServer && isDowned.value && !isDead.value)
            {
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
            if (isOwner && isDowned.value && !isDead.value)
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
            // PREVENT "STAND UP" BUG: If they are dead, ignore the "undowned" signal!
            if (isDead.value) return; 

            OnDownedStateChanged?.Invoke(downed);

            // Toggle the grey screen on/off only for the player who owns this body
            if (isOwner && downedUIContainer != null)
            {
                downedUIContainer.SetActive(downed);
            }
        }

        public void TakeDamage(float amount)
        {
            if (!isServer || isDead.value || isDowned.value) return; 
            if (amount <= 0) return;

            currentHealth.value -= amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);

            if (currentHealth.value > 0)
            {
                SyncDamageSound();
            }
            else
            {
                EnterDownedState();
            }
        }

        public void Heal(float amount)
        {
            if (!isServer || isDead.value || isDowned.value) return; 
            if (amount <= 0) return;

            currentHealth.value += amount;
            currentHealth.value = Mathf.Clamp(currentHealth.value, 0, maxHealth);
        }

        private void EnterDownedState()
        {
            if (FindObjectsOfType<CharacterHealth>().Length <= 1)
            {
                Die();
                return;
            }

            isDowned.value = true;
            bleedOutTime.value = maxBleedOutTime;

            if (GameManager.Instance != null) GameManager.Instance.CheckGameOverCondition();
        }

        public void RevivePlayer()
        {
            if (!isServer || isDead.value) return;

            isDowned.value = false;
            isBeingRevived.value = false;
            currentHealth.value = revivedHealth; 

            ObserverFixReviveVisuals();
        }

        private void Die()
        {
            if (isDead.value) return; 
            
            isDead.value = true;
            isDowned.value = false; 

            // Tell all clients to execute the physical death and UI logic
            ObserverHandleDeath();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckGameOverCondition();
            }
        }

        // ==========================================
        // AUDIO RPCs
        // ==========================================
        [ObserversRpc]
        private void SyncDamageSound()
        {
            if (voiceSource == null) return;

            if (Time.time >= lastPainTime + painCooldown)
            {
                // Roll a random number between 0 and 100
                float roll = UnityEngine.Random.Range(0f, 100f);

                // If the roll is within our easter egg chance, play the rare sound
                if (easterEggDamageGrunt != null && roll <= easterEggChance)
                {
                    voiceSource.PlayOneShot(easterEggDamageGrunt);
                }
                // Otherwise, play a normal grunt (if we have any)
                else if (damageGrunts != null && damageGrunts.Length > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, damageGrunts.Length);
                    voiceSource.PlayOneShot(damageGrunts[randomIndex]);
                }

                lastPainTime = Time.time;
            }
        }

        // ==========================================
        // DEATH AND RESPAWN LOGIC
        // ==========================================
        [ObserversRpc]
        private void ObserverHandleDeath()
        {
            // Freeze physics so we don't fall through the floor
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

            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            // Turn off all physical presence
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = false;

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = false;

            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases) canvas.enabled = false;

            // Only the owner of this dead body should open their menus and spectate
            if (isOwner)
            {
                if (downedUIContainer != null) downedUIContainer.SetActive(false);
                
                GetComponent<PlayerSpectator>()?.StartSpectating();
                OnDeath?.Invoke(); 
            }
        }

        public float GetCurrentHealth() => currentHealth.value;
        public float GetMaxHealth() => maxHealth;

        public void RespawnPlayer(Vector3 spawnPosition = default)
        {
            if (!isServer) return;

            isDead.value = false;
            isDowned.value = false;
            isBeingRevived.value = false;
            currentHealth.value = maxHealth;

            // ALWAYS use the initialSpawnPosition instead of whatever the WaveManager says
            ObserverHandleFullRespawn(initialSpawnPosition);
        }

        [ObserversRpc]
        private void ObserverHandleFullRespawn(Vector3 correctSpawnPosition)
        {
            GetComponent<PlayerSpectator>()?.StopSpectating();
            
            // Calculate the safe drop position
            Vector3 spawnPos = correctSpawnPosition + (Vector3.up * 1.5f);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;          
                rb.position = spawnPos;         
                transform.position = spawnPos;  
                rb.isKinematic = false;         
            }
            else
            {
                transform.position = spawnPos;
            }

            // Turn ALL renderers back on
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = true;

            // Turn Colliders and UI back on
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = true;
            
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases) canvas.enabled = true;

            // Turn control scripts back on
            var charScript = GetComponent<Character>();
            if (charScript != null) charScript.enabled = true;

            var kinScript = GetComponent<CharacterKinematics>();
            if (kinScript != null) kinScript.enabled = true;

            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null) playerInput.enabled = true;

            // Re-equip weapons to fix invisibility
            var inventory = GetComponent<Inventory>();
            if (inventory != null) 
            {
                inventory.Init(); 
                int currentIndex = inventory.GetEquippedIndex();
                inventory.Equip(currentIndex);
            }

            ObserverFixReviveVisuals();
            
            // Fix the UI and Cursor exclusively for the person respawning
            if (isOwner)
            {
                var deathMenu = FindObjectOfType<DeathMenu>(true);
                if (deathMenu != null) deathMenu.gameObject.SetActive(false); 

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
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