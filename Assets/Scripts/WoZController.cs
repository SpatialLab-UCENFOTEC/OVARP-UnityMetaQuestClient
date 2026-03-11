using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Triggers Controller.ProcessInput() — Space on desktop, A button on Quest
public class WoZController : MonoBehaviour
{
    public Controller Controller;
    public GameObject TheWorldController;

    private bool _prevButtonState = false;

    void Start()
    {
        TheWorldController = GameObject.Find("Controller");
        Controller = TheWorldController.GetComponent<Controller>();
    }

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Quest right controller — primary button (A) toggles recording
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, devices);

        // Trigger value > 0.5 = pressed (right trigger toggles mic)
        float triggerValue = 0f;
        bool pressed = false;
        if (devices.Count > 0)
        {
            devices[0].TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
            pressed = triggerValue > 0.5f;
        }

        if (pressed && !_prevButtonState)
            Controller.ProcessInput();

        _prevButtonState = pressed;
#else
        if (Input.GetKeyUp(KeyCode.Space))
            Controller.ProcessInput();
#endif
    }
}
