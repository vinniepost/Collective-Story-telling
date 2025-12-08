using UnityEngine;
using NativeWebSocket;
using System.Collections;
using System.Threading.Tasks;

public class WebSocketController : MonoBehaviour
{
    [Header("VR & Lights")]
    public Transform vrPlayer;
    public Light sunLight;
    public Light rainLight;
    public Light light1;
    public Light light2;

    private WebSocket websocket;

    async void Start()
    {
        if (vrPlayer == null) Debug.LogError("vrPlayer not assigned!");

        websocket = new WebSocket("wss://websocketmixedreality.onrender.com/");

        websocket.OnOpen += () => Debug.Log("[WebSocket] OPEN");
        websocket.OnError += (e) => Debug.LogError("[WebSocket] ERROR: " + e);
        websocket.OnClose += (e) => Debug.LogWarning("[WebSocket] CLOSED");

        websocket.OnMessage += (bytes) =>
        {
            string msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("[WebSocket] RECEIVED: " + msg);

            try
            {
                var voteData = JsonUtility.FromJson<VoteUpdate>(msg);
                if (voteData.type == "update")
                {
                    HandleVoteUpdate(voteData);
                }
                else if (voteData.type == "round_start")
                {
                    Debug.Log("New round started: " + voteData.round.name);
                }
            }
            catch { /* ignore non-vote messages */ }
        };

        try
        {
            await websocket.Connect();
            Debug.Log("WebSocket connected. Starting position updates...");
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
        websocket?.DispatchMessageQueue();
    }
#endif

    private IEnumerator PositionUpdateCoroutine()
    {
        while (this != null && websocket != null && websocket.State == WebSocketState.Open)
        {
            if (vrPlayer == null) yield break;

            SendPosition();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private async void SendPosition()
    {
        if (websocket == null || websocket.State != WebSocketState.Open) return;

        var pos = new PlayerPosition
        {
            type = "player_position",
            x = vrPlayer.position.x,
            y = vrPlayer.position.y,
            z = vrPlayer.position.z
        };

        string json = JsonUtility.ToJson(pos);
        await websocket.SendText(json);
    }

    private void HandleVoteUpdate(VoteUpdate data)
    {
        if (data.round.name == "weather")
        {
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
// ---------------------
[System.Serializable]
public class PlayerPosition
{
    public string type;
    public float x, y, z;
}

[System.Serializable]
public class VoteUpdate
{
    public string type;
    public Votes votes;
    public RoundInfo round;
}

[System.Serializable]
public class Votes
{
    public int sunny;
    public int rain;
    public int light1;
    public int light2;
}

[System.Serializable]
public class RoundInfo
{
    public string name;
    public string[] options;
}

[System.Serializable]
public class TriggerEvent
{
    public string type = "game_event";
    public string event_id;            
}