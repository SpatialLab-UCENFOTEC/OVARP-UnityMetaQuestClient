// Bridges the Spatial Panel VR UI to OvarpServerConnector.
// Assign all fields in Inspector — no programmatic UI construction.
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class ServerSetupUI : MonoBehaviour
{
    [SerializeField] private OvarpServerConnector serverConnector;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private UnityEngine.UI.Button connectButton;
    [SerializeField] private GameObject spatialPanelRoot; // root GameObject to hide after connect
    [SerializeField] private UIDocument chatDocument;     // show after connect
    [SerializeField] private string defaultIp = "192.168.100.38";

    private const string PlayerPrefsKey = "ovarp_server_ip";
    private const string LegacyPlayerPrefsKey = "ovaf_server_ip";

    private void Start()
    {
        string saved = PlayerPrefs.GetString(PlayerPrefsKey, "");
        if (string.IsNullOrEmpty(saved) && PlayerPrefs.HasKey(LegacyPlayerPrefsKey))
        {
            saved = PlayerPrefs.GetString(LegacyPlayerPrefsKey, defaultIp);
            PlayerPrefs.SetString(PlayerPrefsKey, saved);
            PlayerPrefs.Save();
        }
        ipInputField.text = string.IsNullOrEmpty(saved) ? defaultIp : saved;

        connectButton.onClick.AddListener(OnConnectClicked);

        // XRI ray interactor sends pointer events that don't always trigger
        // TMP_InputField.ActivateInputField() (which opens TouchScreenKeyboard).
        // Force it explicitly on pointer click.
        var trigger = ipInputField.gameObject.GetComponent<EventTrigger>()
                      ?? ipInputField.gameObject.AddComponent<EventTrigger>();
        var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener(_ => ipInputField.ActivateInputField());
        trigger.triggers.Add(clickEntry);

        // Hide chat until server is connected — use style.display so Chat.OnEnable() still runs
        if (chatDocument != null)
        {
            chatDocument.rootVisualElement.style.display = DisplayStyle.None;
            chatDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
        }

        if (serverConnector != null)
            serverConnector.OnConnected += OnConnected;
        else
            Debug.LogError("[ServerSetupUI] serverConnector not assigned.");
    }

    private void OnDestroy()
    {
        if (serverConnector != null) serverConnector.OnConnected -= OnConnected;
        connectButton.onClick.RemoveListener(OnConnectClicked);
    }

    private void OnConnected()
    {
        if (chatDocument != null)
        {
            chatDocument.rootVisualElement.style.display = DisplayStyle.Flex;
            chatDocument.rootVisualElement.pickingMode = PickingMode.Position;
        }
        if (spatialPanelRoot != null) spatialPanelRoot.SetActive(false);
        enabled = false;
    }

    private void OnConnectClicked()
    {
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip)) ip = defaultIp;

        PlayerPrefs.SetString(PlayerPrefsKey, ip);
        PlayerPrefs.Save();

        if (serverConnector == null) { Debug.LogError("[ServerSetupUI] serverConnector not assigned."); return; }
        serverConnector.ConnectToServer($"ws://{ip}:8000");
    }
}
