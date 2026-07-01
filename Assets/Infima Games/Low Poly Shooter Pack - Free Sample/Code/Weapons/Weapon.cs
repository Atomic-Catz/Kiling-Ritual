// Copyright 2021, Infima Games. All Rights Reserved.

using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class Weapon : WeaponBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Trader Settings")]
        public string weaponName = "New Weapon";
        public bool isPurchased = false;
        public int weaponPrice = 1000;

        [Header("Shotgun Settings")]
        [SerializeField] private bool isShotgun = false;
        [SerializeField] private int pelletCount = 8;
        [SerializeField] private float shotgunSpread = 0.05f;

        [Header("Firing")]
        [SerializeField] private bool automatic;
        [SerializeField] private float projectileImpulse = 400.0f;
        [SerializeField] private int roundsPerMinutes = 200;
        [SerializeField] private LayerMask mask;
        [SerializeField] private float maximumDistance = 500.0f;

        [Header("Animation")]
        [SerializeField] private Transform socketEjection;

        [Header("Resources")]
        [SerializeField] private GameObject prefabCasing;
        [SerializeField] private GameObject prefabProjectile;
        [SerializeField] public RuntimeAnimatorController controller;
        [SerializeField] private Sprite spriteBody;

        [Header("Audio Clips Holster")]
        [SerializeField] private AudioClip audioClipHolster;
        [SerializeField] private AudioClip audioClipUnholster;

        [Header("Audio Clips Reloads")]
        [SerializeField] private AudioClip audioClipReload;
        [SerializeField] private AudioClip audioClipReloadEmpty;

        [Header("Audio Clips Other")]
        [SerializeField] private AudioClip audioClipFireEmpty;

        [Header("Reloading")]
        [SerializeField] private float reloadDuration = 1.5f;
        [SerializeField] private float ammoApplyTime = 0.6f;
        [SerializeField] private int reserveAmmunition = 90;
        [SerializeField] private int reserveAmmunitionMax = 90;
        [SerializeField] private bool autoReloadOnEmpty = true;
        [SerializeField] private bool allowReloadInterrupt = true;

        #endregion

        #region FIELDS
        private Animator animator;
        private WeaponAttachmentManagerBehaviour attachmentManager;
        private int ammunitionCurrent;
        private MagazineBehaviour magazineBehaviour;
        private MuzzleBehaviour muzzleBehaviour;
        private IGameModeService gameModeService;
        private CharacterBehaviour characterBehaviour;
        private Transform playerCamera;
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
                if (magazineBehaviour != null) ammunitionCurrent = magazineBehaviour.GetAmmunitionTotal();
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
                return false; // Removed the rogue 2D audio here!
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

            if (animator != null) animator.Play("Fire", 0, 0.0f);
            
            ammunitionCurrent = Mathf.Clamp(ammunitionCurrent - 1, 0, magazineBehaviour.GetAmmunitionTotal());
            muzzleBehaviour.Effect();

            bool trackingInstaKill = false;
            if (characterBehaviour != null)
            {
                OneShotKillBuff buff = characterBehaviour.gameObject.GetComponent<OneShotKillBuff>() ?? 
                                      characterBehaviour.gameObject.GetComponentInChildren<OneShotKillBuff>();
                if (buff != null && buff.IsActive) trackingInstaKill = true;
            }

            var networkCharacter = characterBehaviour as Character;
            if (networkCharacter != null && networkCharacter.isOwner)
            {
                if (isShotgun)
                {
                    Vector3 baselineTargetPoint = playerCamera.position + playerCamera.forward * maximumDistance;
                    if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward), out RaycastHit hit, maximumDistance, mask))
                    {
                        baselineTargetPoint = hit.point;
                    }

                    Vector3 coreDirection = (baselineTargetPoint - muzzleSocket.position).normalized;

                    for (int i = 0; i < pelletCount; i++)
                    {
                        float randomSpreadX = UnityEngine.Random.Range(-shotgunSpread, shotgunSpread) * spreadMultiplier;
                        float randomSpreadY = UnityEngine.Random.Range(-shotgunSpread, shotgunSpread) * spreadMultiplier;

                        Vector3 randomizedPelletDirection = (coreDirection + (playerCamera.right * randomSpreadX) + (playerCamera.up * randomSpreadY)).normalized;
                        Quaternion pelletRotation = Quaternion.LookRotation(randomizedPelletDirection);

                        networkCharacter.CmdSpawnNetworkedProjectile(muzzleSocket.position, pelletRotation, projectileImpulse, trackingInstaKill);
                    }
                }
                else
                {
                    Quaternion rotation = Quaternion.LookRotation(playerCamera.forward * 1000.0f - muzzleSocket.position);

                    if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward), out RaycastHit hit, maximumDistance, mask))
                    {
                        rotation = Quaternion.LookRotation(hit.point - muzzleSocket.position);
                    }

                    networkCharacter.CmdSpawnNetworkedProjectile(muzzleSocket.position, rotation, projectileImpulse, trackingInstaKill);
                }
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