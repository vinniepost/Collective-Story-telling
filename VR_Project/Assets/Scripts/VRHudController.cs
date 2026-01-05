using UnityEngine;
using TMPro;

public class VRHudController : MonoBehaviour
{
    [Header("HUD")]
    public TextMeshProUGUI messageLabel;
    [Tooltip("Optional voice-over audio source to play when server triggers voice_over.")]
    public AudioSource voiceOverSource;
    public AudioClip defaultVoiceClip;
    [Header("Code Red Source")]
    public CodeRedManager codeRedManager;

    void Start()
    {
        // Subscribe to local CodeRed events for HUD and countdown
        if (codeRedManager == null)
        {
            codeRedManager = FindObjectOfType<CodeRedManager>();
        }
        if (codeRedManager != null)
        {
            codeRedManager.OnHudMessage.AddListener(OnVrMessage);
            codeRedManager.OnTimerUpdate.AddListener(OnLocalTimerUpdate);
            codeRedManager.OnCodeRedEnd.AddListener(ClearHudOnEnd);
        }
    }

    private void OnVrMessage(string text)
    {
        if (messageLabel == null) return;
        messageLabel.text = text ?? string.Empty;
        if (!messageLabel.gameObject.activeSelf) messageLabel.gameObject.SetActive(true);
    }

    private void OnLocalTimerUpdate(float secondsRemaining)
    {
        if (messageLabel == null) return;
        int s = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
        messageLabel.text = $"Technician: Purge commencing in {s}s";
        if (!messageLabel.gameObject.activeSelf) messageLabel.gameObject.SetActive(true);
    }

    private void ClearHudOnEnd()
    {
        if (messageLabel == null) return;
        messageLabel.text = string.Empty;
        messageLabel.gameObject.SetActive(false);
    }
}
