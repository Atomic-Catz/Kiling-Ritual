using UnityEngine;
using TMPro;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class PlayerNametag : NetworkBehaviour
    {
        [Header("UI Setup")]
        public TextMeshProUGUI nametagText;
        public Canvas nametagCanvas;

        private Camera mainCamera;
        private const string NamePrefsKey = "PlayerNametag";

        // Use a SyncVar so anyone who joins late automatically gets the name!
        [SerializeField] private SyncVar<string> playerName = new SyncVar<string>("Player");

        private void Awake()
        {
            // Subscribe to the SyncVar change event
            playerName.onChanged += OnNameChanged;
            mainCamera = Camera.main;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
            {
                // If this is OUR player, get our saved name and ask the server to set it
                string myName = PlayerPrefs.GetString(NamePrefsKey, "Survivor_" + Random.Range(1000, 9999));
                CmdSetPlayerName(myName);

                // Hide our own nametag so it doesn't block our view
                if (nametagCanvas != null)
                {
                    nametagCanvas.gameObject.SetActive(false);
                }
            }
            
            // Force the UI to update immediately when they spawn in
            UpdateNametagUI(playerName.value);
        }

        private void Update()
        {
            // Billboard effect: Make the nametag always face the local player's camera
            if (!isOwner && nametagCanvas != null && mainCamera != null)
            {
                nametagCanvas.transform.LookAt(nametagCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                               mainCamera.transform.rotation * Vector3.up);
            }
        }

        // --- NETWORK SYNC ---

        [ServerRpc]
        private void CmdSetPlayerName(string newName)
        {
            // The server sets the SyncVar, which automatically tells all clients!
            playerName.value = newName;
        }

        private void OnNameChanged(string newName)
        {
            UpdateNametagUI(newName);
        }

        private void UpdateNametagUI(string newName)
        {
            if (nametagText != null)
            {
                nametagText.text = newName;
            }
            
            // Rename the GameObject in the Unity hierarchy for easier debugging
            gameObject.name = $"Player_{newName}";
        }
    }
}