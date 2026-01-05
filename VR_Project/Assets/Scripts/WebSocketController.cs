using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// This class handles the WebSocket connection between the Unity VR client and the server.
/// It sends the player's position to the server and receives voting updates to change the environment (lights/weather).
/// </summary>
public class WebSocketController : MonoBehaviour
{
    public static WebSocketController Instance;

    [Header("Connection")]
    public string serverUrl = "ws://139.59.215.136/ws";

    [Header("Server Actions")]
    public UnityEvent OnDoorOpen;
    public UnityEvent OnDoorClose;
    public UnityEvent OnLightOn;
    public UnityEvent OnLightOff;
    public UnityEvent OnSound1;
    public UnityEvent OnSound2;

    private bool isLightOn = false;
    private bool isDoorClosed = false;

    [Header("Map Settings")]
    public Transform playerTransform;
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    [Header("Game Events")]
    public UnityEvent<string> OnAudienceMessage; // When "vr_message_sent" is received
    public UnityEvent<VoteUpdateData> OnVoteUpdate; // When "update" is received
    [Header("Code Red Integration")]
    public CodeRedManager codeRedManager;        // Forward server events to CodeRedManager

    private WebSocket websocket;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    async void Start()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += () => Debug.Log("[WebSocket] Connection open!");
        websocket.OnError += (e) => Debug.LogError("[WebSocket] Error: " + e);
        websocket.OnClose += (e) => Debug.Log("[WebSocket] Connection closed!");

        websocket.OnMessage += (bytes) =>
        {
            // Dispatch to main thread is handled in Update, but we can parse here or there.
            // NativeWebSocket callbacks run on the main thread if DispatchMessageQueue is called.
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("[WebSocket] Received: " + message);
            HandleMessage(message);
        };

