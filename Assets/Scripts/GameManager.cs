using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class GameManager : NetworkBehaviour
    {
        // Singleton pattern so other scripts can easily find it
        public static GameManager Instance;

        // The network variable that tells everyone the game is lost
        [SerializeField] private SyncVar<bool> isGameOver = new SyncVar<bool>(false);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Listen for when the server flips this to true
            isGameOver.onChanged += OnGameOverChanged;
        }

        public void CheckGameOverCondition()
        {
            // Only the Server is allowed to declare a Game Over
            if (!isServer || isGameOver.value) return;

            CharacterHealth[] allPlayers = FindObjectsOfType<CharacterHealth>();
            if (allPlayers.Length == 0) return;

            bool allDefeated = true;

            foreach (var player in allPlayers)
            {
                // If even ONE player is NOT downed and NOT dead, the game continues!
                if (!player.isDowned.value && !player.isDead.value)
                {
                    allDefeated = false;
                    break;
                }
            }

            if (allDefeated)
            {
                TriggerGameOver();
            }
        }

        private void TriggerGameOver()
        {
            isGameOver.value = true;
            Debug.Log("[GameManager] ALL PLAYERS DOWN! GAME OVER!");
        }

        private void OnGameOverChanged(bool gameOver)
        {
            if (gameOver)
            {
                // Tell the global DeathMenu to show itself on this client!
                DeathMenu deathMenu = FindObjectOfType<DeathMenu>(true);
                if (deathMenu != null)
                {
                    deathMenu.Show();
                }

                // Stop the local player from shooting/moving while looking at the menu
                Character localPlayer = GetLocalPlayer();
                if (localPlayer != null)
                {
                    var input = localPlayer.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                    if (input != null) input.enabled = false;
                }
            }
        }

        // Helper method to find the local player's character script
        private Character GetLocalPlayer()
        {
            Character[] players = FindObjectsOfType<Character>();
            foreach (var p in players)
            {
                if (p.isOwner) return p;
            }
            return null;
        }
    }
}