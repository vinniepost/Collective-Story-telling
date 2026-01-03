using UnityEngine;

public class RepairTarget : MonoBehaviour
{
    public string taskText = "Repair this module";
    [Tooltip("Map section ID to highlight on the web (e.g., section_1)")]
    public string sectionId = "section_1";
    public Color highlightColor = Color.yellow;
    public float interactDistance = 2.0f;
    public Transform player; // Assign your VR player/camera rig
    public RepairTaskManager manager;

    private bool _isRepaired = false;
    private Renderer _renderer;
    private Color _originalEmission;
    private bool _hasEmission = false;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null && _renderer.material.HasProperty("_EmissionColor"))
        {
            _originalEmission = _renderer.material.GetColor("_EmissionColor");
            _hasEmission = true;
            Debug.Log($"[RepairTarget:{name}] Emission supported. Original={_originalEmission}");
        }
        else
        {
            Debug.Log($"[RepairTarget:{name}] No emission on material.");
        }
    }

    public void SetHighlight(bool on)
    {
        if (_hasEmission)
        {
            _renderer.material.EnableKeyword("_EMISSION");
            _renderer.material.SetColor("_EmissionColor", on ? highlightColor : _originalEmission);
        }
        Debug.Log($"[RepairTarget:{name}] Highlight {(on ? "ON" : "OFF")}");
    }

    void OnMouseDown()
    {
        Debug.Log($"[RepairTarget:{name}] OnMouseDown");
        TryRepair();
    }

    public void OnInteracted() // For XR events
    {
        Debug.Log($"[RepairTarget:{name}] OnInteracted (XR)");
        TryRepair();
    }

    private void TryRepair()
    {
        if (_isRepaired) return;
        if (player != null)
        {
            float d = Vector3.Distance(player.position, transform.position);
            if (d > interactDistance)
            {
                Debug.Log($"[RepairTarget:{name}] Too far (d={d:F2} > {interactDistance})");
                return;
            }
            Debug.Log($"[RepairTarget:{name}] Distance OK (d={d:F2} <= {interactDistance})");
        }

        _isRepaired = true;
        SetHighlight(false);
        if (manager != null)
        {
            Debug.Log($"[RepairTarget:{name}] Notifying manager");
            manager.OnTargetRepaired(this);
        }
        else
        {
            Debug.LogWarning($"[RepairTarget:{name}] Manager not set");
        }
    }
}