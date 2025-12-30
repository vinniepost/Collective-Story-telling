using UnityEngine;

public class CodeRedTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object is the player. 
        // Adjust the tag check based on your actual Player object tag (usually "Player").
        if (other.CompareTag("Player") || other.name.Contains("Player") || other.name.Contains("XR Origin"))
        {
            // Find the manager and trigger escape
            // Assuming CodeRedManager is a singleton-like or easily findable
            var manager = FindFirstObjectByType<CodeRedManager>();
            if (manager != null)
            {
                Debug.Log($"[CodeRedTrigger] Player entered trigger. Escaping!");
                manager.TriggerEscape();
                
                // Disable this trigger so it doesn't fire multiple times
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[CodeRedTrigger] CodeRedManager not found in scene!");
            }
        }
    }
}
