using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    // Reference to the score text UI element
    public TMP_Text scoreText;

    public void UpdateScoreText(int current)
    {
        // Displays as "x1", "x2", etc.
        scoreText.text = $"x{current}";
    }
}