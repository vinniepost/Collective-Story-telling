using UnityEngine;
using TMPro;

public class CodeRedUI : MonoBehaviour
{
    public CodeRedManager manager;
    public TextMeshProUGUI timerText;
    public GameObject uiContainer;

    private void Start()
    {
        if (uiContainer != null) uiContainer.SetActive(false);
        
        if (manager != null)
        {
            manager.OnCodeRedStart.AddListener(ShowUI);
            manager.OnTimerUpdate.AddListener(UpdateTimer);
            manager.OnCodeRedEnd.AddListener(HideUI);
        }
    }

    private void ShowUI()
    {
        if (uiContainer != null) uiContainer.SetActive(true);
    }

    private void HideUI()
    {
        if (uiContainer != null) uiContainer.SetActive(false);
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            // Format mm:ss
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            
            // Optional: Make it red/blink when low
            if (timeRemaining < 10) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }
}
