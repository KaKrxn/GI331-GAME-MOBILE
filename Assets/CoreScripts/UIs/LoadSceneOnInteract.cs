using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // ถ้าใช้ New Input System

public class LoadSceneOnInteract : MonoBehaviour
{
    [Header("Scene ที่จะโหลด")]
    [SerializeField] private string sceneName;

    [Header("UI ปุ่ม/ไอคอน Interact (ไม่บังคับ)")]
    [SerializeField] private GameObject interactUI;

    private bool playerInRange = false;

    private void Update()
    {
        if (!playerInRange) return;

        // ถ้าใช้ New Input System (PlayerInput) แต่ขี้เกียจไปแตะ ExPlayerInteract
        // กดปุ่ม E จากคีย์บอร์ดตรง ๆ ไปก่อน
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            LoadTargetScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (interactUI != null)
            interactUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (interactUI != null)
            interactUI.SetActive(false);
    }

    // เอาไว้ให้ปุ่ม UI เรียกก็ได้ (OnClick)
    public void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[LoadSceneOnInteract] ยังไม่ได้ใส่ชื่อ Scene");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
