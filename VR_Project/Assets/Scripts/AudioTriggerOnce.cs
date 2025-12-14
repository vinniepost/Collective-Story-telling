using UnityEngine;

public class AudioTriggerOnce : MonoBehaviour
{
    // Publieke variabele: Sleep hier de AudioSource van het geluidsobject in (bijv. een luidspreker)
    public AudioSource targetAudioSource; 

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        // Controleer of het de speler is EN of het geluid nog niet is afgespeeld
        if (other.CompareTag("Player") && !hasPlayed)
        {
            // Speel het geluid af, als de referentie is ingesteld
            if (targetAudioSource != null)
            {
                targetAudioSource.Play();
            }
            else
            {
                Debug.LogError("Target Audio Source is niet ingesteld in de Inspector!");
            }
            
            // Markeer als afgespeeld
            hasPlayed = true;
        }
    }
}