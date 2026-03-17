using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using System;
using System.IO;
using System.Text;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif



[RequireComponent(typeof(AudioSource))]
public class Controller : MonoBehaviour
{

    [System.Serializable]
    public class Message
    {
        public string text;

        
        public Message(string text)
        {
            this.text = text;
        }

        public static Message CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<Message>(jsonString);
        }
    }

    // WebGL-only JS interop — no-ops on Editor/Standalone
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Hello(string param);

    [DllImport("__Internal")]
    private static extern void StoreEvents(string param);

    [DllImport("__Internal")]
    private static extern void Print(string param);

    [DllImport("__Internal")]
    private static extern void QuitGame();
#else
    private static void Hello(string param) { Debug.Log("[Hello] " + param); }
    private static void StoreEvents(string param) { Debug.Log("[StoreEvents] " + param); }
    private static void Print(string param) { Debug.Log("[Print] " + param); }
    private static void QuitGame() { Application.Quit(); }
#endif

    [System.Serializable]
    public class ChatCompletion
    {
        public string id;
        public string created;
        public string model;
        public Usage usage;
        public Choice[] choices;
    }

    [System.Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    [System.Serializable]
    public class Choice
    {
        public ChatGPTMessage message;
        public string finish_reason;
        public int index;
    }

    [System.Serializable]
    public class ChatGPTMessage
    {
        public string role;
        public string content;
    }


    [System.Serializable]
    public class Event
    {

        public Event(string evnt, string dt)
        {
            this.eventName = evnt;
            this.date = dt;
        }

        public string eventName;
        public string date;
    }

    // Set this in the Unity Inspector — never hardcode secrets in source
    [SerializeField] private string openAIApiKey;

    public AudioSource audioSource;
    public AudioSource audioSource2;
    public AgentState CurrentState { get; private set; }
    public string[] CurrentIntent { get; private set; }
    public string CurrentUtterance { get; private set; }
    private Queue<string> utteranceQueue = new Queue<string>();
    private Queue<string[]> intentQueue = new Queue<string[]>();
    public bool paused;
    public float audioFadeIn = .2f;
    public float audioFadeOut = .3f;
    public enum AgentState
    {
        Idle,
        Waiting,
        Speaking
    }


    public GameObject gameObject;
    public GameObject agent;  //Agent

    public GameObject LipSync;
    public LipSync lipSync;

    private string outputFilePath;


    public Chat chat;
    public GameObject ChatObject;

    public Queue<string> responses;

    public AnimController anim;
    public RecIndicator recIndicator;

    private List<string> messages;
    private List<string> messagesForEvent;

    private string rememberToAnswerInShort = " ";

    private bool canAskForElse = false;
    private bool isAudioReady = false;
    private bool isListening = false;
    private bool hasMicPermission = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(RequestMicrophonePermission());
        gameObject = GameObject.Find("Client");
        // client = gameObject.GetComponent<UdpSocket>();
        // audioSource = agent.gameObject.GetComponent<AudioSource>();
        LipSync = GameObject.Find("Frank");
        lipSync = LipSync.GetComponent<LipSync>();

        CurrentState = AgentState.Idle;
        audioSource2 = GetComponent<AudioSource>();

        ChatObject = GameObject.Find("Chat UIDocument");
        chat = ChatObject.GetComponent<Chat>();
        responses = new Queue<string>();

        anim = GameObject.Find("Frank").GetComponent<AnimController>();
        recIndicator = GameObject.Find("Sphere").GetComponent<RecIndicator>();

        messages = new List<string>();
        messagesForEvent = new List<string>();
        string name = "Daniel";
        // messages.Add("{\"role\": \"system\", \"content\": \"You are my best friend. Answer in 3 sentences or less and be friendly. Ask for more information if needed. You need to respond like a real person in a conversational tone. You are not to reveal you are not a real person. If asked for advice, always answer as if you were a person. Remember my name is Rodrigo.\"}");
        messagesForEvent.Add("{\"role\": \"system\", \"content\": \"You are an assistant that is supposed to get events from text. I will give you a message and today's date and you should reply with event and date. So if the message is 'I have a flower contest on friday. Today:11/20/23, Tuesday' then you would return the following: 'Event: Flower event. Date: 11/24/24.' If there is no event to extract then return the string 'No event'\"}");

        // Guard: warn early if API key is missing so the error is visible in logs/ScreenLogger
        if (string.IsNullOrEmpty(openAIApiKey))
            Debug.LogError("[Controller] openAIApiKey is not set. Set it in the Inspector on the Controller GameObject.");

        // Default Nova persona — SetName() overrides this when called from UI/WebGL
        if (messages.Count == 0)
        {
            string today = DateTime.Now.ToString("MM/dd/yyyy");
            messages.Add("{\"role\": \"system\", \"content\": \"DateFormat: mm/dd/yyyy. Today: " + today + ".\"}");
            messages.Add("{\"role\": \"system\", \"content\": \"You are a trusted companion named Nova. Answer in 3 sentences or less. Be friendly, curious, and conversational. Never reveal you are not human.\"}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        FixAudioClicks();

        if (CurrentState == AgentState.Idle)
        {
            // Process next utterance in the queue
            if (!paused && utteranceQueue.Count > 0)
            {
                CurrentState = AgentState.Waiting;
                CurrentUtterance = utteranceQueue.Dequeue();
                CurrentIntent = intentQueue.Dequeue();
                StartCoroutine(SynthesizeSpeech(CurrentUtterance, "tts-1", "shimmer", openAIApiKey));
            }
        }
        else if (CurrentState == AgentState.Waiting)
        {
            // Wait for TTS to finish getting audio
            if (isAudioReady)
            {
                CurrentState = AgentState.Speaking;
                // audioSource.clip = textToSpeech.SpeechAudio;
                // audioSource.volume = 0;
                // lipSync.AnalyzeAudioClip(audioSource.clip);
                StartCoroutine(lipSync.AnalyzeAudioClip(audioSource.clip));
                // audioSource.Play();
            }
             
        }
        else if (CurrentState == AgentState.Speaking)
        {
            // Wait for speech to finish playing
            if (!audioSource.isPlaying)
            {
                CurrentState = AgentState.Idle;
                isAudioReady = false;
                // CurrentUtterance = null;
                CurrentIntent = null;
                
            }
            
                
        }

        if(responses.Count != 0)
        {
            chat.killTempAgent();
            chat.sendAgentMessage(responses.Dequeue());
        }

        CheckInputTriggers();
    }

    private bool rightTriggerWasPressed = false;

    private void CheckInputTriggers()
    {
        // Desktop: spacebar toggle
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ProcessInput();
            return;
        }

        // Meta Quest: right controller trigger (press to start, release to stop)
        var rightHandDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightHandDevices
        );

        if (rightHandDevices.Count > 0)
        {
            rightHandDevices[0].TryGetFeatureValue(XRCommonUsages.triggerButton, out bool triggerPressed);
            if (triggerPressed && !rightTriggerWasPressed)
                ProcessInput();
            else if (!triggerPressed && rightTriggerWasPressed)
                ProcessInput();
            rightTriggerWasPressed = triggerPressed;
        }
    }


    public void ProcessInput()
    {
        if(!isListening)
        {
            recIndicator.StartBlinking();
            StartRecording();
            chat.SendTempUser();
            isListening = true;
        }
        else
        {
            recIndicator.StopBlinking();
            StopRecording();
            anim.StartThinking();
            isListening = false;
        }
    }

    public void SetName(string myString)
    {
        DateTime today = DateTime.Now;
        string formattedDate = today.ToString("MM/dd/yyyy");
        string[] elements = myString.Split('|');
        Print(elements[1]);
        messages.Add("{\"role\": \"system\", \"content\": \"DateFormat: mm/dd/yyyy, Events: Today:" + formattedDate +", " + elements[1] +"\"}");
        messages.Add("{\"role\": \"system\", \"content\": \"You are a trusted companion and your name is Nova. Answer in 3 sentences or less if possible. Maintain a conversational tone and engage in meaningful dialogue with the user. Show curiosity about their life by asking follow-up questions on their shared experiences or topics discussed. Utilize past conversations to reinforce your personal connection with them. Your purpose is to provide a friendly, nurturing presence and when necessary, provide advice as any good friend would. Under NO circumstances should you reveal that you're not a human, pretend you are human always. Remember, their name is " + elements[0] + ". You're not just a system but a friend. Be supportive, attentive and conversational, always treating every user interaction as a valuable part of your ongoing relationship.\"}");
    }

    public void ReceiveMessage(string text, bool askForAnythingElse = false)
    {
        anim.StopThinking();
        utteranceQueue.Enqueue(text);
        intentQueue.Enqueue(null);
        responses.Enqueue(text);
        Hello(text);
        // if(askForAnythingElse && canAskForElse)
        // {
        //     SpeakIntent("Is there anything else I can do for you?");
        //     responses.Enqueue("Is there anything else I can do for you?");
        // }
        // if(!canAskForElse)
        // {
        //     canAskForElse = true;
        // }
        
    }

    IEnumerator RequestMicrophonePermission()
    {
        // Android/Quest require explicit runtime permission for microphone
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        hasMicPermission = Application.HasUserAuthorization(UserAuthorization.Microphone);
        if (!hasMicPermission)
            Debug.LogError("Microphone permission denied — voice input will not work.");
    }

    public void StartRecording() {
        if (!hasMicPermission)
        {
            Debug.LogWarning("StartRecording: microphone permission not granted.");
            return;
        }
        outputFilePath = Application.persistentDataPath + "/mic.wav";
        // Loop=true, maxLength=10s — enough for a conversational turn
        audioSource2.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
    }

    public void StopRecording() {
        // Capture actual recorded length before stopping to avoid trailing silence
        int recordedSamples = Microphone.GetPosition(null);
        Microphone.End(null);

        if (recordedSamples <= 0 || audioSource2.clip == null)
        {
            Debug.LogWarning("StopRecording: no audio captured.");
            return;
        }

        // Trim clip to recorded length
        int channels = audioSource2.clip.channels;
        float[] data = new float[recordedSamples * channels];
        audioSource2.clip.GetData(data, 0);
        AudioClip trimmed = AudioClip.Create("mic", recordedSamples, channels, audioSource2.clip.frequency, false);
        trimmed.SetData(data, 0);

        SavWav.Save("mic.wav", trimmed);
        StartCoroutine(TranscribeAudio(outputFilePath, "whisper-1", openAIApiKey));
    }

    public void ReceiveString(string msg)
    {
        chat.killTempUser();
        chat.sendUserMessage(msg);
        DateTime today = DateTime.Now;
        string formattedDateAndDay = today.ToString("MM/dd/yyyy, dddd");
        messages.Add("{\"role\": \"user\", \"content\": \"" + msg + "\"}");
        messagesForEvent.Add("{\"role\": \"user\", \"content\": \"Today:" + formattedDateAndDay + ", Text: " + msg + "\"}");
        StartCoroutine(GetEventFromAPI("gpt-3.5-turbo", openAIApiKey, msg));
        StartCoroutine(GetOpenAIChatResponse("gpt-4", openAIApiKey, msg));
        // client.SendData(msg.text);
        chat.SendTempAgent();
    }


    public void SpeakIntent(string message, bool immediate=false)
    {
        Speak(message, immediate);
    }

    public void Speak(string utterance, bool immediate=false, string[] intent=null)
    {
        if (immediate)
        {
            utteranceQueue.Clear();
            intentQueue.Clear();
            CurrentState = AgentState.Idle;
        }
        utteranceQueue.Enqueue(utterance);
        intentQueue.Enqueue(intent);
    }


    // private void RequestAudio(string utterance)
    // {
    //     CurrentState = AgentState.Waiting;
    //     textToSpeech.RequestSpeechAudio(utterance);
    // }

    private void FixAudioClicks()
    {
        // Fade in/out to hide TTS audio clicks at start/end
        if (audioSource.clip == null) return;

        if (audioSource.time < audioFadeIn)
        {
            audioSource.volume = audioSource.time / audioFadeIn;
        }
        else if (audioSource.time > audioSource.clip.length - audioFadeOut)
        {
            audioSource.volume = (audioSource.clip.length - audioSource.time) / audioFadeOut;
        }
        else
        {
            audioSource.volume = 1;
        }
    }

    IEnumerator TranscribeAudio(string filePath, string modelName, string apiKey) 
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("model", modelName));
        formData.Add(new MultipartFormFileSection("file", File.ReadAllBytes(filePath), "mic.wav", "audio/wav"));

        UnityWebRequest www = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", formData);
        www.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Error: " + www.error);
        } else {
            Debug.Log("Response: " + www.downloadHandler.text);
            Message msg = Message.CreateFromJSON(www.downloadHandler.text);
            www.disposeUploadHandlerOnDispose = true;
            www.disposeDownloadHandlerOnDispose = true;
            www.Dispose();
            chat.killTempUser();
            chat.sendUserMessage(msg.text);
            messages.Add("{\"role\": \"user\", \"content\": \"" + msg.text + rememberToAnswerInShort + "\"}");
            DateTime today = DateTime.Now;
            string formattedDateAndDay = today.ToString("MM/dd/yy, dddd");
            messagesForEvent.Add("{\"role\": \"user\", \"content\": \"Today:" + formattedDateAndDay + ", Text: " + msg.text + "\"}");
            StartCoroutine(GetEventFromAPI("gpt-3.5-turbo", openAIApiKey, msg.text));
            StartCoroutine(GetOpenAIChatResponse("gpt-4", openAIApiKey, msg.text));
            // client.SendData(msg.text);
            chat.SendTempAgent();
        }
    }

    private string GetMessagesString()
    {
        string temp = "[";
        foreach (var x in messages)
        {
            temp += x;
            temp += ",";
        }
        return temp.Remove(temp.Length - 1) + "]";
    }

    private string GetEventsString()
    {
        string temp = "[";
        foreach (var x in messagesForEvent)
        {
            temp += x;
            temp += ",";
        }
        return temp.Remove(temp.Length - 1) + "]";
    }

    // IEnumerator GetOpenAIChatResponse(string modelName, string apiKey, string prevMsg) 
    // {
    //     string url = "https://api.openai.com/v1/chat/completions";

    //     Dictionary<string, string> headers = new Dictionary<string, string>();
    //     headers.Add("Authorization", "Bearer " + apiKey);
    //     headers.Add("Content-Type", "application/json");

    //     // string requestData = "{\"model\": \"" + modelName + "\", \"messages\": [{\"role\": \"user\", \"content\": \"" + userInput + "\"}]}";
    //     string transcript = GetMessagesString();
    //     string requestData = "{\"model\": \"" + modelName + "\", \"messages\":" + transcript + "}";

    //     UnityWebRequest www = UnityWebRequest.Post(url, "");
    //     byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData);
    //     www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
    //     www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

    //     foreach (KeyValuePair<string, string> header in headers) {
    //         www.SetRequestHeader(header.Key, header.Value);
    //     }

    //     yield return www.SendWebRequest();

    //     if (www.result != UnityWebRequest.Result.Success) {
    //         Debug.LogError("Error: " + www.error);
    //     } else {
    //         ChatCompletion chatCompletion = JsonUtility.FromJson<ChatCompletion>(www.downloadHandler.text);
    //         string messageContent = chatCompletion.choices[0].message.content;
    //         messages.Add("{\"role\": \"assistant\", \"content\": \"" + messageContent + "\"}");
    //         if(prevMsg.Length < 25)
    //         {
    //             ReceiveMessage(messageContent,true);
    //         }
    //         else
    //         {
    //             ReceiveMessage(messageContent);
    //         }
    //         // Message msg = Message.CreateFromJSON(www.downloadHandler.text);
    //         // chat.sendUserMessage(msg.text);
    //         // client.SendData(msg.text);
    //     }

    //     www.disposeDownloadHandlerOnDispose = true;
    //     www.disposeUploadHandlerOnDispose = true;
    //     www.Dispose();
    // }

    IEnumerator GetOpenAIChatResponse(string modelName, string apiKey, string prevMsg) 
    {
        string url = "https://api.openai.com/v1/chat/completions";

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", "Bearer " + apiKey);
        headers.Add("Content-Type", "application/json");

        string transcript = GetMessagesString();
        string requestData = "{\"model\": \"" + modelName + "\", \"messages\":" + transcript + "}";

        UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        foreach (KeyValuePair<string, string> header in headers) {
            www.SetRequestHeader(header.Key, header.Value);
        }

        int retryLimit = 2;
        int retryCount = 0;

        while (retryCount < retryLimit) {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError("Error: " + www.error);
                retryCount++;
                Debug.Log("Retry attempt: " + retryCount);
            } else {
                ChatCompletion chatCompletion = JsonUtility.FromJson<ChatCompletion>(www.downloadHandler.text);
                string messageContent = chatCompletion.choices[0].message.content;
                messages.Add("{\"role\": \"assistant\", \"content\": \"" + messageContent + "\"}");
                if(prevMsg.Length < 25)
                {
                    ReceiveMessage(messageContent,true);
                }
                else
                {
                    ReceiveMessage(messageContent);
                }
                yield break; // Successful response, exit the loop
            }

            www.disposeDownloadHandlerOnDispose = true;
            www.disposeUploadHandlerOnDispose = true;
            www.Dispose();
        }

        // Retry limit reached, handle failure
        Debug.LogError("Failed to get response after multiple attempts.");
        // You can handle failure here, such as showing an error message to the user
    }

    IEnumerator GetEventFromAPI(string modelName, string apiKey, string prevMsg) 
    {
        string url = "https://api.openai.com/v1/chat/completions";

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", "Bearer " + apiKey);
        headers.Add("Content-Type", "application/json");

        // string requestData = "{\"model\": \"" + modelName + "\", \"messages\": [{\"role\": \"user\", \"content\": \"" + userInput + "\"}]}";
        string transcript = GetEventsString();
        string requestData = "{\"model\": \"" + modelName + "\", \"messages\":" + transcript + "}";
        messagesForEvent.RemoveAt(messagesForEvent.Count - 1);

        UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        foreach (KeyValuePair<string, string> header in headers) {
            www.SetRequestHeader(header.Key, header.Value);
        }

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Error: " + www.error);
        } else {
            ChatCompletion chatCompletion = JsonUtility.FromJson<ChatCompletion>(www.downloadHandler.text);
            string messageContent = chatCompletion.choices[0].message.content;
            if(messageContent != "No event" && !messageContent.Contains("No event"))
            {
                StoreEvents(messageContent);
            }
        }

        www.disposeDownloadHandlerOnDispose = true;
        www.disposeUploadHandlerOnDispose = true;
        www.Dispose();
    }

    // IEnumerator SynthesizeSpeech(string inputText, string modelName, string voiceName, string apiKey)
    // {
    //     string url = "https://api.openai.com/v1/audio/speech";

    //     Dictionary<string, string> headers = new Dictionary<string, string>();
    //     headers.Add("Authorization", "Bearer " + apiKey);
    //     headers.Add("Content-Type", "application/json");

    //     string requestData = "{\"model\": \"" + modelName + "\", \"input\":\"" + inputText + "\", \"voice\":\"" + voiceName + "\"}";

    //     using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
    //     {
    //         www.method = "POST";
    //         byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);
    //         www.uploadHandler = new UploadHandlerRaw(bodyRaw);

    //         foreach (KeyValuePair<string, string> header in headers)
    //         {
    //             www.SetRequestHeader(header.Key, header.Value);
    //         }

    //         yield return www.SendWebRequest();

    //         if (www.result != UnityWebRequest.Result.Success)
    //         {
    //             Debug.LogError("Error: " + www.error);
    //         }
    //         else
    //         {
    //             AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
    //             audioSource.clip = clip;
    //             isAudioReady = true;
    //             // audioSource.Play();
    //         }
    //     }
    // }

    IEnumerator SynthesizeSpeech(string inputText, string modelName, string voiceName, string apiKey)
    {
        string url = "https://api.openai.com/v1/audio/speech";

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", "Bearer " + apiKey);
        headers.Add("Content-Type", "application/json");

        string requestData = "{\"model\": \"" + modelName + "\", \"input\":\"" + inputText + "\", \"voice\":\"" + voiceName + "\"}";

        int retryLimit = 2;
        int retryCount = 0;

        while (retryCount < retryLimit) {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                www.method = "POST";
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);

                foreach (KeyValuePair<string, string> header in headers)
                {
                    www.SetRequestHeader(header.Key, header.Value);
                }

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error: " + www.error);
                    retryCount++;
                    Debug.Log("Retry attempt: " + retryCount);
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    isAudioReady = true;
                    yield break; // Successful response, exit the loop
                }
            }
        }

        // Retry limit reached, handle failure
        Debug.LogError("Failed to synthesize speech after multiple attempts.");
        // You can handle failure here, such as showing an error message to the user
    }



    IEnumerator GetSummary(string modelName, string apiKey, string prevMsg) 
    {
        string url = "https://api.openai.com/v1/chat/completions";

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("Authorization", "Bearer " + apiKey);
        headers.Add("Content-Type", "application/json");
        string requestData = "{\"model\": \"" + modelName + "\", \"messages\": [{\"role\": \"user\", \"content\": \"" + prevMsg + "\"}]}";
        Debug.Log(requestData);
        UnityWebRequest www = UnityWebRequest.PostWwwForm(url, "");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData);
        www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        foreach (KeyValuePair<string, string> header in headers) {
            www.SetRequestHeader(header.Key, header.Value);
        }

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.LogError("Error: " + www.error);
        } else {
            ChatCompletion chatCompletion = JsonUtility.FromJson<ChatCompletion>(www.downloadHandler.text);
            string messageContent = chatCompletion.choices[0].message.content;
            Debug.Log(messageContent);
            // Message msg = Message.CreateFromJSON(www.downloadHandler.text);
            // chat.sendUserMessage(msg.text);
            // client.SendData(msg.text);
        }

        www.disposeDownloadHandlerOnDispose = true;
        www.disposeUploadHandlerOnDispose = true;
        www.Dispose();
    }

    public void Quit()
    {
        // string transcript = GetMessagesString();
        // StartCoroutine(GetSummary("gpt-3.5-turbo","APIKEY HERE","Please summarize this content as short and concise as possible without losing context:" + transcript));
        QuitGame();
    }
}
