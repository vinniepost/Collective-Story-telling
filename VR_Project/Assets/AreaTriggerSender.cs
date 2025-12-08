using UnityEngine;

public class AreaTriggerSender : MonoBehaviour
{
    public WebSocketController controller;

    public string eventId = "Checkpoint_Default";

    private bool playerIsInside = false; 

    private void Start()
    {
        if (GetComponent<Collider>() == null || !GetComponent<Collider>().isTrigger)
        {
            Debug.LogError($"Collider on '{gameObject.name}' must be set to Is Trigger!");
        }
        if (controller == null)
        {
            Debug.LogError("WebSocketController reference is missing on the trigger zone!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerIsInside)
        {
            playerIsInside = true;
            Debug.Log($"Player entered area: {eventId}. Sending event to server.");

            var triggerEvent = new TriggerEvent
            {
                event_id = eventId
            };

            string json = JsonUtility.ToJson(triggerEvent);

            if (controller != null)
            {
                controller.SendGameEvent(json);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInside = false;
            Debug.Log($"Player exited area: {eventId}");
        }
    }
}