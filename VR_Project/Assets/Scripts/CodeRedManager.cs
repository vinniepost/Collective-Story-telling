using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class CodeRedManager : MonoBehaviour
{
    [Header("Configuration")]
    public float codeRedDuration = 120f;
    public string serverEventId = "CodeRedStart";
    [Tooltip("Default purge duration applied on server when Unity configures it")]
    public int purgeDefaultSeconds = 120;
    [Header("Escape / Scenes")]
    public string startSceneName = "Start";

    [Header("Visuals")]
    // Optional: tint selected point lights red during purge
    [Tooltip("Realtime/Mixed point lights on the map to tint red during purge")]
    public List<Light> mapPointLights;
    public Color purgeLightColor = Color.red;
    
    [Header("Audio")]
    public AudioSource sirenAudioSource;
    public AudioClip sirenClip;
    [Tooltip("AudioSource attached to the VR rig/camera for VO (set Spatial Blend=0 for 2D)")]
    public AudioSource voiceOverSource;
    public AudioClip voiceClip;

    [Header("Events")]
    public UnityEvent OnCodeRedStart;
    public UnityEvent OnCodeRedEnd;
    public UnityEvent<float> OnTimerUpdate;
    public UnityEvent<string> OnHudMessage; // VR HUD text updates (VO line, countdown)

    private bool isCodeRedActive = false;
        // Expose read-only state for other systems (e.g., triggers)
        public bool IsCodeRedActive => isCodeRedActive;
    private float timeRemaining;
    private Dictionary<Light, Color> _originalMapLightColors = new Dictionary<Light, Color>();
    private bool _suppressServerNotifyOnNextTrigger = false;
    private bool _escapedBeforePurge = false;

    private void Start()
    {
        // Configure server purge duration to 10s at startup.
        if (WebSocketController.Instance != null)
        {
            Debug.Log("");
        }
        // Visuals configured by designers. Map light colors cached when purge starts.
    }

    public void TriggerCodeRed()
    {
        if (isCodeRedActive) return;

        isCodeRedActive = true;
        timeRemaining = codeRedDuration;

        // Visuals: tint selected point lights red

        // Recolor map point lights
        CacheOriginalMapLightColors();
        SetMapPointLightsColor(purgeLightColor);

        // Audio
        if (sirenAudioSource != null && sirenClip != null)
        {
            sirenAudioSource.clip = sirenClip;
            sirenAudioSource.loop = true;
            sirenAudioSource.Play();
        }

        // Voice-over handled elsewhere; do not play here.

        // Networking
        if (!_suppressServerNotifyOnNextTrigger)
        {
            if (WebSocketController.Instance != null)
            {
                WebSocketController.Instance.SendGameEvent(serverEventId);
            }
            else
            {
                Debug.LogWarning("WebSocketController Instance is null, cannot send Code Red event.");
            }
        }
        _suppressServerNotifyOnNextTrigger = false;

        OnCodeRedStart?.Invoke();
    }

    // Unity-initiated pre-purge: play VO, tint lights, then start purge (and notify server)
    public void StartPrePurgeSequence()
    {
        // Show VO directive
        OnHudMessage?.Invoke("ESCAPE TO THE START");

        // Tint lights immediately to indicate impending purge
        CacheOriginalMapLightColors();
        SetMapPointLightsColor(purgeLightColor);

        // Play VO locally
        float waitSeconds = 8f;
        if (voiceClip != null && voiceOverSource != null)
        {
            voiceOverSource.PlayOneShot(voiceClip);
            waitSeconds = Mathf.Max(0.1f, voiceClip.length);
            Debug.Log($"[CodeRedManager] Playing VO clip, duration={waitSeconds:F2}s");
        }
        else if (voiceOverSource != null && voiceOverSource.clip != null)
        {
            voiceOverSource.Play();
            waitSeconds = Mathf.Max(0.1f, voiceOverSource.clip.length);
            Debug.Log($"[CodeRedManager] Playing VO source clip, duration={waitSeconds:F2}s");
        }
        else
        {
            Debug.LogWarning("[CodeRedManager] No voice clip/source assigned; using default 8s delay before purge.");
        }

        // After VO delay, start purge locally and notify server
        StartCoroutine(StartPurgeAfterDelay(waitSeconds));
    }

    private IEnumerator StartPurgeAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_escapedBeforePurge)
        {
            EndCodeRed(true);
        }
        else
        {
            EndCodeRed(false);
        }
    }

    // Called by WebSocketController when server sends voice_over
    public void HandleServerVoiceOver(string text, int durationSeconds)
    {
        // Show VO text on VR HUD
        OnHudMessage?.Invoke(text);
        // Play VO locally (VR-only)
        if (voiceOverSource != null)
        {
            if (voiceClip != null)
                voiceOverSource.PlayOneShot(voiceClip);
            else if (voiceOverSource.clip != null)
                voiceOverSource.Play();
        }
        // After VO finishes, start Code Red locally
        StartCoroutine(StartCodeRedAfterVoice(durationSeconds));
    }

    // Called by WebSocketController when server sends code_red (fallback)
    public void HandleServerCodeRed(int durationSeconds)
    {
        codeRedDuration = durationSeconds;
        if (isCodeRedActive)
        {
            timeRemaining = durationSeconds;
        }
        else
        {
            _suppressServerNotifyOnNextTrigger = true; // prevent echo-loop
            TriggerCodeRed();
        }
    }

    private IEnumerator StartCodeRedAfterVoice(int seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0, seconds));
        TriggerCodeRed();
    }

    // (Removed no-voice variant; purge starts via TriggerCodeRed)

    public void TriggerEscape()
    {
        if (isCodeRedActive)
        {
            EndCodeRed(true);
        }
        else
        {
            // Mark escaped before purge
            _escapedBeforePurge = true;
            OnHudMessage?.Invoke("ESCAPED!");
        }
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
        // Restore map point lights to original colors
        RestoreMapPointLightsColors();
        
        string resultEvent = playerEscaped ? "CodeRedEscape" : "CodeRedFail";
        
        if (WebSocketController.Instance != null)
        {
            WebSocketController.Instance.SendGameEvent(resultEvent);
        }

        Debug.Log(playerEscaped ? "Player Escaped!" : "Code Red Failed - Player Captured");
        OnCodeRedEnd?.Invoke();
        OnHudMessage?.Invoke(playerEscaped ? "YOU ESCAPED" : "GAME OVER");
    }
    
    // Debug method to test from Inspector
    [ContextMenu("Test Trigger Code Red")]
    public void TestTrigger()
    {
        TriggerCodeRed();
    }

    private void LoadStartScene()
    {
        if (!string.IsNullOrEmpty(startSceneName))
        {
            SceneManager.LoadScene(startSceneName);
        }
    }

    // Configure server's default purge duration from Unity
    [ContextMenu("Apply Purge Duration To Server")]
    public void ApplyPurgeDurationToServer()
    {
        if (WebSocketController.Instance != null)
        {
            WebSocketController.Instance.SendCodeRedConfig(purgeDefaultSeconds);
            Debug.Log($"[CodeRedManager] Sent server purge duration config: {purgeDefaultSeconds}s");
        }
        else
        {
            Debug.LogWarning("[CodeRedManager] WebSocketController.Instance is null; cannot configure server purge duration.");
        }
    }

    // Start purge via server using explicit duration
    public void StartServerPurgeWithDuration(int seconds)
    {
        if (WebSocketController.Instance != null)
        {
            WebSocketController.Instance.SendCodeRedStartWithDuration(seconds);
            Debug.Log($"[CodeRedManager] Requested server Code Red start: {seconds}s");
        }
        else
        {
            Debug.LogWarning("[CodeRedManager] WebSocketController.Instance is null; cannot start server purge.");
        }
    }

    private void CacheOriginalMapLightColors()
    {
        _originalMapLightColors.Clear();
        if (mapPointLights == null) return;
        foreach (var l in mapPointLights)
        {
            if (l == null) continue;
            if (!_originalMapLightColors.ContainsKey(l))
                _originalMapLightColors[l] = l.color;
        }
    }

    private void SetMapPointLightsColor(Color c)
    {
        if (mapPointLights == null) return;
        foreach (var l in mapPointLights)
        {
            if (l == null) continue;
            l.color = c;
        }
    }

    private void RestoreMapPointLightsColors()
    {
        foreach (var kvp in _originalMapLightColors)
        {
            var l = kvp.Key;
            if (l == null) continue;
            l.color = kvp.Value;
        }
        _originalMapLightColors.Clear();
    }
}
