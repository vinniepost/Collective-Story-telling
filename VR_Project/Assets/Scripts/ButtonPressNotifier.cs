using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class ButtonPressNotifier : MonoBehaviour
{
    public UnityEvent OnButtonPressed;

    private bool alreadyPressed = false;

    private void Awake()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnPress);
    }

    private void OnPress(SelectEnterEventArgs args)
    {
        if (alreadyPressed)
            return; // <-- blokkeer dubbel drukken

        alreadyPressed = true;
        Debug.Log($"{gameObject.name} was pressed");

        OnButtonPressed.Invoke();
    }
}
