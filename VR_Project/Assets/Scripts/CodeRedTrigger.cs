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
                Debug.Log("[CodeRedTrigger] Player entered trigger. Escaping!");
                manager.TriggerEscape();
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[CodeRedTrigger] CodeRedManager not found in scene!");
            }
        }
    }
}
