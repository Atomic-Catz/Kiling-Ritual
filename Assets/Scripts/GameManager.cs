using UnityEngine;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class GameManager : NetworkBehaviour
    {
        // Singleton pattern so other scripts can easily find it
        public static GameManager Instance;

        [Header("UI")]
        [Tooltip("The screen that pops up when everyone dies.")]
        [SerializeField] private GameObject gameOverScreen;

        // The network variable that tells everyone the game is lost
        [SerializeField] private SyncVar<bool> isGameOver = new SyncVar<bool>(false);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Listen for when the server flips this to true
            isGameOver.onChanged += OnGameOverChanged;
        }

        private void Start()
        {
            // Ensure the screen is off when the game starts
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
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
                // If even ONE player is alive and NOT downed, the game continues!
                if (!player.isDowned.value && player.GetCurrentHealth() > 0)
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
            
            // TODO: Optional Future Polish
            // 1. Find all Enemy AI and force them into an "Idle" or "Cheer" animation
            // 2. Stop the WaveManager from spawning new zombies
            // 3. Play a spooky Game Over audio track
        }

        private void OnGameOverChanged(bool gameOver)
        {
            if (gameOver)
            {
                // Show Game Over UI for everyone on the network
                if (gameOverScreen != null) gameOverScreen.SetActive(true);

                // Unlock the mouse so players can click "Main Menu", "Restart", or "Quit"
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Stop the local player from shooting/moving while looking at the menu
                Character localPlayer = GetLocalPlayer();
                if (localPlayer != null)
                {
                    localPlayer.SetMenuOpen(true);
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