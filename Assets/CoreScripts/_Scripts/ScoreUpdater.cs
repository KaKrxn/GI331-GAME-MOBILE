using UnityEngine;
using TMPro;

/// <summary>
/// Responsible for updating the score text.
/// </summary>
public class ScoreUpdater : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI scoreText;

    /// <summary>
    /// Update the score text.
    /// </summary>
    /// <param name="score">The score of the player.</param>
    public void UpdateScore(int score) {
        scoreText.text = score.ToString();
    }
}
