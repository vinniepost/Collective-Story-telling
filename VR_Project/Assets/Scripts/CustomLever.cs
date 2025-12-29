using System;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    [SerializeField] private bool sceneCangeBool = false;
    

    [Header("Light Settings")]
    [SerializeField] private Light lightSource;

    [Header("Teleport Settings")]
    [SerializeField] private GameObject destination;

    [Header("Handle settings")]
    [SerializeField] private bool status = false;
    [SerializeField] private GameObject handle;
    [SerializeField] private float triggerValue = 80f;

    [Header("Scene change settings")]
    [SerializeField] private string sceneName;


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
        else if (status && teleportBool)
        {
            Debug.Log("Teleprt");
            ChangeScene(sceneName);
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

    private void ChangeScene(string sceneName)
    {
        if (sceneName == null)
        {
            Debug.Log($"No scene with name {sceneName} found, renaming it to main.");
            sceneName = "main";
        }


        SceneManager.LoadScene(sceneName);

    }
}
