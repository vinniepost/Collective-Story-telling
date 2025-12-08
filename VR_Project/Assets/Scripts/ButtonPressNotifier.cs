using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class ButtonPressNotifier : MonoBehaviour
{
    public UnityEvent OnButtonPressed; // Assigned in inspector

    private void Awake()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnPress);
    }

    private void OnPress(SelectEnterEventArgs args)
    {
        Debug.Log($"{gameObject.name} was pressed");
        OnButtonPressed.Invoke();
    }
}
