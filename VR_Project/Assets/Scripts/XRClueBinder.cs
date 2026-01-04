using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public class XRClueBinder : MonoBehaviour
{
    public CluePickup clue;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (clue == null)
        {
            clue = GetComponentInParent<CluePickup>();
        }
    }

    private void OnEnable()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
            Debug.Log("[XRClueBinder] selectEntered listener attached");
        }
    }

    private void OnDisable()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            Debug.Log("[XRClueBinder] selectEntered listener removed");
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (clue != null)
        {
            Debug.Log($"[XRClueBinder] selectEntered by {args.interactorObject.transform.name} → forwarding to CluePickup");
            clue.OnInteracted();
        }
        else
        {
            Debug.LogWarning("[XRClueBinder] No CluePickup bound; assign via Inspector or place on same GameObject");
        }
    }
}
