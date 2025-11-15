using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI")]
    public GameObject pauseMenuUI;

    public GameObject player;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        
        player.GetComponent<AudioSource>().enabled = true;
        player.GetComponentInChildren<CameraLook>().enabled = true;
        player.GetComponent<Movement>().enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;

        player.GetComponent<AudioSource>().enabled = false;
        player.GetComponentInChildren<CameraLook>().enabled = false;
        player.GetComponent<Movement>().enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}