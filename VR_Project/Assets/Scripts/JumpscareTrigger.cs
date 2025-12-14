using UnityEngine;
using UnityEngine.Events;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("Settings")]
    public bool triggerOnce = true; // Should it happen only once?
    private bool hasTriggered = false;

    [Header("Actions")]
    public UnityEvent onScareTriggered; // Drag and drop functionality here

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering is the Player (ensure your VR player has the tag "Player")
        if (other.CompareTag("Player") && !hasTriggered)
        {
            onScareTriggered.Invoke();

            if (triggerOnce)
            {
                hasTriggered = true;
                // Optional: Destroy the trigger so it never happens again
                // Destroy(gameObject); 
            }
        }
    }
}