using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper class to load scenes easily.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    public void LoadScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }
}
