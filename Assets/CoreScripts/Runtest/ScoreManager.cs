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
    [Tooltip("�ٻẺ����ʴ�����Ţ�ͧ���зҧ/����­")]
    public string numberFormat = "N0";   // �� 1,234

    [Header("Options")]
    [Tooltip("�Ѻ���зҧ�ҡ�������͹��� (᡹ XZ) �����ͧ�Ѻ���������")]
    public bool accumulatePathDistance = true;

    // runtime state
    private Vector3 lastPos;
    private float totalDistance; // ˹��������»���ҳ (������йҺ XZ)
    private int coins;

    // public getters (���ʤ�Ի�������ҹ��)
    public int CurrentDistanceInt => Mathf.FloorToInt(totalDistance);
    public int CurrentCoins => coins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // �����ҡ�������������¹�չ ����Դ��÷Ѵ�Ѵ�
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
            // �Ѵ���зҧ����Թ��ԧ���йҺ XZ (�ͧ�Ѻ���������)
            Vector3 now = player.position;
            Vector2 a = new Vector2(lastPos.x, lastPos.z);
            Vector2 b = new Vector2(now.x, now.z);
            totalDistance += Vector2.Distance(a, b);
            lastPos = now;

            // �ѻവ UI ���зҧ
            if (distanceText)
                distanceText.text = CurrentDistanceInt.ToString(numberFormat);
        }
        else
        {
            // ������� (��Ҩ���): �Ѵ੾��᡹ Z
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
