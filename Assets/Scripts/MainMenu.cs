using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using PurrNet;
using PurrNet.Transports;

public class MainMenu : MonoBehaviour
{
    // === NEW: STATIC MEMORY VARIABLES ===
    // These survive the scene load and tell Map1 what to do!
    public static bool connectAsHost = false;
    public static bool connectAsClient = false;
    public static string joinIP = "";

    [Header("Menu Panels")] 
    public GameObject mainMenuPanel;
    public GameObject optionsMenuPanel;
    public GameObject playMenuPanel;

    [Header("Options Tabs")] 
    public GameObject audioTabPanel;
    public GameObject controlsTabPanel;
    public GameObject videoTabPanel;

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

    [Header("Network Settings")] 
    public TMP_InputField ipInputField;
    public TMP_Text networkErrorText;
    public TMP_Text hostIpText;
    public string defaultIP = "127.0.0.1";
    
    [Header("Scene Settings")] 
    public string gameSceneName = "Map1";

    private void Start()
    {
        ShowMainMenu();
        LoadControlSettings();
        InitializeVideoSettings();
    }

    // ==========================================
    // MENU NAVIGATION
    // ==========================================

    public void ShowMainMenu() { mainMenuPanel.SetActive(true); optionsMenuPanel.SetActive(false); if (playMenuPanel != null) playMenuPanel.SetActive(false); }
    public void ShowOptions() { mainMenuPanel.SetActive(false); optionsMenuPanel.SetActive(true); if (playMenuPanel != null) playMenuPanel.SetActive(false); ShowAudioTab(); }
    public void ShowPlayMenu() { mainMenuPanel.SetActive(false); optionsMenuPanel.SetActive(false); if (playMenuPanel != null) playMenuPanel.SetActive(true); if (hostIpText != null) StartCoroutine(FetchPublicIP()); }
    public void ShowAudioTab() { audioTabPanel.SetActive(true); controlsTabPanel.SetActive(false); videoTabPanel.SetActive(false); }
    public void ShowControlsTab() { audioTabPanel.SetActive(false); controlsTabPanel.SetActive(true); videoTabPanel.SetActive(false); }
    public void ShowVideoTab() { audioTabPanel.SetActive(false); controlsTabPanel.SetActive(false); videoTabPanel.SetActive(true); }
    
    // ==========================================
    // MULTIPLAYER PLAY SECTION (UPDATED)
    // ==========================================

    public void PlaySolo()
    {
        Debug.Log("Starting Solo Game...");
        ConnectAndLoad(true);
    }

    public void HostLobby()
    {
        Debug.Log("Loading Map as Host...");
        ConnectAndLoad(true);
    }

    public void JoinLobby() 
    { 
        if (string.IsNullOrWhiteSpace(ipInputField.text))
        {
            if (networkErrorText != null) networkErrorText.text = "PLEASE ENTER AN IP ADDRESS";
            return; 
        }

        if (networkErrorText != null) networkErrorText.text = "";
        ConnectAndLoad(false);
    }

    private void ConnectAndLoad(bool isHost)
    {
        // 1. Set the static memory variables for Map1 to read
        if (isHost)
        {
            connectAsHost = true;
            connectAsClient = false;
        }
        else
        {
            connectAsClient = true;
            connectAsHost = false;
            joinIP = string.IsNullOrWhiteSpace(ipInputField.text) ? defaultIP : ipInputField.text;
        }

        // 2. Simply load the map! No waiting, no freezing.
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator FetchPublicIP()
    {
        hostIpText.text = "Fetching IP...";
        using (UnityWebRequest webRequest = UnityWebRequest.Get("https://api.ipify.org"))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                hostIpText.text = "Your IP: Offline";
            else
                hostIpText.text = "Your IP: " + webRequest.downloadHandler.text;
        }
    }
    
    // ==========================================
    // SETTINGS LOGIC
    // ==========================================

    public void SetMasterVolume(float sliderValue) { mainMixer.SetFloat("MasterVol", Mathf.Log10(sliderValue) * 20); }
    public void SetMusicVolume(float sliderValue) { mainMixer.SetFloat("MusicVol", Mathf.Log10(sliderValue) * 20); }
    public void SetSFXVolume(float sliderValue) { mainMixer.SetFloat("SFXVol", Mathf.Log10(sliderValue) * 20); }

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
    public void QuitGame() { Application.Quit(); }
}