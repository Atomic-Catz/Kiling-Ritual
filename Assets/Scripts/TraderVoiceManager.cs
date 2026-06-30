using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class TraderVoiceManager : NetworkBehaviour
    {
        [Header("Audio Setup")]
        public AudioSource voiceSource;

        [Header("Voice Line Arrays")]
        public AudioClip[] greetingLines;
        public AudioClip[] purchaseSuccessLines;
        public AudioClip[] notEnoughMoneyLines;

        // --- NEW: Replaced the cooldown with a simple one-time flag ---
        private bool hasGreetedLocalPlayer = false;
        // --------------------------------------------------------------

        // ==========================================
        // 1. PROXIMITY GREETING (Local Trigger)
        // ==========================================
        private void OnTriggerEnter(Collider other)
        {
            // We only want the local player to trigger this.
            Character player = other.GetComponent<Character>();
            
            if (player != null && player.isOwner)
            {
                // Only greet them if we haven't already greeted them this wave!
                if (!hasGreetedLocalPlayer)
                {
                    PlayRandomLine(greetingLines);
                    hasGreetedLocalPlayer = true;
                }
            }
        }

        // ==========================================
        // 2. PURCHASE SUCCESS (Networked via RPC)
        // ==========================================
        // The server calls this when it successfully validates the wallet transaction.
        // [ObserversRpc] ensures ALL players in the shack hear the purchase!
        [ObserversRpc]
        public void PlayPurchaseSuccessRPC()
        {
            PlayRandomLine(purchaseSuccessLines);
        }

        // ==========================================
        // 3. INSUFFICIENT FUNDS (Local Call)
        // ==========================================
        // The client calls this locally when they click "Buy" but know they don't have enough.
        public void PlayNotEnoughMoneyLocal()
        {
            // Only play if he isn't already talking, so lines don't overlap awkwardly
            if (!voiceSource.isPlaying)
            {
                PlayRandomLine(notEnoughMoneyLines);
            }
        }

        // ==========================================
        // UTILITY: PLAY RANDOM CLIP
        // ==========================================
        private void PlayRandomLine(AudioClip[] clipArray)
        {
            if (clipArray == null || clipArray.Length == 0) return;

            int randomIndex = Random.Range(0, clipArray.Length);
            voiceSource.clip = clipArray[randomIndex];
            voiceSource.Play();
        }
    }
}