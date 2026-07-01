// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour
    {
        #region FIELDS SERIALIZED

        [Header("Audio Setup")]
        [Tooltip("The specific AudioSource used for footsteps. This prevents it from hijacking your voice/hit audio source!")]
        [SerializeField] private AudioSource footstepsAudioSource;
        
        [Header("Audio Clips")]
        
        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Speeds")] 
        [SerializeField]
        private float jumpForce = 10f;
        
        [SerializeField]
        private float speedWalking = 5.0f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float speedRunning = 9.0f;

        #endregion

        #region PROPERTIES

        private Vector3 Velocity
        {
            get => rigidBody.linearVelocity;
            set => rigidBody.linearVelocity = value;
        }

        #endregion

        #region FIELDS

        private Rigidbody rigidBody;
        private CapsuleCollider capsule;
        private AudioSource audioSource;
        private bool grounded;

        private CharacterBehaviour playerCharacter;
        private WeaponBehaviour equippedWeapon;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        #endregion

        #region UNITY FUNCTIONS

        protected override void Awake()
        {
            // Keep this empty
        }
        
        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
            {
                playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        
                // --- ADD THIS DEBUG LINE ---
                if (playerCharacter == null)
                {
                    Debug.LogError($"[Movement] {gameObject.name} is the Owner, but playerCharacter is NULL from the Service Locator!");
                }
                else
                {
                    Debug.Log($"[Movement] {gameObject.name} successfully linked to its Character script!");
                }
                // ---------------------------
            }
            else
            {
                if (rigidBody == null) rigidBody = GetComponent<Rigidbody>();
                if (rigidBody != null) rigidBody.isKinematic = true;
            }
        }

        protected override void Start()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            capsule = GetComponent<CapsuleCollider>();

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;

            // If we are NOT the owner, let the network completely dictate physics movement
            if (!isOwner)
            {
                if (rigidBody != null)
                {
                    rigidBody.isKinematic = true; // Prevents gravity/collisions from fighting NetworkTransform
                }
            }
        }

        private void OnCollisionStay()
        {
            // NETWORK CHECK: Non-owners shouldn't run collision ground detection
            if (!isOwner) return;

            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            float radius = extents.x - 0.01f;
            
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
                groundHits, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
            
            if (!groundHits.Any(hit => hit.collider != null && hit.collider != capsule)) 
                return;
            
            for (var i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();

            grounded = true;
        }
            
        protected override void FixedUpdate()
        {
            // NETWORK CHECK
            if (!isOwner) return;
            
            if (playerCharacter == null) return;

            MoveCharacter();
            grounded = false;
        }

        protected override void Update()
        {
            // NETWORK CHECK
            if (!isOwner) return;

            // Enforce safe access to playerCharacter
            if (playerCharacter == null) return;

            equippedWeapon = playerCharacter.GetInventory().GetEquipped();
            PlayFootstepSounds();
            Jump();
        }

        #endregion

        #region METHODS

        void Jump()
        {
            // FIX: Stop the player from jumping if they are downed!
            CharacterHealth health = GetComponent<CharacterHealth>();
            if (health != null && health.isDowned.value) 
            {
                return; 
            }

            if (grounded && Input.GetKeyDown(KeyCode.Space))
            {
                rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

                grounded = false;
            }
        }
        
        private void MoveCharacter()
        {
            // 1. Check if the player is currently downed
            CharacterHealth health = GetComponent<CharacterHealth>();
            bool isDowned = health != null && health.isDowned.value;

            Vector2 frameInput = playerCharacter.GetInputMovement();
    
            // --- BYPASS USING THE NEW INPUT SYSTEM HARDWARE API ---
            if (frameInput.sqrMagnitude < 0.01f)
            {
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) frameInput.y = 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) frameInput.y = -1f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) frameInput.x = -1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) frameInput.x = 1f;
                }
            }
            // -----------------------------------------------------

            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);
    
            // 2. THE ULTIMATE OVERRIDE: 
            // If downed, force a very slow speed and completely ignore sprint input!
            if (isDowned)
            {
                movement *= (speedWalking * 0.25f); // 25% of normal walking speed
            }
            else if (playerCharacter.IsRunning())
            {
                movement *= speedRunning;
            }
            else
            {
                movement *= speedWalking;
            }

            movement = transform.TransformDirection(movement);
            Velocity = new Vector3(movement.x, Velocity.y, movement.z);
        }

        private void PlayFootstepSounds()
        {
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f)
            {
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        #endregion
    }
}