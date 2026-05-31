// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Camera Look. Handles the rotation of the camera with network isolation layers.
    /// </summary>
    public class CameraLook : MonoBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Header("Settings")]
        
        [Tooltip("Sensitivity when looking around.")]
        [SerializeField]
        private Vector2 sensitivity = new Vector2(1, 1);

        [Tooltip("Minimum and maximum up/down rotation angle the camera can have.")]
        [SerializeField]
        private Vector2 yClamp = new Vector2(-60, 60);

        [Tooltip("Should the look rotation be interpolated?")]
        [SerializeField]
        private bool smooth;

        [Tooltip("The speed at which the look rotation is interpolated.")]
        [SerializeField]
        private float interpolationSpeed = 25.0f;
        
        #endregion
        
        #region FIELDS
        
        /// <summary>
        /// Player Character.
        /// </summary>
        private Character playerCharacter;
        /// <summary>
        /// The player character's rigidbody component.
        /// </summary>
        private Rigidbody playerCharacterRigidbody;

        /// <summary>
        /// The player character's rotation.
        /// </summary>
        private Quaternion rotationCharacter;
        /// <summary>
        /// The camera's rotation.
        /// </summary>
        private Quaternion rotationCamera;

        /// <summary>
        /// Cache indicator to verify if this specific instance belongs to our local player window.
        /// </summary>
        private bool isLocalOwner;

        #endregion
        
        #region UNITY

        private void Awake()
        {
            // Find our unique body script higher up in the hierarchy
            playerCharacter = GetComponentInParent<Character>();
            
            if (playerCharacter != null)
            {
                playerCharacterRigidbody = playerCharacter.GetComponent<Rigidbody>();
            }
            else
            {
                Debug.LogError($"[CameraLook] Could not locate a Character script component above {gameObject.name}!");
            }
        }

        private void Start()
        {
            if (playerCharacter == null) return;

            // Evaluate network ownership here in Start() instead of Awake().
            // This gives PurrNet enough frames to register who actually owns this prefab instance.
            isLocalOwner = playerCharacter.isOwner;

            // Cache the character's initial rotation.
            rotationCharacter = playerCharacter.transform.localRotation;
            // Cache the camera's initial rotation.
            rotationCamera = transform.localRotation;
        }

        private void LateUpdate()
        {
            // If network ownership hasn't finished handshaking yet, check it again as a fallback
            if (playerCharacter != null && !isLocalOwner && playerCharacter.isOwner)
            {
                isLocalOwner = true;
            }

            // SECURITY CHECK: Only run look logic if we explicitly own this character instance
            if (!isLocalOwner || playerCharacter == null) return;

            // Frame Input. The Input to add this frame!
            Vector2 frameInput = playerCharacter.IsCursorLocked() ? playerCharacter.GetInputLook() : default;
            
            // Sensitivity scaling.
            frameInput *= sensitivity;

            // Yaw (Horizontal look around).
            Quaternion rotationYaw = Quaternion.Euler(0.0f, frameInput.x, 0.0f);
            // Pitch (Vertical tilt look).
            Quaternion rotationPitch = Quaternion.Euler(-frameInput.y, 0.0f, 0.0f);
            
            // Save rotation values. We use this for smooth rotation tracking filters.
            rotationCamera *= rotationPitch;
            rotationCharacter *= rotationYaw;
            
            // Local Rotation reference wrapper.
            Quaternion localRotation = transform.localRotation;

            // Smooth interpolation engine filters.
            if (smooth)
            {
                // Interpolate local camera rotation.
                localRotation = Quaternion.Slerp(localRotation, rotationCamera, Time.deltaTime * interpolationSpeed);
                // Interpolate character body movement position rotation.
                playerCharacterRigidbody.MoveRotation(Quaternion.Slerp(playerCharacterRigidbody.rotation, rotationCharacter, Time.deltaTime * interpolationSpeed));
            }
            else
            {
                // Rotate camera locally.
                localRotation *= rotationPitch;
                // Clamp looking pitch layout boundaries.
                localRotation = Clamp(localRotation);

                // Rotate the rigid base body asset structure.
                playerCharacterRigidbody.MoveRotation(playerCharacterRigidbody.rotation * rotationYaw);
            }
            
            // Apply calculations back to transform layout.
            transform.localRotation = localRotation;
        }

        #endregion

        #region FUNCTIONS

        /// <summary>
        /// Clamps the pitch of a quaternion according to our clamps.
        /// </summary>
        private Quaternion Clamp(Quaternion rotation)
        {
            rotation.x /= rotation.w;
            rotation.y /= rotation.w;
            rotation.z /= rotation.w;
            rotation.w = 1.0f;

            // Pitch math extraction logic.
            float pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);

            // Bounds capping.
            pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);
            rotation.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

            return rotation;
        }

        #endregion
    }
}