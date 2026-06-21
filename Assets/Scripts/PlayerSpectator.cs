using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem; // REQUIRED for Infima's Input System

namespace InfimaGames.LowPolyShooterPack
{
    public class PlayerSpectator : NetworkBehaviour
    {
        [Header("Spectator UI")]
        public GameObject spectatorUI;
        public TextMeshProUGUI spectatingNameText;

        private Camera myCam;
        private Vector3 originalCamLocalPos;
        private Quaternion originalCamLocalRot;

        private List<Character> alivePlayers = new List<Character>();
        private int currentIndex = 0;
        private bool isSpectating = false;

        private void Start()
        {
            if (spectatorUI != null) spectatorUI.SetActive(false);
            
            var charScript = GetComponent<Character>();
            if (charScript != null && charScript.GetCameraWorld() != null)
            {
                myCam = charScript.GetCameraWorld();
                originalCamLocalPos = myCam.transform.localPosition;
                originalCamLocalRot = myCam.transform.localRotation;
            }
        }

        public void StartSpectating()
        {
            if (!isOwner || myCam == null) return;
            
            isSpectating = true;
            if (spectatorUI != null) spectatorUI.SetActive(true);
            
            RefreshAlivePlayers();
        }

        public void StopSpectating()
        {
            if (!isOwner || myCam == null) return;
            
            isSpectating = false;
            if (spectatorUI != null) spectatorUI.SetActive(false);

            // Snap camera back into our own head
            myCam.transform.localPosition = originalCamLocalPos;
            myCam.transform.localRotation = originalCamLocalRot;
        }

        private void Update()
        {
            if (!isOwner || !isSpectating) return;

            // FIX 1: Use the New Input System so clients can actually click!
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) CycleSpectator(1);
                if (Mouse.current.rightButton.wasPressedThisFrame) CycleSpectator(-1);
            }
        }

        private void LateUpdate()
        {
            if (!isOwner || !isSpectating || myCam == null) return;

            if (alivePlayers.Count == 0 || alivePlayers[currentIndex] == null)
            {
                RefreshAlivePlayers();
                if (alivePlayers.Count == 0) return; 
            }

            Character target = alivePlayers[currentIndex];

            // FIX 2: Safely find the target's camera transform, even if the camera is disabled on this client!
            Camera targetCam = target.GetComponentInChildren<Camera>(true);
            
            if (targetCam != null)
            {
                myCam.transform.position = targetCam.transform.position;
                myCam.transform.rotation = targetCam.transform.rotation;
            }
            else
            {
                // Bulletproof Fallback: Put the camera at their head height
                myCam.transform.position = target.transform.position + new Vector3(0, 1.5f, 0);
                myCam.transform.rotation = target.transform.rotation;
            }

            // Update UI to show who we are watching
            if (spectatingNameText != null)
            {
                string targetName = target.gameObject.name.Replace("Player_", "").Replace("(Clone)", "");
                spectatingNameText.text = $"Spectating: {targetName}";
            }
        }

        private void CycleSpectator(int direction)
        {
            RefreshAlivePlayers();
            if (alivePlayers.Count == 0) return;

            currentIndex += direction;
            
            // Wrap around if we hit the end of the list
            if (currentIndex >= alivePlayers.Count) currentIndex = 0;
            if (currentIndex < 0) currentIndex = alivePlayers.Count - 1;
        }

        private void RefreshAlivePlayers()
        {
            alivePlayers.Clear();
            Character[] allChars = FindObjectsOfType<Character>();
            
            foreach (var c in allChars)
            {
                if (c == this.GetComponent<Character>()) continue; // Don't spectate myself

                var health = c.GetComponent<CharacterHealth>();
                
                // FIX 3: Rely on the SyncVar we just created!
                if (health != null && !health.isDead.value)
                {
                    alivePlayers.Add(c);
                }
            }
        }
    }
}