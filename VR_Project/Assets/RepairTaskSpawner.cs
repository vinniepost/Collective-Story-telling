using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
#if TMP_PRESENT
using TMPro;
#endif
using UnityEngine.UI;

public class RepairTaskSpawner : MonoBehaviour
{
    [Header("Server Settings")]
    [Tooltip("Base URL of the Node server")]
    public string serverBaseUrl = "http://localhost:3000";

    [Tooltip("Optional specific section to highlight (e.g., section_5)")]
    public string sectionIdOverride = "";

    [Header("UI References")]
#if TMP_PRESENT
    public TextMeshProUGUI popupTextTMP;
#endif
    public Text popupText;

    [Tooltip("Seconds the popup stays visible")]
    public float popupDurationSeconds = 6f;

    private Canvas _canvas;

    private void Awake()
    {
        // Ensure we have a Canvas for overlay if none assigned
        _canvas = GetComponentInChildren<Canvas>();
        if (_canvas == null)
        {
            var go = new GameObject("RepairTaskCanvas");
            go.transform.SetParent(this.transform);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            // Create a Text element if none present
            var textGO = new GameObject("RepairTaskText");
            textGO.transform.SetParent(go.transform);
#if TMP_PRESENT
            popupTextTMP = textGO.AddComponent<TextMeshProUGUI>();
            popupTextTMP.fontSize = 28f;
            popupTextTMP.color = Color.yellow;
            popupTextTMP.alignment = TextAlignmentOptions.TopRight;
#else
            popupText = textGO.AddComponent<Text>();
            popupText.fontSize = 24;
            popupText.color = Color.yellow;
            popupText.alignment = TextAnchor.UpperRight;
#endif
            var rect = textGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f); // 20px from top-right
            rect.sizeDelta = new Vector2(600f, 80f);
        }
    }

    private void Start()
    {
        // Trigger when player spawns in the main scene
        StartCoroutine(RequestRepairTask());
    }

    private IEnumerator RequestRepairTask()
    {
        var url = serverBaseUrl.TrimEnd('/') + "/api/start-repair";
        WWWForm form = new WWWForm();
        if (!string.IsNullOrEmpty(sectionIdOverride))
        {
            form.AddField("sectionId", sectionIdOverride);
        }

        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                ShowText("Repair task failed to load");
                yield break;
            }

            var json = req.downloadHandler.text;
            var response = JsonUtility.FromJson<RepairResponse>(json);
            if (response != null && !string.IsNullOrEmpty(response.text))
            {
                ShowText($"REPAIR: {response.text} ({response.sectionId})");
            }
            else
            {
                ShowText("Repair task unavailable");
            }
        }
    }

    private void ShowText(string text)
    {
#if TMP_PRESENT
        if (popupTextTMP != null)
        {
            popupTextTMP.text = text;
            popupTextTMP.gameObject.SetActive(true);
        }
#endif
        if (popupText != null)
        {
            popupText.text = text;
            popupText.gameObject.SetActive(true);
        }
        StopAllCoroutines();
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(popupDurationSeconds);
#if TMP_PRESENT
        if (popupTextTMP != null) popupTextTMP.gameObject.SetActive(false);
#endif
        if (popupText != null) popupText.gameObject.SetActive(false);
    }

    [System.Serializable]
    private class RepairResponse
    {
        public bool ok;
        public string sectionId;
        public string text;
    }
}