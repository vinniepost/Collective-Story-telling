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


    [Header("Reset Settings")]
    [SerializeField] private bool autoReset = true;
    [SerializeField] private float resetDelay = 3.0f;
    [SerializeField] private float resetDuration = 0.5f;

    private Quaternion initialRotation;
    private bool isResetting = false;

    private void Start()
    {
        if (handle == null)
        {
            handle = this.gameObject;
        }
        initialRotation = handle.transform.localRotation;
    }

    private void Update()
    {
        // Only update status if not currently resetting
        if (!isResetting)
        {
            bool newStatus = StatusChange(handle, triggerValue);
            
            // If status changed to true and we want auto reset
            if (newStatus && !status && autoReset)
            {
                StartCoroutine(ResetLeverRoutine());
            }

            status = newStatus;

            if (status) 
            {
                if (lightBool) ChangeLightStatus(lightSource);
                if (teleportBool) { /* Teleport Logic */ }
                if (sceneCangeBool) ChangeScene(sceneName);
            }
        }
    }

    private System.Collections.IEnumerator ResetLeverRoutine()
    {
        yield return new WaitForSeconds(resetDelay);

        isResetting = true;
        
        // Disable interactions? If it's XR Interactable, might need to force drop.
        // For now, simpler lerp of transform.
        
        Quaternion currentRot = handle.transform.localRotation;
        float elapsed = 0f;

        while (elapsed < resetDuration)
        {
            handle.transform.localRotation = Quaternion.Slerp(currentRot, initialRotation, elapsed / resetDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        handle.transform.localRotation = initialRotation;
        status = false; // Force status false
        isResetting = false;
        
        Debug.Log("Lever Reset");
    }

    /// <summary>
    /// Returns true when the current rotation is larger then the triggerValue
    /// </summary>
    private bool StatusChange(GameObject handle, float triggerValue = 80f)
    {
        float currentValue = handle.transform.localEulerAngles.x;
        // Fix for Euler angles wrapping (if needed). 
        // Assuming simplistic check as per original code.
        return currentValue < triggerValue;
    }

    private void ChangeLightStatus(Light light)
    {
        if (light == null) return;
        float currentStatus = light.intensity;
        if (currentStatus > 0f)
        {
            light.intensity = 0f;
        }
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
