using UnityEngine;
public class DoorController : MonoBehaviour
{
    [Header("Identity")]
    public string doorID;

    [Header("References")]
    public Animator doorAnimator;
    public Light statusLight;
    public CustomLever lever; 
    
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
            if (lever.LeverStatus) 
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
                statusLight.enabled = true;
            }
            else if (unlockable)
            {
                statusLight.color = unlockableColor;
                statusLight.intensity = 5f;
                statusLight.enabled = true;
            }
            else
            {
                statusLight.enabled = false;
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
