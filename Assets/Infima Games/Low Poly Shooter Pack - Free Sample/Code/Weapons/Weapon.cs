// Copyright 2021, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Weapon. This class handles most of the things that weapons need.
    /// </summary>
    public class Weapon : WeaponBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Trader Settings")]
        [Tooltip("If true, the player can cycle to this weapon. Set this to true for starting weapon!")]
        public string weaponName = "New Weapon";
        public bool isPurchased = false;

        [Tooltip("The cost of the weapon.")] 
        public int weaponPrice = 1000;

        [Header("Firing")]

        [Tooltip("Is this weapon automatic? If yes, then holding down the firing button will continuously fire.")]
        [SerializeField]
        private bool automatic;

        [Tooltip("How fast the projectiles are.")]
        [SerializeField]
        private float projectileImpulse = 400.0f;

        [Tooltip("Amount of shots this weapon can shoot in a minute. It determines how fast the weapon shoots.")]
        [SerializeField]
        private int roundsPerMinutes = 200;

        [Tooltip("Mask of things recognized when firing.")]
        [SerializeField]
        private LayerMask mask;

        [Tooltip("Maximum distance at which this weapon can fire accurately. Shots beyond this distance will not use linetracing for accuracy.")]
        [SerializeField]
        private float maximumDistance = 500.0f;

        [Header("Animation")]

        [Tooltip("Transform that represents the weapon's ejection port, meaning the part of the weapon that casings shoot from.")]
        [SerializeField]
        private Transform socketEjection;

        [Header("Resources")]

        [Tooltip("Casing Prefab.")]
        [SerializeField]
        private GameObject prefabCasing;

        [Tooltip("Projectile Prefab. This is the prefab spawned when the weapon shoots.")]
        [SerializeField]
        private GameObject prefabProjectile;

        [Tooltip("The AnimatorController a player character needs to use while wielding this weapon.")]
        [SerializeField]
        public RuntimeAnimatorController controller;

        [Tooltip("Weapon Body Texture.")]
        [SerializeField]
        private Sprite spriteBody;

        [Header("Audio Clips Holster")]

        [Tooltip("Holster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipHolster;

        [Tooltip("Unholster Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipUnholster;

        [Header("Audio Clips Reloads")]

        [Tooltip("Reload Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipReload;

        [Tooltip("Reload Empty Audio Clip.")]
        [SerializeField]
        private AudioClip audioClipReloadEmpty;

        [Header("Audio Clips Other")]

        [Tooltip("AudioClip played when this weapon is fired without any ammunition.")]
        [SerializeField]
        private AudioClip audioClipFireEmpty;

        [Header("Reloading")]
        [Tooltip("How long the reload takes (seconds).")]
        [SerializeField]
        private float reloadDuration = 1.5f;

        [Tooltip("At what time (seconds from reload start) the ammo transfer actually happens (useful to sync with animation event).")]
        [SerializeField]
        private float ammoApplyTime = 0.6f;

        [Tooltip("Amount of ammunition stored in reserve (not in the current magazine).")]
        [SerializeField]
        private int reserveAmmunition = 90;
        [Tooltip("Amount of ammunition allowed in reserve.")]
        [SerializeField]
        private int reserveAmmunitionMax = 90;

        [Tooltip("Automatically start reload when magazine becomes empty.")]
        [SerializeField]
        private bool autoReloadOnEmpty = true;

        [Tooltip("Allow interrupts (e.g. weapon switch or sprint) to cancel reload.")]
        [SerializeField]
        private bool allowReloadInterrupt = true;

        #endregion

        #region FIELDS

        private Animator animator;
        private WeaponAttachmentManagerBehaviour attachmentManager;
        private int ammunitionCurrent;

        #region Attachment Behaviours

        private MagazineBehaviour magazineBehaviour;
        private MuzzleBehaviour muzzleBehaviour;

        #endregion

        private IGameModeService gameModeService;
        private CharacterBehaviour characterBehaviour;
        private Transform playerCamera;

        // Reload state
        private bool isReloading = false;
        private Coroutine reloadCoroutine;

        #endregion

        #region UNITY

        protected override void Awake()
        {
            animator = GetComponent<Animator>();
            attachmentManager = GetComponent<WeaponAttachmentManagerBehaviour>();

            if (attachmentManager != null)
            {
                magazineBehaviour = attachmentManager.GetEquippedMagazine();
                muzzleBehaviour = attachmentManager.GetEquippedMuzzle();
        
                if (magazineBehaviour != null)
                    ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
            }

            gameModeService = ServiceLocator.Current.Get<IGameModeService>();
            if (gameModeService != null)
            {
                characterBehaviour = gameModeService.GetPlayerCharacter();
                if (characterBehaviour != null && characterBehaviour.GetCameraWorld() != null)
                    playerCamera = characterBehaviour.GetCameraWorld().transform;
            }
        }

        protected override void Start()
        {
            magazineBehaviour = attachmentManager.GetEquippedMagazine();
            muzzleBehaviour = attachmentManager.GetEquippedMuzzle();

            ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
            ammoApplyTime = Mathf.Clamp(ammoApplyTime, 0f, reloadDuration);

            Character parentCharacter = GetComponentInParent<Character>();
            if (parentCharacter != null)
            {
                SetupNetworkOwner(parentCharacter, parentCharacter.GetCameraWorld().transform);
            }
        }

        #endregion

        #region MULTIPLAYER SPECIFIC SETUP

        public void SetupNetworkOwner(CharacterBehaviour owner, Transform ownerCamera)
        {
            characterBehaviour = owner;
            playerCamera = ownerCamera;
        }

        #endregion

        #region GETTERS

        public override Animator GetAnimator() => animator;
        public override Sprite GetSpriteBody() => spriteBody;
        public override AudioClip GetAudioClipHolster() => audioClipHolster;
        public override AudioClip GetAudioClipUnholster() => audioClipUnholster;
        public override AudioClip GetAudioClipReload() => audioClipReload;
        public override AudioClip GetAudioClipReloadEmpty() => audioClipReloadEmpty;
        public override AudioClip GetAudioClipFireEmpty() => audioClipFireEmpty;
        public override AudioClip GetAudioClipFire() => muzzleBehaviour.GetAudioClipFire();
        public override int GetAmmunitionCurrent() => ammunitionCurrent;
        public override int GetAmmunitionTotal() => magazineBehaviour.GetAmmunitionTotal();
        public int GetReserveAmmunition() => reserveAmmunition;
        public override bool IsAutomatic() => automatic;
        public override float GetRateOfFire() => roundsPerMinutes;
        public override bool IsFull() => ammunitionCurrent == magazineBehaviour.GetAmmunitionTotal();
        public override bool HasAmmunition() => ammunitionCurrent > 0;
        public bool IsReserveFull() => reserveAmmunition >= reserveAmmunitionMax;
        public override RuntimeAnimatorController GetAnimatorController() => controller;
        public override WeaponAttachmentManagerBehaviour GetAttachmentManager() => attachmentManager;
        public int GetReserveAmmunitionMax() => reserveAmmunitionMax;
        public GameObject GetPrefabProjectile() => prefabProjectile;
        
        #endregion

        #region METHODS

        public bool TryStartReload()
        {
            if (isReloading) return false;
            if (ammunitionCurrent >= magazineBehaviour.GetAmmunitionTotal()) return false;

            if (reserveAmmunition <= 0)
            {
                if (audioClipFireEmpty != null)
                    AudioSource.PlayClipAtPoint(audioClipFireEmpty, transform.position);
                return false;
            }

            if (animator != null)
                animator.Play(HasAmmunition() ? "Reload" : "Reload Empty", 0, 0.0f);

            reloadCoroutine = StartCoroutine(ReloadRoutine());
            return true;
        }

        public bool CancelReload()
        {
            if (!isReloading || !allowReloadInterrupt) return false;

            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }

            isReloading = false;
            return true;
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;

            float elapsed = 0f;
            while (elapsed < ammoApplyTime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (reserveAmmunition > 0)
            {
                int magTotal = magazineBehaviour.GetAmmunitionTotal();
                int needed = magTotal - ammunitionCurrent;
                int taking = Mathf.Min(needed, reserveAmmunition);
                reserveAmmunition -= taking;
                ammunitionCurrent += taking;
            }

            float remaining = Mathf.Max(0f, reloadDuration - ammoApplyTime);
            elapsed = 0f;
            while (elapsed < remaining)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            isReloading = false;
            reloadCoroutine = null;
        }

        public override void Reload()
        {
            if (TryStartReload()) return;

            if (ammunitionCurrent == 0 && reserveAmmunition == 0 && audioClipFireEmpty != null)
            {
                AudioSource.PlayClipAtPoint(audioClipFireEmpty, transform.position);
            }
        }

        public override void Fire(float spreadMultiplier = 1.0f)
        {
            if (isReloading || muzzleBehaviour == null || playerCamera == null) return;

            if (ammunitionCurrent <= 0)
            {
                if (autoReloadOnEmpty && reserveAmmunition > 0)
                    TryStartReload();
                return;
            }

            Transform muzzleSocket = muzzleBehaviour.GetSocket();

            // Local Visuals Feedback
            const string stateName = "Fire";
            if (animator != null) animator.Play(stateName, 0, 0.0f);
            
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());
            
            // Runs local particle systems and flashlights
            muzzleBehaviour.Effect();

            Quaternion rotation = Quaternion.LookRotation(playerCamera.forward * 1000.0f - muzzleSocket.position);

            if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward),
                out RaycastHit hit, maximumDistance, mask))
            {
                rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);
            }

            bool trackingInstaKill = false;
            if (characterBehaviour != null)
            {
                OneShotKillBuff buff = characterBehaviour.gameObject.GetComponent<OneShotKillBuff>() ?? 
                                      characterBehaviour.gameObject.GetComponentInChildren<OneShotKillBuff>();
                if (buff != null && buff.IsActive) trackingInstaKill = true;
            }

            // FIXED: Only trigger network spawn if this weapon belongs to the local window owner
            var networkCharacter = characterBehaviour as Character;
            if (networkCharacter != null && networkCharacter.isOwner)
            {
                networkCharacter.CmdSpawnNetworkedProjectile(muzzleSocket.position, rotation, projectileImpulse, trackingInstaKill);
            }
        }

        public override void FillAmmunition(int amount)
        {
            if (amount == 0) return;
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent + amount, 0, GetAmmunitionTotal());
        }

        public void AddReserveAmmunition(int amount)
        {
            reserveAmmunition = Mathf.Clamp(reserveAmmunition + amount, 0, reserveAmmunitionMax);
        }

        public override void EjectCasing()
        {
            if (prefabCasing != null && socketEjection != null)
                Instantiate(prefabCasing, socketEjection.position, socketEjection.rotation);
        }

        #endregion
    }
}