        StartCoroutine(SendPlayerLocationRoutine());
        await websocket.Connect();
    }

    IEnumerator SendPlayerLocationRoutine()
    {
        Debug.Log("[WebSocket] Routine Started");
        while (true)
        {
            if (websocket != null && websocket.State == WebSocketState.Open && playerTransform != null)
            {
                float x = Mathf.InverseLerp(minX, maxX, playerTransform.position.x) * 100f;
                float y = Mathf.InverseLerp(minZ, maxZ, playerTransform.position.z) * 100f;
                
                // Clamp to 0-100
                x = Mathf.Clamp(x, 0f, 100f);
                y = Mathf.Clamp(y, 0f, 100f);

                // Invert Y because Unity Z increases upwards (North) but HTML Y increases downwards
                y = 100f - y; 

                // Debug logs to verify coordinates
                Debug.Log($"[WebSocket] Player Pos: {playerTransform.position}, Map Coords: {x}%, {y}%");

                var data = new PlayerLocationData 
                { 
                    type = "player_location", 
                    location = new Location { x = x, y = y } 
                };
                
                string json = JsonUtility.ToJson(data);
                Debug.Log($"[WebSocket] Sending: {json} | Player: {playerTransform.position} | Bounds X:{minX}~{maxX} Z:{minZ}~{maxZ}");
                websocket.SendText(json);
            }
            else
            {
                if (websocket == null) Debug.LogWarning("[WebSocket] WebSocket object is null");
                else if (websocket.State != WebSocketState.Open) Debug.LogWarning($"[WebSocket] Connection not open. State: {websocket.State}");
                
                if (playerTransform == null) Debug.LogError("[WebSocket] Player Transform is NOT assigned in Inspector!");
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Draw a wire cube representing the map boundaries
        Vector3 center = new Vector3((minX + maxX) / 2, playerTransform != null ? playerTransform.position.y : 0, (minZ + maxZ) / 2);
        Vector3 size = new Vector3(Mathf.Abs(maxX - minX), 10f, Mathf.Abs(maxZ - minZ));
        Gizmos.DrawWireCube(center, size);
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    void Update()
    {
        // DispatchMessageQueue is required to process WebSocket events on the main Unity thread.
        // This ensures that callbacks like OnMessage run safely within Unity's lifecycle.
        websocket?.DispatchMessageQueue();
    }
#endif

    private void HandleMessage(string json)
    {
        try
        {
            // Parse just the type first
            ServerMessage baseMsg = JsonUtility.FromJson<ServerMessage>(json);

            switch (baseMsg.type)
            {
                case "update":
                    var updateData = JsonUtility.FromJson<VoteUpdateMessage>(json);
                    var data = new VoteUpdateData 
                    { 
                        votes = updateData.votes, 
                        totalClients = updateData.totalClients, 
                        playerInArea = updateData.playerInArea 
                    };
                    OnVoteUpdate?.Invoke(data);
                    break;

                case "action":
                    var actionMsg = JsonUtility.FromJson<ActionMessage>(json);
                    HandleAction(actionMsg.command);
                    break;

                case "vr_message_sent":
                    var vrMsg = JsonUtility.FromJson<VRMessageSent>(json);
                    Debug.Log("VR Message from Audience: " + vrMsg.message);
                    OnAudienceMessage?.Invoke(vrMsg.message);
                    break;
                case "code_red":
                    var codeRedMsg = JsonUtility.FromJson<CodeRedMessage>(json);
                    Debug.Log($"Server code_red: duration={codeRedMsg.duration}s");
                    EnsureCodeRedManager();
                    codeRedManager?.HandleServerCodeRed(codeRedMsg.duration);
                    break;
                case "voice_over":
                    var voMsg = JsonUtility.FromJson<VoiceOverMessage>(json);
                    Debug.Log($"Server voice_over: '{voMsg.text}' ({voMsg.duration}s)");
                    EnsureCodeRedManager();
                    codeRedManager?.HandleServerVoiceOver(voMsg.text, voMsg.duration);
                    break;
                
                case "assign_username":
                    Debug.Log("Assigned username: " + JsonUtility.FromJson<AssignUsernameMessage>(json).username);
                    break;

                case "door_closed":
                    var closeMsg = JsonUtility.FromJson<DoorEventMessage>(json);
                    MapControlManager.Instance.OnDoorClosed(closeMsg.doorId);
                    break;

                case "door_unlockable":
                    var unlockMsg = JsonUtility.FromJson<DoorEventMessage>(json);
                    MapControlManager.Instance.OnDoorUnlockable(unlockMsg.doorId);
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing message: " + e.Message);
        }
    }

    private void EnsureCodeRedManager()
    {
        if (codeRedManager == null)
        {
            codeRedManager = FindObjectOfType<CodeRedManager>();
            if (codeRedManager == null)
            {
                Debug.LogWarning("WebSocketController: CodeRedManager not found in scene.");
            }
        }
    }

    private void HandleAction(string command)
    {
        switch (command)
        {
            case "close_door":
                if (isDoorClosed)
                {
                    OnDoorOpen?.Invoke();
                    isDoorClosed = false;
                }
                else
                {
                    OnDoorClose?.Invoke();
                    isDoorClosed = true;
                }
                break;
            case "light":
                if (isLightOn)
                {
                    OnLightOff?.Invoke();
                    isLightOn = false;
                }
                else
                {
                    OnLightOn?.Invoke();
                    isLightOn = true;
                }
                break;
            case "sound_1": OnSound1?.Invoke(); break;
            case "sound_2": OnSound2?.Invoke(); break;
            default: Debug.LogWarning("Unknown action command: " + command); break;
        }
    }

    // Public methods for other scripts to call

    public async void SendGameEvent(string eventId)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var data = new GameEventData { type = "game_event", event_id = eventId };
            await websocket.SendText(JsonUtility.ToJson(data));
        }
    }

    // Send Code Red start with explicit duration (Unity-controlled)
    public async void SendCodeRedStartWithDuration(int seconds)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var data = new GameEventData { type = "game_event", event_id = "CodeRedStart", duration = seconds };
            await websocket.SendText(JsonUtility.ToJson(data));
        }
    }

    // Configure server default purge duration
    public async void SendCodeRedConfig(int seconds)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var data = new GameEventData { type = "game_event", event_id = "CodeRedConfig", duration = seconds };
            await websocket.SendText(JsonUtility.ToJson(data));
        }
    }

    public async void SendVRMessage(string message)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var data = new VRMessageData { type = "vr_message", message = message };
            await websocket.SendText(JsonUtility.ToJson(data));
        }
    }

    public async void SendDoorOpened(string doorId)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var data = new DoorEventData { type = "door_opened", doorId = doorId };
            await websocket.SendText(JsonUtility.ToJson(data));
        }
    }

    [System.Serializable]
    public class DoorEventData
    {
        public string type;
        public string doorId;
    }


    public async void StartMessageVote(string[] options)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var data = new StartVoteData { type = "start_message_vote", options = options };
            await websocket.SendText(JsonUtility.ToJson(data));
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null) await websocket.Close();
    }
}

// Data Structures

[System.Serializable]
public class ServerMessage
{
    public string type;
}

[System.Serializable]
public class VoteUpdateMessage : ServerMessage
{
    public Votes votes;
    public int totalClients;
    public bool playerInArea;
}

[System.Serializable]
public class Votes
{
    public int close_door;
    public int light;
    public int sound_1;
    public int sound_2;
}

[System.Serializable]
public class VoteUpdateData
{
    public Votes votes;
    public int totalClients;
    public bool playerInArea;
}

[System.Serializable]
public class ActionMessage : ServerMessage
{
    public string command;
}

[System.Serializable]
public class VRMessageSent : ServerMessage
{
    public string message;
}

[System.Serializable]
public class CodeRedMessage : ServerMessage
{
    public int duration;
}

[System.Serializable]
public class VoiceOverMessage : ServerMessage
{
    public string text;
    public int duration;
}

[System.Serializable]
public class AssignUsernameMessage : ServerMessage
{
    public string username;
}

[System.Serializable]
public class DoorEventMessage : ServerMessage
{
    public string doorId;
}

[System.Serializable]
public class GameEventData
{
    public string type;
    public string event_id;
    public int duration;
}

[System.Serializable]
public class VRMessageData
{
    public string type;
    public string message;
}

[System.Serializable]
public class StartVoteData
{
    public string type;
    public string[] options;
}

// Data structure for generic game events
[System.Serializable]
public class TriggerEvent
{
    public string type = "game_event";
    public string event_id;            
}

[System.Serializable]
public class PlayerLocationData
{
    public string type;
    public Location location;
}

[System.Serializable]
public class Location
{
    public float x;
    public float y;
}