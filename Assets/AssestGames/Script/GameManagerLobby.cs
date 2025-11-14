using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerLobby : MonoBehaviour
{
    [SerializeField] private Button exploreButton;

    public void GoToExploreMode()
    {
        SceneManager.LoadScene("ExploreMode");
    }
}
