using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CustomLever : MonoBehaviour
{
    [Header("Lever Settings")]
    public Transform handle;
    public float minAngle = -89f;
    public float maxAngle = 89f;

    [Header("Output")]
    public float leverValue;

    private XRBaseInteractor interactor;

    // Update is called once per frame
    void Update()
    {
        if (interactor == null) return;

        Vector3 localHandPos = transform.InverseTransformPoint(interactor.transform.position);

        float angle = Mathf.Atan2(localHandPos.z, localHandPos.y) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, minAngle, maxAngle);

        handle.localRotation = Quaternion.Euler(angle, 0f, 0f);

        leverValue = Mathf.InverseLerp(minAngle, maxAngle, angle);
    }


    public void Grab(XRBaseInteractor grabber)
    {
        interactor = grabber;
    }

    public void Release()
    {
        interactor = null;
    }
}
