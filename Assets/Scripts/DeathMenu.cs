using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet; // Needed to safely disconnect

public class DeathMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject deathMenuUI;

    [Header("Scene Settings")]
    [Tooltip("The exact name of your Main Menu scene to load when disconnecting.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isShown = false;

    public void Show()
    {
        if (isShown) return;
        isShown = true;

        if (deathMenuUI != null) deathMenuUI.SetActive(true);

        // Unlock cursor so the player can click the buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Replace your UI "Retry" button with a "Disconnect" button
    public void Disconnect()
    {
        Debug.Log("Disconnecting from server...");

        // Safely destroy the network manager to sever the connection
        if (NetworkManager.main != null)
        {
            Destroy(NetworkManager.main.gameObject);
        }

        // Load back into the main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void Quit()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}