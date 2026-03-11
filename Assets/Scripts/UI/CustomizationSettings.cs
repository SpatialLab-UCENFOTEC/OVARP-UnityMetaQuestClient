using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationSettings : MonoBehaviour
{
    //private string agentNameInput;
    public TMP_InputField agentInput;
    public TMP_InputField userInput;

    private void Start()
    {
        agentInput = GameObject.Find("Avatar InputField (TMP)").GetComponent<TMP_InputField>();
        userInput = GameObject.Find("User InputField (TMP)").GetComponent<TMP_InputField>();
        agentInput.text = GameManager.Instance.GetAgentName();
        userInput.text = GameManager.Instance.GetUserName();
    }

    public void ReadAgentNameInput()
    {
        //GameManager.Instance.SetAgentName(s);
        GameManager.Instance.SetAgentName(agentInput.text);
    }
    
    public void ReadUserNameInput()
    {
        GameManager.Instance.SetUserName(userInput.text);
    }
}
