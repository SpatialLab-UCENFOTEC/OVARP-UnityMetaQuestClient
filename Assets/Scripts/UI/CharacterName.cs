using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterName : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    private void Awake()
    {
        GameManager.OnAgentNameChange += NameChange;
    }

    private void OnDestroy()
    {
        GameManager.OnAgentNameChange -= NameChange;
    }

    void NameChange(string name)
    {
        nameText.text = name;
    }
}
