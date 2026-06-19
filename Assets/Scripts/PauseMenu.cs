using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;
using InfimaGames.LowPolyShooterPack;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent GameObject that holds your Pause Menu buttons/background.")]
    public GameObject pauseUI;

    [Header("Scene Settings")]
    [Tooltip("The exact name of your Main Menu scene to load when disconnecting.")]
    public string mainMenuSceneName = "MainMenu";

    private Character localPlayer;
    private bool isMenuOpen = false;

    private void Start()
    {
        // Start with the menu hidden
        if (pauseUI != null) pauseUI.SetActive(false);

        // Find the local player as soon as this UI spawns
        Character[] players = FindObjectsOfType<Character>();
        foreach (var p in players)
        {
            if (p.isOwner)
            {
                localPlayer = p;
                break;
            }
        }
    }

    private void Update()
    {
        // Toggle the menu when pressing Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        
        if (pauseUI != null) 
            pauseUI.SetActive(isMenuOpen);

        // Tell the character script to unlock the cursor and stop shooting
        if (localPlayer != null)
        {
            localPlayer.SetMenuOpen(isMenuOpen);
        }
        else
        {
            // Fallback just in case the player hasn't fully initialized yet
            Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isMenuOpen;
        }
    }

    public void ResumeGame()
    {
        if (isMenuOpen) ToggleMenu();
    }

    public void Disconnect()
    {
        Debug.Log("Disconnecting from server...");

        // Safely destroy the network manager to sever the connection
        if (NetworkManager.main != null)
        {
            Destroy(NetworkManager.main.gameObject);
        }

        // FIX: Force the cursor to unlock and become visible for the Main Menu!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Load back into the main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
}