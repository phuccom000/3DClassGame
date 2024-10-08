using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using System;

public class TitleMenu : MonoBehaviour
{
    public GameObject mainMenuObject;
    public GameObject settingsObject;
    public GameObject deleteWorldConfirmation; // UI for confirmation prompt

    [Header("Main Menu UI Elements")]
    public TextMeshProUGUI seedField;

    [Header("Settings Menu UI Elements")]
    public Slider viewDstSlider;
    public TextMeshProUGUI viewDstText;
    public Slider mouseSlider;
    public TextMeshProUGUI mouseTxtSlider;
    public Toggle threadingToggle;
    public Toggle chunkAnimToggle;
    public TMP_Dropdown clouds;

    Settings settings;

    private void Awake()
    {
        if (!File.Exists(Application.dataPath + "/settings.cfg"))
        {
            Debug.Log("No settings file found, creating new one.");
            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        }
        else
        {
            Debug.Log("Settings file found, loading settings.");
            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
    }

    public void StartGame()
    {
        int rawSeed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.WorldCentre;
        VoxelData.seed = Mathf.Clamp(rawSeed, 1, 99999999);
        Debug.Log("Generated Seed: " + VoxelData.seed);  // For debugging purposes
        SceneManager.LoadScene("World", LoadSceneMode.Single);
    }

    public void EnterSettings()
    {
        viewDstSlider.value = settings.viewDistance;
        UpdateViewDstSlider();
        mouseSlider.value = settings.mouseSensitivity;
        UpdateMouseSlider();
        threadingToggle.isOn = settings.enableThreading;
        chunkAnimToggle.isOn = settings.enableAnimatedChunks;
        clouds.value = (int)settings.clouds;

        mainMenuObject.SetActive(false);
        settingsObject.SetActive(true);
    }

    public void LeaveSettings()
    {
        settings.viewDistance = (int)viewDstSlider.value;
        settings.mouseSensitivity = mouseSlider.value;
        settings.enableThreading = threadingToggle.isOn;
        settings.enableAnimatedChunks = chunkAnimToggle.isOn;
        settings.clouds = (CloudStyle)clouds.value;

        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        mainMenuObject.SetActive(true);
        settingsObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void UpdateViewDstSlider()
    {
        viewDstText.text = "View Distance: " + viewDstSlider.value;
    }

    public void UpdateMouseSlider()
    {
        mouseTxtSlider.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");
    }

    public void ConfirmDeleteWorld(string worldName)
    {
        deleteWorldConfirmation.SetActive(true); // Show confirmation UI
        mainMenuObject.SetActive(false);
        // You can set up a reference to the world name in your confirmation UI if needed
    }

    public void DeleteWorldConfirmed(string worldName)
    {
        SaveSystem.DeleteWorld(worldName);
        deleteWorldConfirmation.SetActive(false);
        mainMenuObject.SetActive(true);
        // Optionally, update the UI to reflect the world deletion
    }

    public void CancelDeleteWorld()
    {
        deleteWorldConfirmation.SetActive(false);
        mainMenuObject.SetActive(true);
    }
}
