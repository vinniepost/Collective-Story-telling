using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;

public class WebSocketController : MonoBehaviour
{
    WebSocket websocket;
    [SerializeField] GameObject sunny;
    [SerializeField] GameObject rainy;

    async void Start()
    {
        // Assign automatically if not set in Inspector
        if (sunny == null) sunny = GameObject.Find("sunny");
        if (rainy == null) rainy = GameObject.Find("rainy");

        if (sunny == null || rainy == null)
        {
            Debug.LogError("Couldn't find 'sunny' or 'rainy' GameObjects!");
            return;
        }

        sunny.SetActive(false);
        rainy.SetActive(false);

        websocket = new WebSocket("ws://localhost:8080");

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + message);

            var data = JsonUtility.FromJson<VoteUpdate>(message);
            if (data.type == "update")
            {
                if (data.votes.rain > data.votes.sunny)
                    SetWeather("rainy");
                else
                    SetWeather("sunny");
            }
        };

        await websocket.Connect();
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    void Update()
    {
        websocket.DispatchMessageQueue();
    }
#endif

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    void SetWeather(string weather)
    {
        if (weather == "rainy")
        {
            rainy.SetActive(true);
            sunny.SetActive(false);
        }
        else if (weather == "sunny")
        {
            rainy.SetActive(false);
            sunny.SetActive(true);
        }

        Debug.Log("Setting weather to " + weather);
    }
}

[System.Serializable]
public class VoteUpdate
{
    public string type;
    public Votes votes;
}

[System.Serializable]
public class Votes
{
    public int sunny;
    public int rain;
}
