using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class ColorSettings : MonoBehaviour
{
    public TMP_Dropdown agentDropDown;
    public TMP_Dropdown userDropDown;

    [Serializable]
    public struct nameColor
    {
        public string name;
        public Color color;
    }
    public nameColor[] colors;
    public Sprite baseImage;

    private Color colorTint = new Color(0.75f, 0.75f, 0.75f, 1f);
    private void Start()
    {
        agentDropDown.ClearOptions();
        userDropDown.ClearOptions();
        List<TMP_Dropdown.OptionData> colorItems = new List<TMP_Dropdown.OptionData>();
        foreach (var color in colors)
        {
            var colorOption = new ColorOptionData(color.name, baseImage ,color.color);
            //Debug.Log("Color: " + color.name + ", " + color.color);
            colorItems.Add(colorOption);
            
        }
        agentDropDown.AddOptions(colorItems);
        userDropDown.AddOptions(colorItems);
        agentDropDown.captionImage.color = colors[0].color * colorTint;
        userDropDown.captionImage.color = colors[0].color;
    }

    public void AgentColorChange(int choice)
    {
        agentDropDown.captionImage.color = colors[choice].color * colorTint;
        GameManager.Instance.SetAgentColor(colors[choice].color * colorTint);
    }

    public void UserColorChange(int choice)
    {
        userDropDown.captionImage.color = colors[choice].color;
        GameManager.Instance.SetUserColor(colors[choice].color);
    }
}
