using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles game over logic. Stops the game and opens the game over UI.
/// online service leaderboard and progression integration has been removed.
/// </summary>
public class GameOver : MonoBehaviour
{
    [SerializeField, Tooltip("Field for player to input their name (no longer sent to an online service).")]
    private TMP_InputField inputField;

    [SerializeField, Tooltip("The score of the player on their previous run.")]
    private TextMeshProUGUI scoreText;

    [SerializeField, Tooltip("Optional UI text for the scores of the players on the leaderboard (offline).")]
    private TextMeshProUGUI leaderboardScoreText;

    [SerializeField, Tooltip("Optional UI text for the names of the players on the leaderboard (offline).")]
    private TextMeshProUGUI leaderboardNameText;
    public string playagiansceneName;
    public string sceneName;

    private int score = 0;

    /// <summary>
    /// Called when the player gets a GameOver.
    /// </summary>
    /// <param name="score">Player score of the previous run.</param>
    public void StopGame(int score)
    {

        
        this.score = score;

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        // Previously this would also contact online service:
        // - GetLeaderboard();
        // - AddXP(score);
        // Those calls have been removed so the game runs fully offline.
    }

    /// <summary>
    /// Called when the player presses the button to submit their score.
    /// Online submission has been removed; this now just logs locally.
    /// </summary>
    public void SubmitScore()
    {
        Debug.Log($"SubmitScore called with score {score}, but online service has been removed.");
        // You can extend this to store a local high score using PlayerPrefs if desired.
    }

    /// <summary>
    /// Reloads the current scene.
    /// </summary>
    public void ReloadScene()
    {
        SceneManager.LoadScene(playagiansceneName);
    }
    public void BackloadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
