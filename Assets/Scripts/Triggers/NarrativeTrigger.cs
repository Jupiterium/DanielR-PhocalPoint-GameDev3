using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))] // Automatically adds an AudioSource
public class NarrativeTrigger : MonoBehaviour
{
    [Header("References")]
    public TMP_Text textComponent;

    [Header("Audio Settings")]
    public AudioClip textAppearSound;
    [Range(0f, 1f)] public float soundVolume = 0.5f;

    [Header("Timing Settings")]
    public float fadeInDuration = 1.5f;
    public float displayDuration = 2.0f;
    public float fadeOutDuration = 1.5f;

    private AudioSource audioSource;
    private bool hasTriggered = false;

    private void Awake()
    {
        // Get the AudioSource attached to this same object
        audioSource = GetComponent<AudioSource>();

        // Ensure settings are optimal for 2D UI sounds
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void Start()
    {
        if (textComponent != null)
        {
            textComponent.gameObject.SetActive(true);
            textComponent.alpha = 0f;
        }
    }

    // Trigger when player enters the collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(PlayTextSequence());
            GetComponent<Collider>().enabled = false;
        }
    }

    private IEnumerator PlayTextSequence()
    {
        // Play Sound
        if (textAppearSound != null)
        {
            audioSource.PlayOneShot(textAppearSound, soundVolume);
        }

        // Fade In
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            textComponent.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
            yield return null;
        }
        textComponent.alpha = 1f;

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            textComponent.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
            yield return null;
        }
        textComponent.alpha = 0f;
        textComponent.gameObject.SetActive(false);
    }
}