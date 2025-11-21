using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortalInteractable : MonoBehaviour
{
    [Header("Scene ที่จะโหลด")]
    [SerializeField] private string sceneName;

    public void TriggerSceneLoad()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[ScenePortalInteractable] {name} ยังไม่ได้ใส่ชื่อ Scene");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
