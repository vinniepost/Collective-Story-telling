using UnityEngine;

public class RepairTarget : MonoBehaviour
{
    public string taskText = "Repair this module";
    [Tooltip("Map section ID to highlight on the web (e.g., section_1)")]
    public string sectionId = "section_1";
    public Color highlightColor = Color.yellow;
    public RepairTaskManager manager;
    public PipeIndicator indicator;

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
        if (indicator != null)
        {
            indicator.SetRepaired(false);
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

        _isRepaired = true;
        if (indicator != null)
        {
            indicator.SetRepaired(true);
        }
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