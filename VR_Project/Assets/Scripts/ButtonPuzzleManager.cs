using UnityEngine;
using TMPro;

public class ButtonPuzzleManager : MonoBehaviour
{
    [SerializeField] private int totalButtons = 3;
    private int pressedCount = 0;

    [SerializeField] private TMP_Text counterText;   // <- de UI tekst

    private void Start()
    {
        UpdateText();
    }

    public void RegisterButtonPress()
    {
        if (pressedCount < totalButtons)
        {
            pressedCount++;
            Debug.Log($"Buttons pressed: {pressedCount}/{totalButtons}");
            UpdateText();

            if (pressedCount >= totalButtons)
            {
                Debug.Log("Pressed ALL the buttons!");
                // Hier later je deur openen of wat dan ook
            }
        }
    }

    private void UpdateText()
    {
        if (counterText != null)
        {
            counterText.text = $"Buttons pressed: {pressedCount}/{totalButtons}";
        }
    }
}
