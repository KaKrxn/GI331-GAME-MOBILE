using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Refs")]
    public Transform player;
    public TMP_Text distanceText;
    public TMP_Text coinsText;

    [Header("Formatting")]
    [Tooltip("รูปแบบการแสดงตัวเลขของระยะทาง/เหรียญ")]
    public string numberFormat = "N0";   // เช่น 1,234

    [Header("Options")]
    [Tooltip("นับระยะทางจากการเคลื่อนที่ (แกน XZ) เพื่อรองรับการเลี้ยว")]
    public bool accumulatePathDistance = true;

    // runtime state
    private Vector3 lastPos;
    private float totalDistance; // หน่วยเมตรโดยประมาณ (สะสมในระนาบ XZ)
    private int coins;

    // public getters (ให้สคริปต์อื่นอ่านได้)
    public int CurrentDistanceInt => Mathf.FloorToInt(totalDistance);
    public int CurrentCoins => coins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // ถ้าอยากคงค่าเวลาเปลี่ยนซีน ให้เปิดบรรทัดถัดไป
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!player)
        {
            Debug.LogError("[ScoreManager] Please assign Player Transform.");
            enabled = false;
            return;
        }

        lastPos = player.position;
        totalDistance = 0f;
        coins = 0;

        UpdateUI_Force();
    }

    void Update()
    {
        if (!player) return;

        if (accumulatePathDistance)
        {
            // วัดระยะทางที่เดินจริงบนระนาบ XZ (รองรับการเลี้ยว)
            Vector3 now = player.position;
            Vector2 a = new Vector2(lastPos.x, lastPos.z);
            Vector2 b = new Vector2(now.x, now.z);
            totalDistance += Vector2.Distance(a, b);
            lastPos = now;

            // อัปเดต UI ระยะทาง
            if (distanceText)
                distanceText.text = CurrentDistanceInt.ToString(numberFormat);
        }
        else
        {
            // โหมดเดิม (ถ้าจำเป็น): วัดเฉพาะแกน Z
            if (distanceText)
                distanceText.text = Mathf.Max(0, Mathf.FloorToInt(player.position.z - lastPos.z)).ToString(numberFormat);
        }
    }

    // ====== External API ======
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        if (coinsText)
            coinsText.text = coins.ToString(numberFormat);
    }

    public void ResetScore()
    {
        totalDistance = 0f;
        coins = 0;
        if (player) lastPos = player.position;
        UpdateUI_Force();
    }

    // ====== Helpers ======
    private void UpdateUI_Force()
    {
        if (distanceText) distanceText.text = CurrentDistanceInt.ToString(numberFormat);
        if (coinsText) coinsText.text = coins.ToString(numberFormat);
    }
}
