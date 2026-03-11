using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DarkMode : MonoBehaviour
{
    public Image settingsBackground;
    public SpriteRenderer sceneBackground;
    public GameObject chat;
    public TMP_ColorGradient textColor;

    public Color darkColor;
    public Color lightColor;
    
    
    public void SetDarkMode(bool state)
    {
        GameManager.Instance.SetDarkMode(state);
        if (state)
        {
            //dark
            settingsBackground.color = darkColor;
            sceneBackground.color = darkColor;
            chat.GetComponent<Chat>().DarkMode(true, darkColor);

        }
        else
        {
            settingsBackground.color = lightColor;
            sceneBackground.color = lightColor;
            chat.GetComponent<Chat>().DarkMode(false, lightColor);
        }
    }
}
