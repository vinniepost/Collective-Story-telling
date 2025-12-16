using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SmartStepOffset : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The step offset to use when there is plenty of headroom (e.g., 0.3)")]
    public float defaultStepOffset = 0.3f;
    
    [Tooltip("The lowest allowed step offset. Prevents getting stuck on tiny floor seams.")]
    public float minStepOffset = 0.05f;

    [Tooltip("How wide of a check to cast upwards. Should match your collider radius roughly.")]
    public float checkRadius = 0.15f;

    [Header("References")]
    public CharacterController characterController;
    public Transform headTransform; // Assign your Main Camera here

    // Debug info
    private bool _hasHit;
    private RaycastHit _hitInfo;

    private void Update()
    {
        if (characterController == null || headTransform == null) return;

        // 1. Where is the top of the player's physical collider right now?
        // Note: We use the camera position for the origin, but we need to start the check 
        // slightly below the top of the collider to avoid starting 'inside' the ceiling. 
        Vector3 startPos = headTransform.position;

        // 2. Cast a sphere upwards to see how far the ceiling is
        RaycastHit hit;
        float headroom = 10.0f; // Arbitrary large number if no ceiling found

        // We cast upward. The max distance is the default step offset + a little buffer.
        if (Physics.SphereCast(startPos, checkRadius, Vector3.up, out hit, defaultStepOffset + 0.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            headroom = hit.distance; // This is the distance from our start point to the ceiling
            _hasHit = true;
            _hitInfo = hit;
        }
        else
        {
            _hasHit = false;
        }

        // 3. Logic: You can't step up higher than the empty space above your head.
        // We subtract a small 'safety buffer' (0.05) to ensure we don't scrape the ceiling.
        float maxAllowedStep = headroom - 0.05f;

        // 4. Apply the calculated offset, clamped between our Min and Default settings.
        characterController.stepOffset = Mathf.Clamp(maxAllowedStep, minStepOffset, defaultStepOffset);
    }

    // Visualization for Debugging
    private void OnDrawGizmos()
    {
        if (characterController != null)
        {
            float capsuleTop = characterController.height;
            Vector3 startPos = headTransform.position;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(startPos, checkRadius);
            Gizmos.DrawLine(startPos, startPos + Vector3.up * defaultStepOffset);

            if (_hasHit)
            {
                // Draw the sphere where it hit the ceiling
                Gizmos.color = Color.red;
                Vector3 sphereCenterAtHit = startPos + Vector3.up * _hitInfo.distance;
                Gizmos.DrawWireSphere(sphereCenterAtHit, checkRadius);
                
                // Draw the contact point
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(_hitInfo.point, 0.02f);
            }
        }
    }
}