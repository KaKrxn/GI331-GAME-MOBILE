using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTriggerLoader : MonoBehaviour
{
    public enum LoadMode
    {
        ByName,
        ByBuildIndex,
        NextInBuildSettings
    }

    [Header("Trigger Settings")]
    [Tooltip("Tag ที่ต้องมาชน Trigger เช่น Player")]
    public string requiredTag = "Player";

    [Tooltip("ให้ Trigger ทำงานครั้งเดียวหรือไม่")]
    public bool onlyOnce = true;

    [Header("UI ก่อนโหลด")]
    [Tooltip("Panel ที่จะแสดงตอนเริ่มโหลด (เช่น หน้า Loading / Fade)")]
    public GameObject preLoadUIPanel;

    [Tooltip("Slider แสดงความคืบหน้าการโหลด (0–1)")]
    public Slider progressSlider;

    [Tooltip("เวลาขั้นต่ำที่ให้ UI โชว์ก่อนเข้าสู่ฉากใหม่ (วินาที)")]
    public float loadDelay = 0.5f;

    [Header("Scene Load Settings")]
    public LoadMode loadMode = LoadMode.ByName;

    [Tooltip("ใช้เมื่อ LoadMode = ByName")]
    public string sceneName;

    [Tooltip("ใช้เมื่อ LoadMode = ByBuildIndex")]
    public int buildIndex = 1;

    private bool hasTriggered = false;

    private void Start()
    {
        if (preLoadUIPanel != null)
            preLoadUIPanel.SetActive(false);

        if (progressSlider != null)
            progressSlider.value = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (onlyOnce && hasTriggered)
            return;

        hasTriggered = true;

        if (preLoadUIPanel != null)
            preLoadUIPanel.SetActive(true);

        if (progressSlider != null)
            progressSlider.value = 0f;

        StartCoroutine(LoadSceneAsyncRoutine());
    }

    private IEnumerator LoadSceneAsyncRoutine()
    {
        // หาว่าจะโหลด scene ไหน
        string targetSceneName = null;
        int targetIndex = -1;

        switch (loadMode)
        {
            case LoadMode.ByName:
                targetSceneName = sceneName;
                break;

            case LoadMode.ByBuildIndex:
                targetIndex = buildIndex;
                break;

            case LoadMode.NextInBuildSettings:
                int current = SceneManager.GetActiveScene().buildIndex;
                targetIndex = current + 1;
                break;
        }

        AsyncOperation op;

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            op = SceneManager.LoadSceneAsync(targetSceneName);
        }
        else
        {
            if (targetIndex < 0 || targetIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("[SceneTriggerLoader] target scene invalid");
                yield break;
            }

            op = SceneManager.LoadSceneAsync(targetIndex);
        }

        // ควบคุมจังหวะเข้า scene เอง
        op.allowSceneActivation = false;

        float timer = 0f;
        float displayedProgress = 0f; // ค่าที่ใช้กับ slider

        while (!op.isDone)
        {
            timer += Time.deltaTime;

            // progress จริงจาก Unity (0 → ~0.9)
            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);

            // ให้ slider ค่อยๆ วิ่งเข้า targetProgress
            // speed = 1 / loadDelay = เติมเต็มหลอดประมาณในเวลา loadDelay (ถ้า scene โหลดทัน)
            if (loadDelay > 0f)
            {
                float speed = 1f / loadDelay;
                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, speed * Time.deltaTime);
            }
            else
            {
                displayedProgress = targetProgress;
            }

            if (progressSlider != null)
                progressSlider.value = displayedProgress;

            // เงื่อนไขเข้า scene:
            // - โหลดจริงเสร็จ (targetProgress >= 1)
            // - slider เติมเต็มแล้ว (displayedProgress >= 0.999)
            // - เวลาโชว์ UI ครบขั้นต่ำ
            if (targetProgress >= 1f &&
                displayedProgress >= 0.999f &&
                timer >= loadDelay)
            {
                if (progressSlider != null)
                    progressSlider.value = 1f;

                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
