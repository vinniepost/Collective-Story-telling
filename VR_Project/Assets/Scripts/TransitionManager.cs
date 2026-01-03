using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;
    public CanvasGroup fadeGroup; 
    public AudioSource audioSource; // Assign an AudioSource here

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
        // Snap the black screen to the VR Camera before we start
        Camera vrCam = Camera.main;
        if (vrCam != null)
        {
            fadeGroup.transform.SetParent(vrCam.transform);
            fadeGroup.transform.localPosition = new Vector3(0, 0, 0.5f); // Half a meter in front
            fadeGroup.transform.localRotation = Quaternion.identity;
        }

        StartCoroutine(TransitionSequence(sceneName, storyClip));
    }

  IEnumerator TransitionSequence(string sceneName, AudioClip storyClip)
{
    // 1. Fade to Black
    yield return StartCoroutine(Fade(1));

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

    // 5. Fade back in
    yield return StartCoroutine(Fade(0));
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
}