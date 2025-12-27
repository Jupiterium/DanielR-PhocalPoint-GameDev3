using UnityEngine;
using System.Collections;

public class Companion : MonoBehaviour
{
    [System.Serializable]
    public class DialogueEntry
    {
        [TextArea(2, 4)]
        public string message = "System check...";
        public float duration = 4.0f;
        public AudioClip audioClip;
    }

    [Header("Interaction")]
    [Tooltip("Add or remove dialogue entries here")]
    public DialogueEntry[] dialogueSequence = new DialogueEntry[]
    {
        new DialogueEntry()
    };

    [Header("Aftermath")]
    [Tooltip("Which drone appears after this one leaves?")]
    public GameObject nextDroneToActivate;

    [Header("Exit Logic")]
    [Tooltip("If true, it shrinks and vanishes. If false, it flies to the Exit Location.")]
    public bool justDisintegrate = true;
    public Transform specificExitLocation; // Optional: Drag an empty GameObject here
    [Tooltip("Duration (in seconds) for the disintegrate effect")]
    public float disintegrateDuration = 1.0f;
    [Tooltip("Duration (in seconds) for the fly-away effect")]
    public float flyAwayDuration = 1.5f;

    [Header("Visuals")]
    public Renderer meshRenderer;
    public int eyeMaterialIndex = 1;
    public Material eyeNormal;
    public Material eyeHappy;
    public Material eyeDead;

    [Header("Behaviour Tuning")]
    [Tooltip("How quickly the drone rotates to face the player (higher = faster)")]
    [Range(1f, 10f)]
    public float playerLookSpeed = 5f;

    private bool isInteracting = false;
    private Transform player;
    private AudioSource audioSource;
    private Vector3 initialScale;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Safely find player with null check
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning($"[DRONE] Player with tag 'Player' not found! Drone at {transform.position} will not look at player.");
            player = null;
        }
        else
        {
            player = playerObj.transform;
        }

        initialScale = transform.localScale;

        // Start Idle
        SetEye(eyeNormal);

        // Ensure the NEXT drone is hidden at the start (Safety check)
        if (nextDroneToActivate != null)
            nextDroneToActivate.SetActive(false);

        // Validation warnings
        ValidateSetup();
    }

    private void Update()
    {
        // When talking, rotate to face the player smoothly
        if (isInteracting && player != null)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0; // Keep head level
            direction = -direction; // Invert so drone faces toward player instead of away
            Quaternion rot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * playerLookSpeed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInteracting && other.CompareTag("Player"))
        {
            StartCoroutine(InteractionSequence());
        }
    }

    IEnumerator InteractionSequence()
    {
        isInteracting = true;

        // --- PHASE 1: DIALOGUE SEQUENCE ---
        if (dialogueSequence != null && dialogueSequence.Length > 0)
        {
            foreach (DialogueEntry dialogue in dialogueSequence)
            {
                SetEye(eyeHappy);

                // Play audio if assigned
                if (dialogue.audioClip != null && audioSource != null)
                    audioSource.PlayOneShot(dialogue.audioClip);

                // Send Text to Manager (with safety checks)
                if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
                {
                    GameManager.Instance.uiManager.ShowSubtitle(dialogue.message, dialogue.duration);
                }
                else
                {
                    Debug.LogWarning("[DRONE] GameManager or UIManager not found. Message will only appear in console.");
                }
                Debug.Log($"[DRONE]: {dialogue.message}");

                yield return new WaitForSeconds(dialogue.duration);
            }
        }
        else
        {
            Debug.LogWarning("[DRONE] No dialogue entries configured!");
        }

        // --- PHASE 2: EXIT ---
        SetEye(eyeDead);

        if (justDisintegrate)
        {
            // Implode Effect
            float timer = 0;
            while (timer < disintegrateDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / disintegrateDuration;
                // Shrink to zero + Move up slightly
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
                transform.position += Vector3.up * Time.deltaTime;
                yield return null;
            }
        }
        else if (specificExitLocation != null)
        {
            // Fly to target Effect
            float timer = 0;
            while (timer < flyAwayDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / flyAwayDuration;
                transform.position = Vector3.Lerp(transform.position, specificExitLocation.position, progress);
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("[DRONE] Exit type is 'Fly Away' but no Exit Location specified. Drone will just disappear.");
        }

        // --- PHASE 3: CHAIN REACTION ---
        if (nextDroneToActivate != null)
        {
            nextDroneToActivate.SetActive(true);
        }

        Destroy(gameObject); // Bye bye
    }

    void SetEye(Material mat)
    {
        if (meshRenderer == null || mat == null)
            return;

        Material[] mats = meshRenderer.materials;
        if (eyeMaterialIndex >= 0 && eyeMaterialIndex < mats.Length) 
        {
            mats[eyeMaterialIndex] = mat;
            meshRenderer.materials = mats;
        }
        else
        {
            Debug.LogError($"[DRONE] Eye Material Index {eyeMaterialIndex} is out of bounds! Renderer has {mats.Length} materials.");
        }
    }

    private void ValidateSetup()
    {
        if (meshRenderer == null)
            Debug.LogWarning("[DRONE] Mesh Renderer not assigned! Eye animations will not work.");

        if (eyeNormal == null || eyeHappy == null || eyeDead == null)
            Debug.LogWarning("[DRONE] One or more eye materials are not assigned!");

        if (dialogueSequence == null || dialogueSequence.Length == 0)
            Debug.LogWarning("[DRONE] No dialogue entries configured!");

        if (!justDisintegrate && specificExitLocation == null)
            Debug.LogWarning("[DRONE] Exit type is 'Fly Away' but Exit Location is not assigned!");
    }
}