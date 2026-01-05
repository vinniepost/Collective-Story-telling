using UnityEngine;

public class RatMover : MonoBehaviour
{
    [Header("Instellingen")]
    public Transform targetDestination; // Sleep hier het lege object (Empty) heen
    public float speed = 6f;            // Hoe snel de rat rent
    
    private bool isRunning = false;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Deze functie wordt aangeroepen door je Trigger script
    public void StartRunning()
    {
        if (targetDestination == null) return;
        
        isRunning = true;

        // Start de animatie (zorg dat de naam "Run" klopt met jouw animator)
        if (animator != null)
        {
            animator.Play("Run"); 
        }
    }

    void Update()
    {
        if (isRunning && targetDestination != null)
        {
            // 1. Beweeg de rat richting het doel
            transform.position = Vector3.MoveTowards(transform.position, targetDestination.position, speed * Time.deltaTime);

            // 2. Laat de rat naar het doel kijken
            transform.LookAt(targetDestination);

            // 3. Check of de rat er is
            if (Vector3.Distance(transform.position, targetDestination.position) < 0.1f)
            {
                // Verdwijn!
                gameObject.SetActive(false);
            }
        }
    }
}