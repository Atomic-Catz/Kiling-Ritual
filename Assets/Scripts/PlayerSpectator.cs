using UnityEngine;
using PurrNet;
using System.Collections.Generic;
using TMPro;

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

            // Left and Right click to cycle players
            if (Input.GetMouseButtonDown(0)) CycleSpectator(1);
            if (Input.GetMouseButtonDown(1)) CycleSpectator(-1);
        }

        private void LateUpdate()
        {
            if (!isOwner || !isSpectating || myCam == null) return;

            if (alivePlayers.Count == 0 || alivePlayers[currentIndex] == null)
            {
                RefreshAlivePlayers();
                return; 
            }

            // Glue our camera directly to the target player's camera position and rotation
            Transform targetCam = alivePlayers[currentIndex].GetCameraWorld().transform;
            myCam.transform.position = targetCam.position;
            myCam.transform.rotation = targetCam.rotation;

            // Update UI to show who we are watching
            if (spectatingNameText != null)
            {
                string targetName = alivePlayers[currentIndex].gameObject.name.Replace("Player_", "");
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
                
                // Only spectate people who are not completely dead (downed is okay)
                if (health != null && health.GetCurrentHealth() > 0)
                {
                    alivePlayers.Add(c);
                }
            }
        }
    }
}