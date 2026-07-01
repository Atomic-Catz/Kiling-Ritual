using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Play Sound Behaviour. Custom Networked Version!
    /// </summary>
    public class PlaySoundBehaviour : StateMachineBehaviour
    {
        #region FIELDS SERIALIZED
        
        [Header("Setup")]
        [Tooltip("AudioClip to play!")]
        [SerializeField]
        private AudioClip clip;
        
        [Header("Settings")]
        [Tooltip("Audio Settings.")]
        [SerializeField]
        private AudioSettings settings = new AudioSettings(1.0f, 0.0f, true);

        #endregion

        #region UNITY

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (clip == null) return;

            // 1. Find the character holding this weapon
            Character character = animator.GetComponentInParent<Character>();
            
            // Assume local player unless we explicitly find a remote character
            bool isLocalPlayer = true; 
            if (character != null)
            {
                isLocalPlayer = character.isOwner;
            }

            // 2. Create a temporary audio object exactly at the weapon's location
            GameObject audioObj = new GameObject($"NetworkedAnimAudio -> {clip.name}");
            audioObj.transform.position = animator.transform.position;
            
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = settings.Volume;
            
            // 3. MULTIPLAYER SPLIT
            if (isLocalPlayer)
            {
                // Loud and punchy in your head
                source.spatialBlend = 0f; 
            }
            else
            {
                // 3D positional audio for teammates
                source.spatialBlend = 1f; 
                source.minDistance = 2f;
                source.maxDistance = 100f;
                source.rolloffMode = AudioRolloffMode.Linear;
            }

            // 4. Play and clean up
            source.Play();
            Destroy(audioObj, clip.length + 0.1f);
        }

        #endregion
    }
}