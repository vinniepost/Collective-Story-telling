using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Optional helper: automatically bind XR select events to RepairTarget.
// Add this component on the same GameObject as your RepairTarget and XRSimpleInteractable.
public class XRRepairBinder : MonoBehaviour
{
    private RepairTarget _target;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;

    void Awake()
    {
        _target = GetComponent<RepairTarget>();
        // Bind to any XRBaseInteractable (works for Simple or Grab Interactable)
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
            Debug.Log($"[XRRepairBinder:{name}] Bound selectEntered to RepairTarget");
        }
        else
        {
            Debug.LogWarning($"[XRRepairBinder:{name}] No XRBaseInteractable found on object");
        }
    }

    private void OnDestroy()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (_target != null)
        {
            Debug.Log($"[XRRepairBinder:{name}] selectEntered by {args.interactorObject}");
            _target.OnInteracted();
        }
    }
}