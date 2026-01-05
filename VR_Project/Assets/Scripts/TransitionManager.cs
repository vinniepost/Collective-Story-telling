using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.XR.CoreUtils;


public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;
    public CanvasGroup fadeGroup; 
    public AudioSource audioSource; // Assign an AudioSource here
    [Tooltip("Tag used to find the spawn point in the target scene")]
    public string spawnPointTag = "SpawnPoint";
    [Tooltip("Disable teleport/locomotion while transitioning to avoid accidental input")] 
    public bool disableTeleportDuringTransition = true;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void TeleportToMainMap(string sceneName, AudioClip storyClip)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        // Ensure fade canvas exists and is ready
        if (fadeGroup != null)
        {
            // Try to position the fade in front of the current camera (without risking destruction on scene swap)
            Camera vrCam = Camera.main;
            if (vrCam != null)
            {
                // Temporarily parent during the transition, but we'll reattach after activation as well
                fadeGroup.transform.SetParent(vrCam.transform, false);
                fadeGroup.transform.localPosition = new Vector3(0, 0, 0.5f);
                fadeGroup.transform.localRotation = Quaternion.identity;
            }
        }

        StartCoroutine(TransitionSequence(sceneName, storyClip));
    }

  IEnumerator TransitionSequence(string sceneName, AudioClip storyClip)
{
    // 1. Fade to Black
    yield return StartCoroutine(Fade(1));

        // Optionally disable teleport/locomotion providers while transitioning
        UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider[] disabledProviders = null;
        UnityEngine.CharacterController[] disabledCharacterControllers = null;
        if (disableTeleportDuringTransition)
        {
            disabledProviders = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);
            foreach (var tp in disabledProviders)
                tp.enabled = false;
            // Also disable CharacterControllers to avoid physics snaps during rig placement
            disabledCharacterControllers = FindObjectsOfType<UnityEngine.CharacterController>(true);
            foreach (var cc in disabledCharacterControllers)
                cc.enabled = false;
        }

    // 2. START LOADING IN BACKGROUND (but don't switch scenes yet)
    AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
    operation.allowSceneActivation = false; // This is the secret!

    // Wait until Unity has loaded everything it can (it stops at 0.9)
    while (operation.progress < 0.9f)
    {
        yield return null; 
    }

    // 3. NOW play the audio while the scene is ready and waiting
    if (storyClip != null && audioSource != null)
    {
        audioSource.clip = storyClip;
        audioSource.Play();
        while (audioSource.isPlaying) { yield return null; }
    }

    // 4. ACTIVATE the scene (Instantly swaps because it's already in memory)
    operation.allowSceneActivation = true;

    // Wait until the scene is fully swapped
    while (!operation.isDone) { yield return null; }

    // Reattach fade to the new camera (old camera may have been destroyed)
    if (fadeGroup != null)
    {
        Camera newCam = Camera.main;
        if (newCam != null)
        {
            fadeGroup.transform.SetParent(newCam.transform, false);
            fadeGroup.transform.localPosition = new Vector3(0, 0, 0.5f);
            fadeGroup.transform.localRotation = Quaternion.identity;
        }
    }

    // Place XR Origin at a spawn point to avoid recenter/respawn feeling
    TryPositionXROriginAtSpawn();

    // 5. Fade back in
    yield return StartCoroutine(Fade(0));

    // Re-enable any disabled teleport providers
    if (disabledProviders != null)
    {
        foreach (var tp in disabledProviders)
            tp.enabled = true;
    }
    if (disabledCharacterControllers != null)
    {
        foreach (var cc in disabledCharacterControllers)
            cc.enabled = true;
    }
    isTransitioning = false;
}

    IEnumerator Fade(float targetAlpha)
    {
        float speed = 0.8f; 
        while (!Mathf.Approximately(fadeGroup.alpha, targetAlpha))
        {
            fadeGroup.alpha = Mathf.MoveTowards(fadeGroup.alpha, targetAlpha, speed * Time.deltaTime);
            yield return null;
        }
    }

    void TryPositionXROriginAtSpawn()
    {
        // Find XR Origin in the new scene
        var xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
            return;

        // Find a spawn point by tag; creator should place an empty GameObject with this tag
        var spawnGO = GameObject.FindWithTag(spawnPointTag);
        if (spawnGO == null)
            return;

        var spawn = spawnGO.transform;

        // Move origin and align orientation
        // MoveCameraToWorldLocation ensures the camera ends up exactly at the given world position
        xrOrigin.MoveCameraToWorldLocation(spawn.position);
        xrOrigin.MatchOriginUpCameraForward(spawn.up, spawn.forward);
    }

    // Optional: Additive variant if you choose to keep a single persistent XR rig in a bootstrap scene
    public void TeleportToMainMapAdditive(string sceneName, AudioClip storyClip)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(TransitionSequenceAdditive(sceneName, storyClip));
    }

    IEnumerator TransitionSequenceAdditive(string sceneName, AudioClip storyClip)
    {
        // Fade to black and optionally disable teleport
        yield return StartCoroutine(Fade(1));

        UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider[] disabledProviders = null;
        UnityEngine.CharacterController[] disabledCharacterControllers = null;
        if (disableTeleportDuringTransition)
        {
            disabledProviders = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);
            foreach (var tp in disabledProviders)
                tp.enabled = false;
            disabledCharacterControllers = FindObjectsOfType<UnityEngine.CharacterController>(true);
            foreach (var cc in disabledCharacterControllers)
                cc.enabled = false;
        }

        // Begin loading the target scene additively
        var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f)
            yield return null;

        // Play voice-over while the scene is ready
        if (storyClip != null && audioSource != null)
        {
            audioSource.clip = storyClip;
            audioSource.Play();
            while (audioSource.isPlaying) { yield return null; }
        }

        // Activate the additive scene and set it active
        operation.allowSceneActivation = true;
        while (!operation.isDone) { yield return null; }

        var loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid())
            SceneManager.SetActiveScene(loadedScene);

        // Attach fade to new camera and position XR Origin
        if (fadeGroup != null)
        {
            Camera newCam = Camera.main;
            if (newCam != null)
            {
                fadeGroup.transform.SetParent(newCam.transform, false);
                fadeGroup.transform.localPosition = new Vector3(0, 0, 0.5f);
                fadeGroup.transform.localRotation = Quaternion.identity;
            }
        }
        TryPositionXROriginAtSpawn();

        // Fade back in and re-enable teleport
        yield return StartCoroutine(Fade(0));
        if (disabledProviders != null)
        {
            foreach (var tp in disabledProviders)
                tp.enabled = true;
        }
        if (disabledCharacterControllers != null)
        {
            foreach (var cc in disabledCharacterControllers)
                cc.enabled = true;
        }

        // Note: Unloading the previous scene safely requires your XR rig to be persistent
        // and not duplicated in the newly loaded scene. Otherwise skip unloading here.
    }
}