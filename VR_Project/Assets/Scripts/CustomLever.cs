using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CustomLever : MonoBehaviour
{
    [Header("Public variable")]
    public bool LeverStatus { get { return status; } set { status = value; } }

    [Header("Event Function")]
    [SerializeField] private bool lightBool = false;
    [SerializeField] private bool teleportBool = false;
    

    [Header("Light Settings")]
    [SerializeField] private Light lightSource;

    [Header("Teleport Settings")]
    [SerializeField] private GameObject destination;

    [Header("Handle settings")]
    [SerializeField] private bool status = false;
    [SerializeField] private GameObject handle;
    [SerializeField] private float triggerValue = 80f;


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

        if (status && lightBool)
        {
            Debug.Log("Light");
            ChangeLightStatus(lightSource);
        }
        else if (status && teleportBool)
        {
        }
        else if (!status) { }
        else
        { Debug.Log($"{this.gameObject.name} has no function assigned to it"); }

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

    private void ChangeLightStatus(Light light)
    {
        float currentStatus = light.intensity;
        Debug.Log(currentStatus);
        if (currentStatus > 0f)
        {
            light.intensity = 0f;
        }
        /*else 
        { 
            light.intensity = 1f;
        }*/
    }

}
