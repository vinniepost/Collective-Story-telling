using UnityEngine;

public class ButtonPuzzleManager : MonoBehaviour
{
    [SerializeField] private int totalButtons = 3; // set this in inspector
    private int pressedCount = 0;

    public void RegisterButtonPress()
    {
        pressedCount++;
        Debug.Log($"Buttons pressed: {pressedCount}/{totalButtons}");

        if (pressedCount >= totalButtons)
        {
            Debug.Log("Pressed ALL the buttons!");
            // TODO: Trigger your final action here
        }
    }
}
