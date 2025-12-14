using System;
using UnityEngine;

public class DelayedMovementTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    // OBJECTEN DIE JE IN DE INSPECTOR SLEEPT:
    public GameObject objectToMove;
    public Transform startWaypoint;
    public Transform endWaypoint;
    public AudioSource scareAudioSource; // Referentie naar de AudioSource component

    // PARAMETERS:
    public float movementDuration = 0.3f;    // Hoe snel de charge is
    public float audioDelaySeconds = 2.0f; // NIEUW: De vertraging tussen geluid en beweging

    // VARIABELEN VOOR DE BEWEGING
    private float startTime;
    private bool isMoving = false;

    void Start()
    {
        // Zorg ervoor dat het bewegende object op de startpositie staat en UIT is
        if (objectToMove != null && startWaypoint != null)
        {
            objectToMove.transform.position = startWaypoint.position;
            objectToMove.SetActive(false); 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Controleer of het de speler is en eenmalig mag triggeren
        if (other.CompareTag("Player") && !hasTriggered)
        {
            Debug.Log("Trigger geraakt door: " + other.gameObject.name);
            hasTriggered = true; 
            

            // 1. Speel het geluid af
            if (scareAudioSource != null)
            {
                scareAudioSource.Play();
            }

            // 2. Roep de bewegingsfunctie aan na de ingestelde vertraging
            // De Invoke-functie start de methode "StartMovement" na "audioDelaySeconds"
            Invoke("StartMovement", audioDelaySeconds);
        }
    }

    // NIEUWE FUNCTIE: Start de feitelijke beweging
    void StartMovement()
    {
        if (objectToMove != null)
        {
            objectToMove.SetActive(true); // Schakel het monster in
            isMoving = true;
            startTime = Time.time;
        }
    }

    void Update()
    {
        if (isMoving && objectToMove != null)
        {
            if (startWaypoint == null || endWaypoint == null) return;
            
            float t = (Time.time - startTime) / movementDuration;
            t = Mathf.Clamp01(t); 

            // Beweging
            objectToMove.transform.position = Vector3.Lerp(startWaypoint.position, endWaypoint.position, t);

            // Rotatie (houdt X/Z vast zoals eerder besproken)
            Vector3 targetDirection = endWaypoint.position - objectToMove.transform.position;
            targetDirection.y = 0; 
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            
            objectToMove.transform.rotation = Quaternion.Euler(
                0,                             // Vries X-rotatie
                targetRotation.eulerAngles.y,  // Gebruik Y-rotatie
                0                              // Vries Z-rotatie
            );
            
            if (t >= 1.0f)
            {
                // De beweging is voltooid
                isMoving = false; 
                objectToMove.SetActive(false); 
            }
        }
    }
}