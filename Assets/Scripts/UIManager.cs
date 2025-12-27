using UnityEngine;
using TMPro;
using System.Collections; 

public class UIManager : MonoBehaviour
{
    [Header("Score UI")]
    [Tooltip("Drag your World Space 'HUD_Text_3D' object here")]
    public TMP_Text scoreText;

    [Header("Subtitle UI")]
    [Tooltip("Drag the background Panel (Image) here")]
    public GameObject subtitlePanel;
    [Tooltip("Drag the Subtitle Text (TMP) here")]
    public TMP_Text subtitleText;

    private Coroutine currentSubtitleRoutine;

    // --- SCORE LOGIC ---
    public void UpdateScoreText(int currentCount)
    {
        if (scoreText != null)
        {
            // Updates text to "x1", "x2", etc.
            scoreText.text = $"x{currentCount}";
        }
    }

    // --- SUBTITLE LOGIC ---
    public void ShowSubtitle(string message, float duration)
    {
        // If a subtitle is already running, stop it so we can overwrite it immediately
        if (currentSubtitleRoutine != null)
        {
            StopCoroutine(currentSubtitleRoutine);
        }

        currentSubtitleRoutine = StartCoroutine(SubtitleSequence(message, duration));
    }

    private IEnumerator SubtitleSequence(string message, float duration)
    {
        // 1. Show the panel and text
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(true);
            subtitleText.text = message;
        }

        // 2. Wait for the duration
        yield return new WaitForSeconds(duration);

        // 3. Hide everything
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (subtitleText != null) subtitleText.text = "";
    }
}