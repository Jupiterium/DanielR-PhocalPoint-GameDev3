using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    private AudioSource audioSource;
    private bool hasTriggered = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player and we haven't played yet
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            audioSource.Play();

            Debug.Log("Audio triggered on player entry.");

            // Optional: Fade in logic could go here, but Play() is fine for now.
        }
    }
}
