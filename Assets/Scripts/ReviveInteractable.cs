using UnityEngine;
using PurrNet;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack
{
    public class ReviveInteractable : NetworkBehaviour, IInteractable
    {
        private CharacterHealth targetHealth;

        private void Awake()
        {
            // We assume this script is placed on the player prefab (or a child of it)
            targetHealth = GetComponentInParent<CharacterHealth>();
        }

        public string GetInteractText()
        {
            // If they aren't downed, don't show any UI prompt
            if (targetHealth == null || !targetHealth.isDowned.value) 
                return "";
            
            // If someone is already helping them, update the UI
            if (targetHealth.isBeingRevived.value) 
                return "Reviving...";

            // Show the [E] prompt and their remaining bleed-out time
            return $"[E] Revive Teammate ( {Mathf.Ceil(targetHealth.bleedOutTime.value)}s )"; 
        }

        public void Interact(CharacterBehaviour user)
        {
            // PRE-CHECK: Stop if they are already being revived or aren't actually downed
            if (targetHealth == null || !targetHealth.isDowned.value || targetHealth.isBeingRevived.value) 
                return;

            NetworkBehaviour playerNetwork = user.GetComponent<NetworkBehaviour>();
            if (playerNetwork == null || !playerNetwork.isOwner) 
                return;

            int healerId = playerNetwork.owner.HasValue ? (int)(ulong)playerNetwork.owner.Value.id : 0;
            
            // Ask the server to start the revive process
            CmdStartRevive(healerId);
        }

        // SERVER LOGIC: Handle the 3-second revive timer
        [ServerRpc(requireOwnership: false)]
        private void CmdStartRevive(int healerId)
        {
            // Double check on the server
            if (!targetHealth.isDowned.value || targetHealth.isBeingRevived.value) 
                return;

            Character healerPlayer = GetPlayerById(healerId);
            if (healerPlayer != null)
            {
                StartCoroutine(ReviveProcess(healerPlayer));
            }
        }

        private IEnumerator ReviveProcess(Character healer)
        {
            // Tell the network this player is being helped (this should pause your bleed-out timer in CharacterHealth)
            targetHealth.isBeingRevived.value = true;
            
            float reviveTimer = 0f;
            float reviveDuration = 3f; // Takes 3 seconds to pick them up

            while (reviveTimer < reviveDuration)
            {
                // CANCEL CHECK: If the healer runs away (further than 4 units) or dies/disconnects, cancel the revive!
                if (healer == null || Vector3.Distance(transform.position, healer.transform.position) > 4f)
                {
                    targetHealth.isBeingRevived.value = false;
                    yield break;
                }

                reviveTimer += Time.deltaTime;
                yield return null;
            }

            // SUCCESS! The server officially brings them back.
            targetHealth.RevivePlayer();
        }

        // Helper to find the player doing the reviving
        private Character GetPlayerById(int id)
        {
            foreach (Character p in FindObjectsOfType<Character>())
            {
                if (p.owner.HasValue && (int)(ulong)p.owner.Value.id == id) return p;
            }
            return null;
        }
    }
}