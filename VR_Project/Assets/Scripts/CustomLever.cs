using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CustomLever : MonoBehaviour
{
    private bool status = false;
    public bool LeverStatus { get { return status; } set { status = value; } }
    private GameObject handle;
    private float triggerValue = 80f;

    private void Start()
    {
        if (handle == null)
        {
            handle = this.gameObject;
        }
    }
    private void Update()
    {
        status = StatusChange(handle, triggerValue);
    }

    /// <summary>
    /// Returns true when the current rotation is larger then the triggerValue
    ///
    /// </summary>
    /// <param name="handle">Object that is being rotated</param>
    /// <param name="triggerValue">Value for the rotation, should be somewhere around 80</param>
    /// <returns></returns>
    private bool StatusChange(GameObject handle, float triggerValue = 80f)
    {
        float currentValue = handle.transform.localEulerAngles.x;
        if (currentValue < triggerValue) return true;
        else return false;
    }

    public void HapticTest()
    {
        float currentValue = handle.transform.localEulerAngles.x;
        Debug.Log(currentValue);
        if (currentValue > 200f)
        {
            Debug.Log("Haptic feedback test");
            StartHaptics(1f, 1f, 2f);
        }
    }


    public void StartHaptics(float lowFreq, float highFreq, float duration)
    {
        Debug.Log(Gamepad.current);
        if (Gamepad.current == null)
            return;

        Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);
        Invoke(nameof(StopHaptics), duration);
    }

    public void StopHaptics()
    {
        if (Gamepad.current == null)
            return;

        Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}
