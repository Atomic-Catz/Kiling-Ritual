using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using PurrNet;

namespace InfimaGames.LowPolyShooterPack
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Menu Panels")]
        public GameObject pauseMainPanel;
        public GameObject optionsPanel;

        [Header("Options Tabs")] 
        public GameObject audioTabPanel;
        public GameObject controlsTabPanel;
        public GameObject videoTabPanel;

        [Header("Scene Settings")]
        public string mainMenuSceneName = "MainMenu";

        [Header("Audio Settings")] 
        public AudioMixer mainMixer;

        [Header("Controls Settings UI")]
        public Slider sensitivitySlider;
        public Toggle invertYToggle;

        [Header("Video Settings UI")] 
        public TMP_Dropdown resolutionDropdown;
        public TMP_Dropdown qualityDropdown;
        public Toggle fullscreenToggle;

        private Resolution[] resolutions;

        public static bool IsPlayerDead = false;
        private Character localPlayer;

        private void Start()
        {
            // Hide panels on spawn
            if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);

            LoadControlSettings();
            InitializeVideoSettings();
        }

        private void Update()
        {
            if (IsPlayerDead) return;

            // Constantly try to find our local player if we haven't yet
            if (localPlayer == null)
            {
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

            // We handle the Escape key directly right here!
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool isAnyPanelOpen = (pauseMainPanel != null && pauseMainPanel.activeSelf) || 
                                      (optionsPanel != null && optionsPanel.activeSelf);

                if (isAnyPanelOpen)
                {
                    Resume(); // If it's open, close it
                }
                else
                {
                    Pause();  // If it's closed, open it
                }
            }
        }

        // ==========================================
        // MAIN PAUSE LOGIC
        // ==========================================

        public void Pause()
        {
            ShowMainPausePanel();
            
            // Unlock the mouse so you can click the buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Force Infima to freeze your camera and guns
            if (localPlayer != null)
            {
                localPlayer.SetMenuOpen(true);
            }
        }

        public void Resume()
        {
            // Close all UI panels
            if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            
            // Hide and lock the mouse again
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Force Infima to unfreeze your camera and guns
            if (localPlayer != null)
            {
                localPlayer.SetMenuOpen(false);
            }
        }

        public void Disconnect()
        {
            if (NetworkManager.main != null) Destroy(NetworkManager.main.gameObject);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ==========================================
        // PANEL NAVIGATION
        // ==========================================

        public void ShowMainPausePanel()
        {
            if (pauseMainPanel != null) pauseMainPanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        public void ShowOptionsPanel()
        {
            if (pauseMainPanel != null) pauseMainPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            ShowAudioTab(); 
        }

        public void ShowAudioTab() 
        { 
            if (audioTabPanel) audioTabPanel.SetActive(true); 
            if (controlsTabPanel) controlsTabPanel.SetActive(false); 
            if (videoTabPanel) videoTabPanel.SetActive(false); 
        }
        
        public void ShowControlsTab() 
        { 
            if (audioTabPanel) audioTabPanel.SetActive(false); 
            if (controlsTabPanel) controlsTabPanel.SetActive(true); 
            if (videoTabPanel) videoTabPanel.SetActive(false); 
        }
        
        public void ShowVideoTab() 
        { 
            if (audioTabPanel) audioTabPanel.SetActive(false); 
            if (controlsTabPanel) controlsTabPanel.SetActive(false); 
            if (videoTabPanel) videoTabPanel.SetActive(true); 
        }

        // ==========================================
        // SETTINGS LOGIC
        // ==========================================

        public void SetMasterVolume(float sliderValue) { if (mainMixer != null) mainMixer.SetFloat("MasterVol", Mathf.Log10(sliderValue) * 20); }
        public void SetMusicVolume(float sliderValue) { if (mainMixer != null) mainMixer.SetFloat("MusicVol", Mathf.Log10(sliderValue) * 20); }
        public void SetSFXVolume(float sliderValue) { if (mainMixer != null) mainMixer.SetFloat("SFXVol", Mathf.Log10(sliderValue) * 20); }

        private void LoadControlSettings()
        {
            if(sensitivitySlider != null) sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
            if(invertYToggle != null) invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
        }

        public void SetMouseSensitivity(float sensitivity) { PlayerPrefs.SetFloat("MouseSensitivity", sensitivity); }
        public void SetInvertY(bool isInverted) { PlayerPrefs.SetInt("InvertY", isInverted ? 1 : 0); }

        private void InitializeVideoSettings()
        {
            resolutions = Screen.resolutions;
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                List<string> options = new List<string>();
                int currentResolutionIndex = 0;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    string option = resolutions[i].width + " x " + resolutions[i].height;
                    options.Add(option);
                    if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
                        currentResolutionIndex = i;
                }
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
                resolutionDropdown.RefreshShownValue();
            }
            if (qualityDropdown != null) qualityDropdown.value = QualitySettings.GetQualityLevel();
            if (fullscreenToggle != null) fullscreenToggle.isOn = Screen.fullScreen;
        }
        
        public void SetResolution(int resolutionIndex) { Resolution res = resolutions[resolutionIndex]; Screen.SetResolution(res.width, res.height, Screen.fullScreen); PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex); }
        public void SetQuality(int qualityIndex) { QualitySettings.SetQualityLevel(qualityIndex); }
        public void SetFullscreen(bool isFullscreen) { Screen.fullScreen = isFullscreen; }
    }
}