using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Interaction.Toolkit;


public class CluePickup : MonoBehaviour
{
    [Header("Server")]
    public string serverBaseUrl = "http://localhost:3000";

    [Header("Behavior")]
    public bool oneShot = true;
    public bool destroyOnCollect = false;
    public float cooldownSeconds = 1.0f;

    [Header("Post-Collect Handling")]
    public bool disableOnCollect = false;
    public bool disableXRInteractableOnCollect = false;
    public bool disableCollidersOnCollect = false;
    public bool setIgnoreRaycastLayerOnCollect = false;
    public bool releaseXROnCollect = false;

    [Header("XR Interaction Layer")]
    public bool useClueInteractionLayer = false;
    public string clueInteractionLayerName = "Clue"; // Configure in Project Settings > XR Interaction Toolkit

    [Header("Debug")]
    public bool logRequests = true;

    private bool _collected = false;
    private float _nextAllowedTime = 0f;
    public bool IsCollected => _collected;

    [System.Serializable]
    private class ClueCollectResponse
    {
        public bool ok;
        public int clues;
        public bool bonusApplied;
        public bool codeRedActive;
        public int remainingSeconds;
    }

    // Optional: call from XR binder
    public void OnInteracted()
    {
        if (logRequests) Debug.Log($"[CluePickup] OnInteracted → {name}");
        TryCollect();
    }

#if UNITY_EDITOR
    private void OnMouseDown()
    {
        TryCollect();
    }
#endif

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (logRequests) Debug.Log($"[CluePickup] OnTriggerEnter by Player → {name}");
            TryCollect();
        }
    }

    public void TryCollect()
    {
        if (Time.time < _nextAllowedTime)
        {
            if (logRequests) Debug.Log($"[CluePickup] Skipped: cooldown active for {Mathf.Max(0f, _nextAllowedTime - Time.time):0.00}s");
            return;
        }
        if (oneShot && _collected)
        {
            if (logRequests) Debug.Log("[CluePickup] Skipped: already collected (oneShot)");
            return;
        }

        if (logRequests) Debug.Log("[CluePickup] Collecting clue…");

        StartCoroutine(PostClueCollect());
    }

    private void Start()
    {
        // Register with clue manager for local tracking
        if (ClueSetManager.Instance != null) {
            ClueSetManager.Instance.Register(this);
        }

        if (useClueInteractionLayer)
        {
            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (interactable != null)
            {
                try
                {
                    // XR Interaction Toolkit updated API: use interactionLayers
                    interactable.interactionLayers = InteractionLayerMask.GetMask(clueInteractionLayerName);
                    if (logRequests) Debug.Log($"[CluePickup] Set interaction layer mask to '{clueInteractionLayerName}'");
                }
                catch (System.Exception)
                {
                    Debug.LogWarning("[CluePickup] Interaction Layer '" + clueInteractionLayerName + "' not found. Configure it in Project Settings > XR Interaction Toolkit.");
                }
            }
        }
    }

    private IEnumerator PostClueCollect()
    {
        _nextAllowedTime = Time.time + cooldownSeconds;

        var url = serverBaseUrl.TrimEnd('/') + "/api/clue-collect";
        var payload = "{\"silent\":true}"; // no text feedback to web users
        var bodyRaw = Encoding.UTF8.GetBytes(payload);

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            if (logRequests) Debug.Log($"[CluePickup] POST {url} body={payload}");
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result == UnityWebRequest.Result.Success)
#else
            if (!req.isNetworkError && !req.isHttpError)
#endif
            {
                string json = req.downloadHandler.text;
                if (logRequests) Debug.Log($"[CluePickup] Success (HTTP {req.responseCode}): {json}");

                try
                {
                    var resp = JsonUtility.FromJson<ClueCollectResponse>(json);
                    if (resp != null && logRequests)
                    {
                        Debug.Log($"[CluePickup] Server acknowledged: ok={resp.ok}, clues={resp.clues}/{4}, bonusApplied={resp.bonusApplied}, codeRedActive={resp.codeRedActive}, remaining={resp.remainingSeconds}s");
                    }
                }
                catch { /* ignore parse errors */ }
                // Optionally release XR selection/hover to prevent stuck grabs
                if (releaseXROnCollect) ReleaseXRInteractions();
                _collected = true;
                if (destroyOnCollect)
                {
                    if (logRequests) Debug.Log("[CluePickup] Destroying object on collect");
                    Destroy(gameObject);
                }
                else if (disableOnCollect)
                {
                    if (disableXRInteractableOnCollect)
                    {
                        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
                        if (interactable != null)
                        {
                            interactable.enabled = false;
                            if (logRequests) Debug.Log("[CluePickup] XRBaseInteractable disabled after collect");
                        }
                    }

                    if (disableCollidersOnCollect)
                    {
                        var colliders = GetComponentsInChildren<Collider>(true);
                        foreach (var col in colliders) col.enabled = false;
                        if (logRequests) Debug.Log($"[CluePickup] Disabled {colliders.Length} collider(s) after collect");
                    }

                    if (setIgnoreRaycastLayerOnCollect)
                    {
                        gameObject.layer = 2; // Ignore Raycast
                        if (logRequests) Debug.Log("[CluePickup] Set layer to Ignore Raycast (2)");
                    }
                }

                // Notify manager after local collect
                if (ClueSetManager.Instance != null) {
                    ClueSetManager.Instance.NotifyCollected(this);
                }
            }
            else
            {
                Debug.LogWarning($"[CluePickup] Error (HTTP {req.responseCode}): {req.error}");
            }
        }
    }

    private void ReleaseXRInteractions()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (interactable == null) return;
        var manager = interactable.interactionManager;
        if (manager == null) return;

        var selecting = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>(interactable.interactorsSelecting);
        var hovering = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRHoverInteractor>(interactable.interactorsHovering);

        foreach (var inter in selecting)
        {
            try { manager.SelectExit(inter, interactable); } catch { }
        }
        foreach (var inter in hovering)
        {
            try { manager.HoverExit(inter, interactable); } catch { }
        }

        if (logRequests) Debug.Log($"[CluePickup] Released {selecting.Count} selecting and {hovering.Count} hovering interactor(s)");
    }
}
