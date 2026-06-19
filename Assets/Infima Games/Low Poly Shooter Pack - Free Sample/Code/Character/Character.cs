// Copyright 2021, Infima Games. All Rights Reserved.

using System;
using UnityEngine;
using System.Collections;
using PurrNet;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(CharacterKinematics))]
    public sealed class Character : CharacterBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Death")]
        [SerializeField] private GameObject deathScreen;
        
        [Header("Healing Settings")]
        [SerializeField] private float healAmount = 30f;         
        [SerializeField] private float healCooldown = 5f;        
        [SerializeField] private float healDuration = 2f;        

        private float lastHealTime = -Mathf.Infinity;
        private Coroutine healCoroutine;
        
        [Header("Downed Prototype Settings")]
        [SerializeField] private float downedDropHeight = 1.0f;
        [SerializeField] private float downedTiltAngle = 30f;
        [SerializeField] private float crawlSpeedMultiplier = 0.25f; // Slows movement when downed
        
        [Header("Interaction")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private LayerMask interactMask;

        private IInteractable currentInteractable;

        [Header("Inventory")]
        [SerializeField] private InventoryBehaviour inventory;

        [Header("Cameras")]
        [SerializeField] private Camera cameraWorld;

        [Header("Animation")]
        [SerializeField] private float dampTimeLocomotion = 0.15f;
        [SerializeField] private float dampTimeAiming = 0.3f;
        
        [Header("Animation Procedural")]
        [SerializeField] private Animator characterAnimator;

        #endregion

        #region FIELDS

        private bool aiming;
        private bool running;
        private bool holstered;
        private float lastShotTime;

        private int layerOverlay;
        private int layerHolster;
        private int layerActions;

        private CharacterKinematics characterKinematics;
        private WeaponBehaviour equippedWeapon;
        private WeaponAttachmentManagerBehaviour weaponAttachmentManager;
        private ScopeBehaviour equippedWeaponScope;
        private MagazineBehaviour equippedWeaponMagazine;
        private CharacterHealth characterHealth;

        private bool reloading;
        private bool inspecting;
        private bool holstering;

        private Vector2 axisLook;
        private Vector2 axisMovement;

        private bool holdingButtonAim;
        private bool holdingButtonRun;
        private bool holdingButtonFire;

        private bool tutorialTextVisible;
        private bool cursorLocked;

        private Coroutine reloadSafetyCoroutine;

        // Downed Visual Tracking
        private Vector3 defaultCameraLocalPos;
        private Vector3 defaultInventoryLocalPos;
        private Coroutine downedVisualCoroutine;

        // Tracks if the pause menu is open!
        public bool isMenuOpen { get; private set; }

        #endregion

        #region CONSTANTS

        private static readonly int HashAimingAlpha = Animator.StringToHash("Aiming");
        private static readonly int HashMovement = Animator.StringToHash("Movement");

        #endregion

        #region UNITY

        private void HandleDeath()
        {
            enabled = false;
            if (characterKinematics != null) characterKinematics.enabled = false;

            if (isOwner)
            {
                CmdSyncDeathVisuals();
            }

            if (deathScreen != null) deathScreen.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [ServerRpc]
        private void CmdSyncDeathVisuals()
        {
            ObserverSyncDeathVisuals();
        }

        [ObserversRpc]
        private void ObserverSyncDeathVisuals()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.enabled = false;

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) c.enabled = false;
            
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases) canvas.enabled = false;
        }

        private void HandleDownedStateChanged(bool downed)
        {
            if (downed)
            {
                aiming = false;
                running = false;
                
                // Turn off Kinematics so it stops overriding our camera tilt!
                if (characterKinematics != null) characterKinematics.enabled = false;
            }
            else
            {
                // Turn Kinematics back on when revived!
                if (characterKinematics != null) characterKinematics.enabled = true;
            }

            // INSTANT INVISIBILITY: Hide or show the arms and weapon
            if (isOwner && inventory != null)
            {
                Renderer[] renderers = inventory.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.enabled = !downed; // False when down, True when back up!
                }
            }

            if (isOwner && cameraWorld != null && inventory != null)
            {
                if (downedVisualCoroutine != null) StopCoroutine(downedVisualCoroutine);
                downedVisualCoroutine = StartCoroutine(DoDownedVisualTransition(downed));
            }
        }
        
        private IEnumerator DoDownedVisualTransition(bool isDowned)
        {
            Vector3 targetCamPos = isDowned ? defaultCameraLocalPos - new Vector3(0, downedDropHeight, 0) : defaultCameraLocalPos;
            Vector3 targetInvPos = isDowned ? defaultInventoryLocalPos - new Vector3(0, downedDropHeight, 0) : defaultInventoryLocalPos;
            
            float targetRoll = isDowned ? downedTiltAngle : 0f;
            float t = 0;

            while (t < 1f)
            {
                t += Time.deltaTime * 4f; 
                
                cameraWorld.transform.localPosition = Vector3.Lerp(cameraWorld.transform.localPosition, targetCamPos, t);
                Vector3 camEuler = cameraWorld.transform.localEulerAngles;
                camEuler.z = Mathf.LerpAngle(camEuler.z, targetRoll, t);
                cameraWorld.transform.localEulerAngles = camEuler;

                // Even though the arms are invisible, we still move the inventory down so audio/muzzles stay aligned
                inventory.transform.localPosition = Vector3.Lerp(inventory.transform.localPosition, targetInvPos, t);
                Vector3 invEuler = inventory.transform.localEulerAngles;
                invEuler.z = Mathf.LerpAngle(invEuler.z, targetRoll, t);
                inventory.transform.localEulerAngles = invEuler;

                yield return null;
            }
        }
        
        private void Awake()
        {
            cursorLocked = true;
            UpdateCursorState();

            characterKinematics = GetComponent<CharacterKinematics>();
            characterHealth = GetComponent<CharacterHealth>();

            if (cameraWorld != null) defaultCameraLocalPos = cameraWorld.transform.localPosition;
            if (inventory != null) defaultInventoryLocalPos = inventory.transform.localPosition;

            if (characterHealth != null) 
            {
                characterHealth.OnDeath += HandleDeath;
                characterHealth.OnDownedStateChanged += HandleDownedStateChanged;
            }

            inventory.Init();
            RefreshWeaponSetup();
        }

        private void OnDestroy()
        {
            if (characterHealth != null) 
            {
                characterHealth.OnDeath -= HandleDeath;
                characterHealth.OnDownedStateChanged -= HandleDownedStateChanged;
            }
        }

        protected override void Start()
        {
            if (!isOwner)
            {
                if(cameraWorld != null) cameraWorld.gameObject.SetActive(false);
                if (characterKinematics != null) characterKinematics.enabled = true;
                    
                var movementScript = GetComponent("Movement") as MonoBehaviour;
                if (movementScript != null) movementScript.enabled = false;
            }
            
            layerHolster = characterAnimator.GetLayerIndex("Layer Holster");
            layerActions = characterAnimator.GetLayerIndex("Layer Actions");
            layerOverlay = characterAnimator.GetLayerIndex("Layer Overlay");
        }

        protected override void Update()
        {
            if (!isOwner) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && cursorLocked)
            {
                axisLook = mouse.delta.ReadValue() * 0.05f;
            }
            else
            {
                axisLook = Vector2.zero;
            }
    
            aiming = holdingButtonAim && CanAim();
            running = holdingButtonRun && CanRun();

            if (holdingButtonFire)
            {
                if (CanPlayAnimationFire() && equippedWeapon.HasAmmunition() && equippedWeapon.IsAutomatic())
                {
                    if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire())
                        Fire();
                }
            }

            UpdateAnimator();
            CheckForInteractable();
        }

        protected override void LateUpdate()
        {
            if (!isOwner) return;
            if (equippedWeapon == null || equippedWeaponScope == null) return;
            if (characterKinematics != null) characterKinematics.Compute();
        }

        #endregion

        #region GETTERS

        public override Camera GetCameraWorld() => cameraWorld;
        public override InventoryBehaviour GetInventory() => inventory;
        public override bool IsCrosshairVisible() => !aiming && !holstered && !isMenuOpen; // Hide crosshair in menu
        public override bool IsRunning() => running;
        public override bool IsAiming() => aiming;
        public override bool IsCursorLocked() => cursorLocked;
        public override bool IsTutorialTextVisible() => tutorialTextVisible;
        
        public override Vector2 GetInputMovement() 
        {
            // Block all movement if the menu is open
            if (isMenuOpen) return Vector2.zero;

            if (characterHealth != null && characterHealth.isDowned.value) 
            {
                return axisMovement * crawlSpeedMultiplier; 
            }
            return axisMovement;
        }
        
        public override Vector2 GetInputLook() => axisLook;

        #endregion

        #region METHODS

        // THE MISSING METHOD IS BACK: Called by PauseMenu.cs to unlock the cursor
        public void SetMenuOpen(bool open)
        {
            isMenuOpen = open;
            cursorLocked = !open;
            UpdateCursorState();
            
            // Immediately stop aiming and firing if we open the menu
            if (open)
            {
                aiming = false;
                holdingButtonFire = false;
            }
        }

        private void CheckForInteractable()
        {
            currentInteractable = null;
            Ray ray = cameraWorld.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
            {
                currentInteractable = hit.collider.GetComponent<IInteractable>();
            }
        }
        
        private void UpdateAnimator()
        {
            characterAnimator.SetFloat(HashMovement, Mathf.Clamp01(Mathf.Abs(axisMovement.x) + Mathf.Abs(axisMovement.y)), dampTimeLocomotion, Time.deltaTime);
            characterAnimator.SetFloat(HashAimingAlpha, Convert.ToSingle(aiming), 0.25f / 1.0f * dampTimeAiming, Time.deltaTime);
            characterAnimator.SetBool("Aim", aiming);
            characterAnimator.SetBool("Running", running);
        }

        private void Inspect()
        {
            inspecting = true;
            characterAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
        }

        private void Fire()
        {
            lastShotTime = Time.time;
            
            if(equippedWeapon != null) equippedWeapon.Fire();
            characterAnimator.CrossFade("Fire", 0.05f, layerOverlay, 0);
            
            RequestFireServer();
        }

        private void PlayReloadAnimation()
        {
            string stateName = equippedWeapon.HasAmmunition() ? "Reload" : "Reload Empty";
            characterAnimator.Play(stateName, layerActions, 0.0f);
            reloading = true;

            if (isOwner) equippedWeapon.Reload();

            if (reloadSafetyCoroutine != null) StopCoroutine(reloadSafetyCoroutine);
            reloadSafetyCoroutine = StartCoroutine(SafetyUnlockReload(2.5f));
        }

        private IEnumerator SafetyUnlockReload(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (reloading) reloading = false;
        }

        private IEnumerator Equip(int index = 0)
        {
            if(!holstered)
            {
                SetHolstered(true);
                holstering = true;

                float timeout = Time.time + 0.75f;
                yield return new WaitUntil(() => holstering == false || Time.time > timeout);
                
                holstering = false; 
            }
            
            SetHolstered(false);
            characterAnimator.Play("Unholster", layerHolster, 0);
            inventory.Equip(index);
            RefreshWeaponSetup();

            if (isOwner)
            {
                CmdSyncWeaponSwap(index);
            }
        }

        private void RefreshWeaponSetup()
        {
            if ((equippedWeapon = inventory.GetEquipped()) == null) return;
            characterAnimator.runtimeAnimatorController = equippedWeapon.GetAnimatorController();
            weaponAttachmentManager = equippedWeapon.GetAttachmentManager();
            if (weaponAttachmentManager == null) return;
            equippedWeaponScope = weaponAttachmentManager.GetEquippedScope();
            equippedWeaponMagazine = weaponAttachmentManager.GetEquippedMagazine();

            if (equippedWeapon is Weapon customWeapon && cameraWorld != null)
            {
                customWeapon.SetupNetworkOwner(this, cameraWorld.transform);
            }
        }

        private void FireEmpty()
        {
            lastShotTime = Time.time;
            characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
            RequestFireEmptyServer();
        }

        private void UpdateCursorState()
        {
            Cursor.visible = !cursorLocked;
            Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        private void SetHolstered(bool value = true)
        {
            holstered = value;
            characterAnimator.SetBool("Holstered", holstered);
        }

        #endregion

        #region ACTION CHECKS

        private bool IsPlayerDowned() => characterHealth != null && characterHealth.isDowned.value;

        // FIXED: Added !isMenuOpen to all actions so you can't shoot/reload while clicking UI buttons!
        private bool CanPlayAnimationFire() => !(holstered || holstering || reloading || inspecting) && !IsPlayerDowned() && !isMenuOpen;
        private bool CanPlayAnimationReload() => !(reloading || inspecting) && !IsPlayerDowned() && !isMenuOpen; 
        private bool CanPlayAnimationHolster() => !(reloading || inspecting) && !IsPlayerDowned() && !isMenuOpen;
        private bool CanChangeWeapon() => !(holstering || reloading || inspecting) && !IsPlayerDowned() && !isMenuOpen;
        private bool CanPlayAnimationInspect() => !(holstered || reloading || holstering || inspecting) && !IsPlayerDowned() && !isMenuOpen;
        private bool CanAim() => !(holstered || inspecting || reloading || holstering) && !IsPlayerDowned() && !isMenuOpen;
        private bool CanRun()
        {
            if (IsPlayerDowned() || inspecting || reloading || aiming || isMenuOpen) return false;
            if (holdingButtonFire && equippedWeapon.HasAmmunition()) return false;
            if (axisMovement.y <= 0 || Math.Abs(Mathf.Abs(axisMovement.x) - 1) < 0.01f) return false;
            return true;
        }

        #endregion

        #region INPUT

        public void OnTryInteract(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked || context.phase != InputActionPhase.Performed || IsPlayerDowned() || isMenuOpen) return;
            if (currentInteractable != null) currentInteractable.Interact(this);
        }
        
        public void OnTryHeal(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked || characterHealth == null || context.phase != InputActionPhase.Started || IsPlayerDowned() || isMenuOpen) return;
            if (reloading || inspecting || holstering || characterHealth.GetCurrentHealth() >= characterHealth.GetMaxHealth()) return;
            if (Time.time - lastHealTime < healCooldown) return;

            if (healCoroutine != null) StopCoroutine(healCoroutine);
            RequestHealServer(healAmount, healDuration);
            lastHealTime = Time.time;
        }

        private IEnumerator GradualHeal(float totalAmount, float duration)
        {
            float healed = 0f;
            float rate = totalAmount / duration;

            while (healed < totalAmount)
            {
                if (characterHealth.GetCurrentHealth() >= characterHealth.GetMaxHealth()) break;
                float healThisFrame = rate * Time.deltaTime;
                characterHealth.Heal(healThisFrame);
                healed += healThisFrame;
                yield return null;
            }
        }
        
        public void OnTryFire(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked) return;

            switch (context)
            {
                case {phase: InputActionPhase.Started}:
                    holdingButtonFire = true;
                    break;
                case {phase: InputActionPhase.Performed}:
                    if (!CanPlayAnimationFire()) break;
                    if (equippedWeapon.HasAmmunition())
                    {
                        if (equippedWeapon.IsAutomatic()) break;
                        if (Time.time - lastShotTime > 60.0f / equippedWeapon.GetRateOfFire()) Fire();
                    }
                    else FireEmpty();
                    break;
                case {phase: InputActionPhase.Canceled}:
                    holdingButtonFire = false;
                    break;
            }
        }

        public void OnTryPlayReload(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked || !CanPlayAnimationReload() || context.phase != InputActionPhase.Performed) return;

            bool hasAmmo = equippedWeapon.GetAmmunitionCurrent() > 0 || (equippedWeapon is Weapon w && w.GetReserveAmmunition() > 0);
            if (hasAmmo) RequestReloadServer();
            else if (equippedWeapon is Weapon w2 && w2.GetAudioClipFireEmpty() != null)
                AudioSource.PlayClipAtPoint(w2.GetAudioClipFireEmpty(), transform.position);
        }

        // --- PURRNET RPC CHANNELS ---

        [ServerRpc]
        private void CmdSyncWeaponSwap(int index)
        {
            ObserverSyncWeaponSwap(index);
        }

        [ObserversRpc]
        private void ObserverSyncWeaponSwap(int index)
        {
            if (isOwner) return; 

            inventory.Equip(index);
            RefreshWeaponSetup();
        }

        [ServerRpc]
        public void CmdSpawnNetworkedProjectile(Vector3 fallbackPosition, Quaternion rotation, float impulse, bool trackingInstaKill)
        {
            var activeWeapon = equippedWeapon as Weapon;
            if (activeWeapon == null || activeWeapon.GetPrefabProjectile() == null) return;

            Vector3 spawnPosition = fallbackPosition;
            var attachmentManager = activeWeapon.GetAttachmentManager();
            if (attachmentManager != null)
            {
                var muzzle = attachmentManager.GetEquippedMuzzle();
                if (muzzle != null && muzzle.GetSocket() != null)
                {
                    spawnPosition = muzzle.GetSocket().position;
                }
            }

            bool globalInstaKill = GlobalBuffManager.Instance != null && GlobalBuffManager.Instance.isInstaKillActive;
            
            GameObject projectileObj = Instantiate(activeWeapon.GetPrefabProjectile(), spawnPosition, rotation);
            Projectile projectileScript = projectileObj.GetComponent<Projectile>();
            
            if (projectileScript != null)
            {
                int currentAttackerId = owner.HasValue ? (int)(ulong)owner.Value.id : 0;
                
                projectileScript.InitializeProjectile(GetComponent<Collider>(), currentAttackerId, globalInstaKill);
            }

            Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = projectileObj.transform.forward * impulse;
            }
        }

        [ServerRpc]
        private void RequestHealServer(float totalAmount, float duration)
        {
            if (healCoroutine != null) StopCoroutine(healCoroutine);
            healCoroutine = StartCoroutine(GradualHeal(totalAmount, duration));
        }

        [ServerRpc]
        private void RequestFireServer() => ObserverPlayFireEffects();
        
        [ObserversRpc]
        private void ObserverPlayFireEffects()
        {
            if (isOwner) return; 
            
            if (equippedWeapon != null)
            {
                var activeWeapon = equippedWeapon as Weapon;
                if (activeWeapon != null && activeWeapon.GetAttachmentManager() != null)
                {
                    var muzzle = activeWeapon.GetAttachmentManager().GetEquippedMuzzle();
                    if (muzzle != null) 
                    {
                        muzzle.Effect(); 
                    }
                }
                
                if (equippedWeapon.GetAnimator() != null)
                    equippedWeapon.GetAnimator().Play("Fire", 0, 0.0f);
            }

            if (characterAnimator != null)
                characterAnimator.CrossFade("Fire", 0.05f, layerOverlay, 0);
        }

        [ServerRpc]
        private void RequestFireEmptyServer() => ObserverPlayFireEmptyEffects();

        [ObserversRpc]
        private void ObserverPlayFireEmptyEffects()
        {
            if (isOwner) return;
            if (characterAnimator != null) characterAnimator.CrossFade("Fire Empty", 0.05f, layerOverlay, 0);
        }
        
        [ServerRpc]
        private void RequestReloadServer() => ObserverPlayReloadAnimation();

        [ObserversRpc]
        private void ObserverPlayReloadAnimation() => PlayReloadAnimation();
    
        public void OnTryInspect(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked || !CanPlayAnimationInspect() || context.phase != InputActionPhase.Performed) return;
            Inspect();
        }

        public void OnTryAiming(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked) return;
            switch (context.phase)
            {
                case InputActionPhase.Started: holdingButtonAim = true; break;
                case InputActionPhase.Canceled: holdingButtonAim = false; break;
            }
        }

        public void OnTryHolster(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked || context.phase != InputActionPhase.Performed) return;
            if (CanPlayAnimationHolster())
            {
                SetHolstered(!holstered);
                holstering = true;
            }
        }

        public void OnTryRun(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked) return;
            switch (context.phase)
            {
                case InputActionPhase.Started: holdingButtonRun = true; break;
                case InputActionPhase.Canceled: holdingButtonRun = false; break;
            }
        }

        public void OnTryInventoryNext(InputAction.CallbackContext context)
        {
            if (!isOwner || !cursorLocked || inventory == null || context.phase != InputActionPhase.Performed) return;
            float scrollValue = context.valueType.IsEquivalentTo(typeof(Vector2)) ? Mathf.Sign(context.ReadValue<Vector2>().y) : 1.0f;
            int indexNext = scrollValue > 0 ? inventory.GetNextIndex() : inventory.GetLastIndex();
            int indexCurrent = inventory.GetEquippedIndex();
            if (CanChangeWeapon() && (indexCurrent != indexNext)) StartCoroutine(nameof(Equip), indexNext);
        }

        public void OnLockCursor(InputAction.CallbackContext context)
        {
            // Ignore cursor lock toggles if the menu is actively open
            if (!isOwner || context.phase != InputActionPhase.Performed || isMenuOpen) return;
            cursorLocked = !cursorLocked;
            UpdateCursorState();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (!isOwner) return;
            axisMovement = cursorLocked ? context.ReadValue<Vector2>() : default;
        }
        
        public void OnLook(InputAction.CallbackContext context)
        {
            if (!isOwner) return;
            axisLook = cursorLocked ? context.ReadValue<Vector2>() : default;
        }

        public void OnUpdateTutorial(InputAction.CallbackContext context)
        {
            if (!isOwner) return; 
            tutorialTextVisible = context switch
            {
                {phase: InputActionPhase.Started} => true,
                {phase: InputActionPhase.Canceled} => false,
                _ => tutorialTextVisible
            };
        }

        #endregion

        #region ANIMATION EVENTS

        public override void EjectCasing() { if(equippedWeapon != null) equippedWeapon.EjectCasing(); }
        public override void FillAmmunition(int amount) { if(equippedWeapon != null) equippedWeapon.FillAmmunition(amount); }
        public override void SetActiveMagazine(int active) { equippedWeaponMagazine.gameObject.SetActive(active != 0); }
        public override void AnimationEndedReload() { reloading = false; if (reloadSafetyCoroutine != null) StopCoroutine(reloadSafetyCoroutine); }
        public override void AnimationEndedInspect() { inspecting = false; }
        public override void AnimationEndedHolster() { holstering = false; }

        #endregion
    }
}