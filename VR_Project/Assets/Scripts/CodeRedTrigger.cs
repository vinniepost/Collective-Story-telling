using UnityEngine;

public class CodeRedTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Player") || other.name.Contains("XR Origin"))
        {
            var manager = FindFirstObjectByType<CodeRedManager>();
            if (manager != null)
            {
                if (manager.IsCodeRedActive)
                {
                    Debug.Log("[CodeRedTrigger] Purge active: ESCAPE triggered.");
                    manager.TriggerEscape();
                    gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log("[CodeRedTrigger] Ignored: purge not active.");
                }
            }
            else
            {
                Debug.LogWarning("[CodeRedTrigger] CodeRedManager not found in scene!");
            }
        }
    }
}
