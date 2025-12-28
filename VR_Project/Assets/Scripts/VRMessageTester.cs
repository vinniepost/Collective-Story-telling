using UnityEngine;
using UnityEngine.InputSystem;

public class VRMessageTester : MonoBehaviour
{
    public WebSocketController controller;

    [Header("Test Data")]
    public string testMessage = "Hello from VR!";
    public string[] testOptions = new string[] { "Option A", "Option B", "Option C" };

    private void Start()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<WebSocketController>();
        }
    }

    private void Update()
    {
        // Optional: Keyboard shortcuts for quick testing
        if (Keyboard.current != null)
        {
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                SendTestMessage();
            }
            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                StartTestVote();
            }
        }
    }

    [ContextMenu("Send Test Message")]
    public void SendTestMessage()
    {
        if (controller != null)
        {
            Debug.Log($"Sending VR Message: {testMessage}");
            controller.SendVRMessage(testMessage);
        }
        else
        {
            Debug.LogError("WebSocketController not assigned!");
        }
    }

    [ContextMenu("Start Test Vote")]
    public void StartTestVote()
    {
        if (controller != null)
        {
            Debug.Log("Starting Message Vote...");
            controller.StartMessageVote(testOptions);
        }
        else
        {
            Debug.LogError("WebSocketController not assigned!");
        }
    }
}
