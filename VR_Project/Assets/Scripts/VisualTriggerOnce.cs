using UnityEngine;

public class VisualTriggerOnce : MonoBehaviour
{
    // Dit GameObject wordt zichtbaar wanneer de trigger geactiveerd wordt
    public GameObject shadowObject; 
    
    private bool hasActivated = false;

    void Start()
    {
        // Zorg ervoor dat het object bij de start onzichtbaar is
        if (shadowObject != null)
        {
            shadowObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Controleer of het de speler is EN of de schaduw nog niet is verschenen
        if (other.CompareTag("Player") && !hasActivated)
        {
            // Laat de schaduw verschijnen
            if (shadowObject != null)
            {
                shadowObject.SetActive(true);
            }
            
            // Markeer als geactiveerd
            hasActivated = true;
            
            // Optioneel: Vernietig dit script
            // Destroy(this);
        }
    }
}