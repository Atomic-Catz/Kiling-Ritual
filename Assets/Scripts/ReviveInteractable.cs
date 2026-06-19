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
            targetHealth = GetComponentInParent<CharacterHealth>();
        }

        public string GetInteractText()
        {
            if (targetHealth == null || !targetHealth.isDowned.value) return "";
            
            if (targetHealth.isBeingRevived.value) return "Reviving...";

            // You can even display the bleed out time to the team!
            return $"Press to Revive ( {Mathf.Ceil(targetHealth.bleedOutTime.value)}s )"; 
        }

        public void Interact(CharacterBehaviour user)
        {
            // Stop if already being revived or not downed
            if (targetHealth == null || !targetHealth.isDowned.value || targetHealth.isBeingRevived.value) return;

            NetworkBehaviour playerNetwork = user.GetComponent<NetworkBehaviour>();
            if (playerNetwork == null || !playerNetwork.isOwner) return;

            int healerId = playerNetwork.owner.HasValue ? (int)(ulong)playerNetwork.owner.Value.id : 0;
            
            // Ask Server to start the revive channel
            CmdStartRevive(healerId);
        }

        [ServerRpc(requireOwnership: false)]
        private void CmdStartRevive(int healerId)
        {
            if (!targetHealth.isDowned.value || targetHealth.isBeingRevived.value) return;

            Character healerPlayer = GetPlayerById(healerId);
            if (healerPlayer != null)
            {
                StartCoroutine(ReviveProcess(healerPlayer));
            }
        }

        private IEnumerator ReviveProcess(Character healer)
        {
            // Tell the network this player is being helped (pauses bleed-out)
            targetHealth.isBeingRevived.value = true;
            
            float reviveTimer = 0f;
            float reviveDuration = 3f; // Takes 3 seconds to pick them up

            while (reviveTimer < reviveDuration)
            {
                // Cancel if the healer runs away or dies!
                if (healer == null || Vector3.Distance(transform.position, healer.transform.position) > 4f)
                {
                    targetHealth.isBeingRevived.value = false;
                    yield break;
                }

                reviveTimer += Time.deltaTime;
                yield return null;
            }

            // Success! The server officially brings them back.
            targetHealth.RevivePlayer();
        }

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