using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CustomLever : MonoBehaviour
{
    [Header("Rotation Limits")]
    public float minAngle = -75f;
    public float maxAngle = 75f;

    private float startY;
    private float startZ;

    void Start()
    {
        // Store untouched axes
        Vector3 startRot = transform.eulerAngles;
        startY = startRot.y;
        startZ = startRot.z;
    }

    void Update()
    {
        Vector3 localRot = transform.localEulerAngles;

        float x = NormalizeAngle(localRot.x);
        x = Mathf.Clamp(x, minAngle, maxAngle);

        transform.localEulerAngles = new Vector3(
            x,
            startY,
            startZ
        );
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 90f)
            angle -= 180f;
        else if (angle < -90f)
            angle += 180f;
        return angle;
    }

    /// <summary>
    /// Returns lever position from 0 (min) to 1 (max)
    /// </summary>
    public float GetValue01()
    {
        float x = NormalizeAngle(transform.localEulerAngles.x);
        return Mathf.InverseLerp(minAngle, maxAngle, x);
    }
}
