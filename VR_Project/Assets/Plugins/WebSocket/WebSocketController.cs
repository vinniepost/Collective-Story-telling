using UnityEngine;
using NativeWebSocket;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// This class handles the WebSocket connection between the Unity VR client and the server.
/// It sends the player's position to the server and receives voting updates to change the environment (lights/weather).
/// </summary>
public class WebSocketController : MonoBehaviour
{
    [Header("VR & Lights")]
    public Transform vrPlayer;
    public Light sunLight;
    public Light rainLight;
    public Light light1;
    public Light light2;

    // The WebSocket instance used to communicate with the server
    private WebSocket websocket;

    async void Start()
    {
        if (vrPlayer == null) Debug.LogError("vrPlayer not assigned!");

        // Initialize the WebSocket with the server URL.
        // "wss://" indicates a secure WebSocket connection (like https).
        websocket = new WebSocket("ws://localhost:3000");

        // Subscribe to WebSocket events
        websocket.OnOpen += () => Debug.Log("[WebSocket] OPEN"); // Called when connection is established
        websocket.OnError += (e) => Debug.LogError("[WebSocket] ERROR: " + e); // Called when an error occurs
        websocket.OnClose += (e) => Debug.LogWarning("[WebSocket] CLOSED"); // Called when connection is closed

        // Called when a message is received from the server
        websocket.OnMessage += (bytes) =>
        {
            // Convert the received bytes to a string
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("[WebSocket] RECEIVED: " + msg);

            try
            {
                // Parse the JSON message into a VoteUpdate object
                var voteData = JsonUtility.FromJson<VoteUpdate>(msg);
                
                // Check the message type and handle accordingly
                if (voteData.type == "update")
                {
                    HandleVoteUpdate(voteData);
                }
                else if (voteData.type == "round_start")
                {
                    Debug.Log("New round started: " + voteData.round.name);
                }
            }
            catch { /* ignore non-vote messages or JSON parsing errors */ }
        };

        try
        {
            // Attempt to connect to the server asynchronously
            await websocket.Connect();
            Debug.Log("WebSocket connected. Starting position updates...");
            
            // Start sending position updates periodically
            StartCoroutine(PositionUpdateCoroutine());
        }
        catch (System.Exception e)
        {
            Debug.LogError("WebSocket failed to connect: " + e);
        }
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    void Update()
    {
        // DispatchMessageQueue is required to process WebSocket events on the main Unity thread.
        // This ensures that callbacks like OnMessage run safely within Unity's lifecycle.
        websocket?.DispatchMessageQueue();
    }
#endif

    // Coroutine to send player position updates at a fixed interval
    private IEnumerator PositionUpdateCoroutine()
    {
        while (this != null && websocket != null && websocket.State == WebSocketState.Open)
        {
            if (vrPlayer == null) yield break;

            SendPosition();
            // Wait for 0.05 seconds (20 times per second)
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Sends the current VR player position to the server
    private async void SendPosition()
    {
        if (websocket == null || websocket.State != WebSocketState.Open) return;

        // Create a data object with the position
        var pos = new PlayerPosition
        {
            type = "player_position",
            x = vrPlayer.position.x,
            y = vrPlayer.position.y,
            z = vrPlayer.position.z
        };

        // Convert the object to a JSON string
        string json = JsonUtility.ToJson(pos);
        
        // Send the JSON string to the server
        await websocket.SendText(json);
    }

    // Updates the scene (lights/weather) based on voting data received from the server
    private void HandleVoteUpdate(VoteUpdate data)
    {
        // Check which round is currently active
        if (data.round.name == "weather")
        {
            // Compare votes for rain vs sunny
            if (data.votes.rain > data.votes.sunny)
            {
                if (rainLight != null) rainLight.enabled = true;
                if (sunLight != null) sunLight.enabled = false;
            }
            else
            {
                if (rainLight != null) rainLight.enabled = false;
                if (sunLight != null) sunLight.enabled = true;
            }
        }
        else if (data.round.name == "lights")
        {
            // Compare votes for light1 vs light2
            if (data.votes.light1 > data.votes.light2)
            {
                if (light1 != null) light1.enabled = true;
                if (light2 != null) light2.enabled = false;
            }
            else
            {
                if (light1 != null) light1.enabled = false;
                if (light2 != null) light2.enabled = true;
            }
        }
    }

    // Helper method to send a generic game event to the server
    public async void SendGameEvent(string eventJson)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket not open. Cannot send event.");
            return;
        }

        try
        {
            await websocket.SendText(eventJson);
            Debug.Log("[WebSocket] SENT EVENT: " + eventJson);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to send event: " + e.Message);
        }
    }

    // Close the WebSocket connection when the application quits
    async void OnApplicationQuit()
    {
        if (websocket != null)
            await websocket.Close();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}

// ---------------------
// JSON Models
// These classes match the structure of the JSON data sent to/from the server.
// ---------------------

// Data structure for sending player position
[System.Serializable]
public class PlayerPosition
{
    public string type;
    public float x, y, z;
}

// Data structure for receiving vote updates
[System.Serializable]
public class VoteUpdate
{
    public string type;
    public Votes votes;
    public RoundInfo round;
}

// Nested class for vote counts
[System.Serializable]
public class Votes
{
    public int sunny;
    public int rain;
    public int light1;
    public int light2;
}

// Nested class for round information
[System.Serializable]
public class RoundInfo
{
    public string name;
    public string[] options;
}

// Data structure for generic game events
[System.Serializable]
public class TriggerEvent
{
    public string type = "game_event";
    public string event_id;            
}