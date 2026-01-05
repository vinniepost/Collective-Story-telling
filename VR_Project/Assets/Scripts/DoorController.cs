using UnityEngine;
public class DoorController : MonoBehaviour
{
    [Header("Identity")]
    public string doorID;

    [Header("References")]
    public Animator doorAnimator;
    public Light statusLight;
    public Light statusLight2;
    public CustomLever lever;
    public CustomLever lever2;
    
    [Header("Settings")]
    public Color lockedColor = Color.red;
    public Color unlockableColor = Color.green;
    public Color openColor = Color.black; // Or off

    private bool isLocked = false;
    private bool canBeOpened = false;

    private void Start()
    {
        // Initial Visuals
        UpdateVisuals(false, false);
    }

    private void Update()
    {
        if (lever != null && canBeOpened)
        {
            // Check if player pulled lever
            if (lever.LeverStatus || lever2.LeverStatus) 
            {
                OnLeverPulled();
            }
        }
    }

    // Helper to set visuals based on state
    private void UpdateVisuals(bool locked, bool unlockable)
    {
        if (statusLight != null)
        {
            if (locked)
            {
                statusLight.color = lockedColor;
                statusLight.intensity = 5f;
                statusLight2.color = lockedColor;
                statusLight2.intensity = 5f;
            }
            else if (unlockable)
            {
                statusLight.color = unlockableColor;
                statusLight.intensity = 5f;
                statusLight2.color = unlockableColor;
                statusLight2.intensity = 5f;
            }
            else
            {
                statusLight.color = openColor;
                statusLight.intensity = 1f;
                statusLight2.color = openColor;
                statusLight2.intensity = 1f;
            }
        }
    }

    // Called by MapControlManager when Server says "DOOR LOCKED"
    public void LockDoor()
    {
        isLocked = true;
        canBeOpened = false;
        
        if (doorAnimator != null) doorAnimator.SetBool("IsClosed", true);
        
        UpdateVisuals(true, false);

        if (lever != null) lever.LeverStatus = false; 
        if (lever2 != null) lever2.LeverStatus = false;
    }

    // Called by MapControlManager when Server says "DOOR UNLOCKABLE"
    public void MakeUnlockable()
    {
        isLocked = false;
        canBeOpened = true;

        UpdateVisuals(false, true);
    }

    private void OnLeverPulled()
    {
        if (!canBeOpened) return;

        Debug.Log($"[DoorController] Lever Pulled for {doorID}");

        if (doorAnimator != null) doorAnimator.SetBool("IsClosed", false);

        UpdateVisuals(false, false);

        // Reset State
        canBeOpened = false;
        isLocked = false;

        // Reset Lever if needed (CustomLever logic depends on this)
        // lever.LeverStatus = false; 

        // Notify Server
        WebSocketController.Instance.SendDoorOpened(doorID);
    }
}
