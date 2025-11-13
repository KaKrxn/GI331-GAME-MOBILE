using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTriggerLoader : MonoBehaviour
{
    public enum LoadMode
    {
        ByName,        // โหลดจากชื่อ Scene
        ByBuildIndex,  // โหลดจาก Build Index
        NextInBuildSettings // โหลด Scene ถัดไปจาก Scene ปัจจุบัน
    }

    [Header("Trigger Settings")]
    [Tooltip("Tag ที่ต้องการให้มากระตุ้น Trigger เช่น Player")]
    public string requiredTag = "Player";

    [Tooltip("ให้ Trigger ทำงานได้ครั้งเดียวหรือไม่")]
    public bool onlyOnce = true;

    [Tooltip("ดีเลย์ก่อนโหลด (วินาที) เช่น 1.5f ถ้ามีเอฟเฟกต์ Fade Out ก่อนโหลด")]
    public float loadDelay = 0f;

    [Header("Scene Load Settings")]
    public LoadMode loadMode = LoadMode.ByName;

    [Tooltip("ใช้เมื่อ LoadMode = ByName (ต้องพิมพ์ชื่อ Scene ให้ตรงกับใน Build Settings)")]
    public string sceneName;

    [Tooltip("ใช้เมื่อ LoadMode = ByBuildIndex (ลำดับ Scene ใน Build Settings)")]
    public int buildIndex = 1;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // ถ้าในโลก 2D ให้เปลี่ยนเป็น OnTriggerEnter2D(Collider2D other) + other.CompareTag เหมือนกัน
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (onlyOnce && hasTriggered)
            return;

        hasTriggered = true;

        if (loadDelay <= 0f)
        {
            LoadSceneNow();
        }
        else
        {
            StartCoroutine(LoadSceneDelayed());
        }
    }

    private IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSeconds(loadDelay);
        LoadSceneNow();
    }

    private void LoadSceneNow()
    {
        switch (loadMode)
        {
            case LoadMode.ByName:
                if (!string.IsNullOrEmpty(sceneName))
                {
                    SceneManager.LoadScene(sceneName);
                }
                else
                {
                    Debug.LogWarning("[SceneTriggerLoader] sceneName ว่างอยู่ แต่เลือกโหมด ByName");
                }
                break;

            case LoadMode.ByBuildIndex:
                if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(buildIndex);
                }
                else
                {
                    Debug.LogWarning($"[SceneTriggerLoader] buildIndex {buildIndex} ไม่อยู่ใน Build Settings");
                }
                break;

            case LoadMode.NextInBuildSettings:
                int current = SceneManager.GetActiveScene().buildIndex;
                int next = current + 1;

                if (next < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(next);
                }
                else
                {
                    Debug.LogWarning("[SceneTriggerLoader] ไม่มี Scene ถัดไปใน Build Settings แล้ว");
                }
                break;
        }
    }
}
