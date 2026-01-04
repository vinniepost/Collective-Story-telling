using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class RepairTaskManager : MonoBehaviour
{
    [Header("Targets (3 tasks in order)")]
    public List<RepairTarget> targets = new List<RepairTarget>();

    [Header("Popup UI (TextMeshPro)")]
    public TextMeshProUGUI popupTextTMP; // Assign a TMP Text in a Canvas
    public float popupDurationSeconds = 5f;
    public bool autoHidePopup = false; // If true, hide after duration; else keep until next update

    [Header("Server Settings")]
    public string serverBaseUrl = "http://localhost:3000";
    public bool notifyServer = true; // Call HTTP endpoints for map/message sync

    private int _currentIndex = 0;
    private float _hideAt = 0f;

    void Start()
    {
        Debug.Log("[RepairTaskManager] Start");
        if (targets.Count == 0)
        {
            Debug.LogWarning("RepairTaskManager: No targets assigned.");
            return;
        }
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
            {
                targets[i].manager = this;
            }
        }
        FocusCurrentTarget();
        if (notifyServer) StartCoroutine(StartServerTask(targets[_currentIndex].taskText, targets[_currentIndex].sectionId));
    }

    void Update()
    {
        if (autoHidePopup && popupTextTMP != null && _hideAt > 0f && Time.time >= _hideAt)
        {
            popupTextTMP.gameObject.SetActive(false);
            _hideAt = 0f;
            Debug.Log("[RepairTaskManager] Popup auto-hidden");
        }
    }

    private void FocusCurrentTarget()
    {
        for (int i = 0; i < targets.Count; i++)
            if (targets[i] != null) targets[i].SetHighlight(i == _currentIndex);

        var sectionId = targets[_currentIndex].sectionId;
        var readableSection = sectionId;
        if (!string.IsNullOrEmpty(sectionId) && sectionId.StartsWith("section_"))
        {
            var num = sectionId.Substring("section_".Length);
            readableSection = $"Section {num}";
        }

        var txt = $"{targets[_currentIndex].taskText} in {readableSection}";
        ShowPopup(txt);
        Debug.Log($"[RepairTaskManager] Focus target index={_currentIndex}, task='{targets[_currentIndex].taskText}'");
    }

    public void OnTargetRepaired(RepairTarget target)
    {
        if (targets[_currentIndex] != target) return; // Only current target progresses
        Debug.Log($"[RepairTaskManager] Target repaired index={_currentIndex}, task='{target.taskText}'");
        // Notify server for this specific repaired section
        if (notifyServer && !string.IsNullOrEmpty(target.sectionId))
        {
            StartCoroutine(NotifyPipeRepaired(target.sectionId));
        }
        _currentIndex++;
        if (_currentIndex >= targets.Count)
        {
            ShowPopup("All repairs completed");
            if (notifyServer) StartCoroutine(NotifyRepairCompleted());
            Debug.Log("[RepairTaskManager] All tasks completed. Notifying server.");
            return;
        }
        FocusCurrentTarget();
        if (notifyServer) StartCoroutine(StartServerTask(targets[_currentIndex].taskText, targets[_currentIndex].sectionId));
    }

    private void ShowPopup(string text)
    {
        if (popupTextTMP == null) return;
        popupTextTMP.text = text;
        popupTextTMP.gameObject.SetActive(true);
        if (autoHidePopup)
        {
            _hideAt = Time.time + popupDurationSeconds;
        }
        Debug.Log($"[RepairTaskManager] Popup: {text}");
    }

    private IEnumerator StartServerTask(string text, string sectionId)
    {
        var url = serverBaseUrl.TrimEnd('/') + "/api/start-repair";
        WWWForm form = new WWWForm();
        form.AddField("text", text);
        if (!string.IsNullOrEmpty(sectionId))
        {
            form.AddField("sectionId", sectionId);
        }
        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            Debug.Log($"[RepairTaskManager] POST {url} text='{text}' sectionId='{sectionId}'");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[RepairTaskManager] StartServerTask failed: " + req.error);
            }
            else
            {
                Debug.Log("[RepairTaskManager] StartServerTask OK");
            }
        }
    }

    private IEnumerator NotifyRepairCompleted()
    {
        var url = serverBaseUrl.TrimEnd('/') + "/api/repair-completed";
        WWWForm form = new WWWForm();
        // Final message for all repairs
        var completionText = "All pipes repaired";
        form.AddField("text", completionText);
        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            Debug.Log($"[RepairTaskManager] POST {url}");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[RepairTaskManager] NotifyRepairCompleted failed: " + req.error);
            }
            else
            {
                Debug.Log("[RepairTaskManager] NotifyRepairCompleted OK");
            }
        }
    }

    private IEnumerator NotifyPipeRepaired(string sectionId)
    {
        var url = serverBaseUrl.TrimEnd('/') + "/api/repair-completed";
        WWWForm form = new WWWForm();
        form.AddField("text", "REPAIR COMPLETED");
        form.AddField("sectionId", sectionId);
        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            Debug.Log($"[RepairTaskManager] POST {url} sectionId='{sectionId}'");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[RepairTaskManager] NotifyPipeRepaired failed: " + req.error);
            }
            else
            {
                Debug.Log("[RepairTaskManager] NotifyPipeRepaired OK");
            }
        }
    }
}