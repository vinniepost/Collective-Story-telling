using UnityEngine;

public class PlayAudioOnTrigger : MonoBehaviour
{
    private AudioSource audioSource;
    private bool hasPlayed = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasPlayed)
        {
            audioSource.Play();
            hasPlayed = true; 
        }
    }
}
