using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    [Header("Currencies")]
    public int coins = 0;

    [Header("Debug (ดูใน Inspector)")]
    [SerializeField] private List<string> collectedItemIdsDebug = new List<string>();

    // ใช้จริงตอนรัน
    private HashSet<string> collectedItemIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // sync list -> hashset เผื่อมีค่า initial
        collectedItemIds = new HashSet<string>(collectedItemIdsDebug);
    }

    // ---------- Coins ----------
    public void AddCoins(int amount)
    {
        coins += amount;
        if (coins < 0) coins = 0;
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;

        coins -= amount;
        return true;
    }

    // ---------- Items by ID ----------
    public void RegisterItemCollected(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        if (collectedItemIds.Add(itemId))
        {
            // sync ไว้ให้ดูใน Inspector
            collectedItemIdsDebug = new List<string>(collectedItemIds);
            // ถ้าจะทำระบบ Save จริง ๆ ค่อยเพิ่ม save ลง PlayerPrefs/JSON ทีหลัง
        }
    }

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return collectedItemIds.Contains(itemId);
    }

    public bool HasAllItems(params string[] requiredIds)
    {
        if (requiredIds == null || requiredIds.Length == 0) return true;

        foreach (var id in requiredIds)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (!collectedItemIds.Contains(id))
                return false;
        }
        return true;
    }

    public IReadOnlyCollection<string> GetCollectedItems()
    {
        return collectedItemIds;
    }
}
