using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class CodeRedManager : MonoBehaviour
{
    [Header("Configuration")]
    public float codeRedDuration = 120f;
    public string serverEventId = "CodeRedStart";

    [Header("Visuals")]
    public List<GameObject> normalLights;
    public List<GameObject> emergencyLights;
    
    [Header("Audio")]
    public AudioSource sirenAudioSource;
    public AudioClip sirenClip;
    public AudioSource voiceAudioSource;
    public AudioClip voiceClip;

    [Header("Events")]
    public UnityEvent OnCodeRedStart;
    public UnityEvent OnCodeRedEnd;
    public UnityEvent<float> OnTimerUpdate;

    private bool isCodeRedActive = false;
    private float timeRemaining;

    private void Start()
    {
        // Ensure emergency lights are off initially
        foreach (var light in emergencyLights) light.SetActive(false);
    }

    public void TriggerCodeRed()
    {
        if (isCodeRedActive) return;

        isCodeRedActive = true;
        timeRemaining = codeRedDuration;

        // Visuals
        foreach (var light in normalLights) light.SetActive(false);
        foreach (var light in emergencyLights) light.SetActive(true);

        // Audio
        if (sirenAudioSource != null && sirenClip != null)
        {
            sirenAudioSource.clip = sirenClip;
            sirenAudioSource.loop = true;
            sirenAudioSource.Play();
        }

        if (voiceAudioSource != null && voiceClip != null)
        {
            voiceAudioSource.PlayOneShot(voiceClip);
        }

        // Networking
        if (WebSocketController.Instance != null)
        {
            WebSocketController.Instance.SendGameEvent(serverEventId);
        }
        else
        {
            Debug.LogWarning("WebSocketController Instance is null, cannot send Code Red event.");
        }

        OnCodeRedStart?.Invoke();
    }

    public void TriggerEscape()
    {
        if (!isCodeRedActive) return;
        EndCodeRed(true);
    }

    private void Update()
    {
        if (isCodeRedActive)
        {
            timeRemaining -= Time.deltaTime;
            OnTimerUpdate?.Invoke(timeRemaining);

            if (timeRemaining <= 0)
            {
                EndCodeRed(false);
            }
        }
    }

    public void EndCodeRed(bool playerEscaped)
    {
        isCodeRedActive = false;
        if (sirenAudioSource != null) sirenAudioSource.Stop();
        
        string resultEvent = playerEscaped ? "CodeRedEscape" : "CodeRedFail";
        
        if (WebSocketController.Instance != null)
        {
            WebSocketController.Instance.SendGameEvent(resultEvent);
        }

        Debug.Log(playerEscaped ? "Player Escaped!" : "Code Red Failed - Player Captured");
        OnCodeRedEnd?.Invoke();
    }
    
    // Debug method to test from Inspector
    [ContextMenu("Test Trigger Code Red")]
    public void TestTrigger()
    {
        TriggerCodeRed();
    }
}
