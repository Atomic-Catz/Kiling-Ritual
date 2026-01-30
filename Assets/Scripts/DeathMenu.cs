using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject deathMenuUI;

    [Header("Player")]
    public GameObject player;

    private bool isShown = false;

    public void Show()
    {
        
        PauseMenu.IsPlayerDead = true;

        if (isShown) return;
        isShown = true;

        deathMenuUI.SetActive(true);
        Time.timeScale = 0f;

        // Disable player control
        if (player != null)
        {
            var movement = player.GetComponent<Movement>();
            if (movement) movement.enabled = false;

            var look = player.GetComponentInChildren<CameraLook>();
            if (look) look.enabled = false;

            var audio = player.GetComponent<AudioSource>();
            if (audio) audio.enabled = false;
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}