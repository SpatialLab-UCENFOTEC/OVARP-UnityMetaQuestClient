using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SettingsUI : MonoBehaviour
{
    public Button customizationSettings;
    public Button settings;
    public GameObject panel;
    public GameObject cutomizationScreen;
    public GameObject settingsScreen;

    private void Start()
    {
        panel.SetActive(false);
        cutomizationScreen.SetActive(false);
        settingsScreen.SetActive(false);
    }

    public void ShowCustomization(bool active)
    {
        panel.SetActive(active);
        settingsScreen.SetActive(false);
        cutomizationScreen.SetActive(active);
        
    }
    
    public void ShowSettings(bool active)
    {
        panel.SetActive(active);
        cutomizationScreen.SetActive(false);
        settingsScreen.SetActive(active);
    }

    private void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            panel.SetActive(false);
            cutomizationScreen.SetActive(false);
            settingsScreen.SetActive(false);
        }
    }
}
