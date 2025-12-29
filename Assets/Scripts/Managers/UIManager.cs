using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Score UI")]
    public TMP_Text scoreText; 

    [Header("Subtitle UI")]
    public GameObject subtitlePanel;
    public TMP_Text subtitleText;

    private Coroutine currentSubtitleRoutine;

    private void Start()
    {
        // Find the Game Manager that survived from the previous scene
        if (GameManager.Instance != null)
        {
            // Link this UI Manager to the Game Manager
            GameManager.Instance.uiManager = this;

            // Initialize score display
            UpdateScoreText(GameManager.Instance.currentScore);
        }
    }

    // Score logic
    public void UpdateScoreText(int currentCount)
    {
        if (scoreText != null)
        {
            scoreText.text = $"x{currentCount}";
        }
    }

    // Subtitle display logic
    public void ShowSubtitle(string message, float duration)
    {
        if (currentSubtitleRoutine != null) StopCoroutine(currentSubtitleRoutine);
        currentSubtitleRoutine = StartCoroutine(SubtitleSequence(message, duration));
    }

    // Subtitle coroutine
    private IEnumerator SubtitleSequence(string message, float duration)
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(true);
            subtitleText.text = message;
        }

        yield return new WaitForSeconds(duration);

        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (subtitleText != null) subtitleText.text = "";
    }
}