using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DMText : MonoBehaviour
{
    private TextMeshProUGUI text;
    void Awake()
    {
        GameManager.OnDarkMode += DarkMode;
        text = this.GetComponent<TextMeshProUGUI>();
    }
    
    void OnDestroy()
    {
        GameManager.OnDarkMode -= DarkMode;
    }

    void DarkMode(bool state)
    {
        text.color = state ? Color.white : Color.black;
    }
}
