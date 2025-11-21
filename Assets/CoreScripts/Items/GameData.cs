#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    [Header("Currencies")]
    public int coins = 0;

    [Header("Debug: เคยเก็บ Item อะไรแล้วบ้าง")]
    [SerializeField] private List<string> collectedItemIdsDebug = new List<string>();

    [Header("Debug: จำนวน Item ปัจจุบันในระบบ (Snapshot)")]
    [SerializeField] private List<InventoryEntry> inventoryDebug = new List<InventoryEntry>();

    // ใช้จริงตอนรัน
    private HashSet<string> collectedItemIds = new HashSet<string>();              // เคยเก็บไหม
    private Dictionary<string, int> inventoryQuantities = new Dictionary<string, int>(); // จำนวน item ปัจจุบัน

    [System.Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int quantity;
    }

    // ---- PlayerPrefs Keys ----
    private const string CoinsKey = "GD_Coins";
    private const string CollectedKey = "GD_ItemsCollected";
    private const string InventoryKey = "GD_Inventory";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFromPrefs();
    }

    // ================= COINS =================

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
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

    // ================= เคยเก็บ Item อะไรแล้วบ้าง =================

    public void RegisterItemCollected(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        if (collectedItemIds.Add(itemId))
        {
            if (!collectedItemIdsDebug.Contains(itemId))
                collectedItemIdsDebug.Add(itemId);
        }
    }

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return collectedItemIds.Contains(itemId);
    }

    public bool HasAllItems(IEnumerable<string> requiredIds)
    {
        if (requiredIds == null) return false;

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

    // ================= INVENTORY ปัจจุบัน (id + quantity) =================

    /// <summary>
    /// ให้ InventoryLite เรียกเวลาต้องการตั้ง snapshot ปัจจุบันทั้งก้อน
    /// (ไม่ Save PlayerPrefs ทันทีนะ แค่เก็บไว้ใน GameData)
    /// </summary>
    public void ApplyInventorySnapshot(IEnumerable<InventoryEntry> entries)
    {
        inventoryQuantities.Clear();

        if (entries != null)
        {
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.itemId) || e.quantity <= 0)
                    continue;

                inventoryQuantities[e.itemId] = e.quantity;
                RegisterItemCollected(e.itemId);
            }
        }

        RefreshInventoryDebugList();
    }

    /// <summary>
    /// ให้ InventoryLite ดึง snapshot ปัจจุบันตอนโหลด Scene
    /// </summary>
    public List<InventoryEntry> GetInventorySnapshot()
    {
        RefreshInventoryDebugList();
        return new List<InventoryEntry>(inventoryDebug);
    }

    private void RefreshInventoryDebugList()
    {
        inventoryDebug.Clear();
        foreach (var kvp in inventoryQuantities)
        {
            inventoryDebug.Add(new InventoryEntry
            {
                itemId = kvp.Key,
                quantity = kvp.Value
            });
        }
    }

    // ================= SAVE / LOAD =================

    public void SaveToPrefs()
    {
        // Coins
        PlayerPrefs.SetInt(CoinsKey, coins);

        // Collected items
        string collectedJoined = string.Join("|", collectedItemIds);
        PlayerPrefs.SetString(CollectedKey, collectedJoined);

        // Inventory quantities: id:qty|id2:qty2|...
        List<string> parts = new List<string>();
        foreach (var kvp in inventoryQuantities)
        {
            parts.Add($"{kvp.Key}:{kvp.Value}");
        }
        string inventoryJoined = string.Join("|", parts);
        PlayerPrefs.SetString(InventoryKey, inventoryJoined);

        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        // Coins
        coins = PlayerPrefs.GetInt(CoinsKey, 0);

        // Collected
        collectedItemIds.Clear();
        collectedItemIdsDebug.Clear();
        string rawCollected = PlayerPrefs.GetString(CollectedKey, "");
        if (!string.IsNullOrEmpty(rawCollected))
        {
            string[] ids = rawCollected.Split('|');
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id)) continue;
                collectedItemIds.Add(id);
                collectedItemIdsDebug.Add(id);
            }
        }

        // Inventory
        inventoryQuantities.Clear();
        inventoryDebug.Clear();
        string rawInv = PlayerPrefs.GetString(InventoryKey, "");
        if (!string.IsNullOrEmpty(rawInv))
        {
            string[] entries = rawInv.Split('|');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                var split = entry.Split(':');
                if (split.Length != 2) continue;

                string id = split[0];
                if (string.IsNullOrEmpty(id)) continue;

                if (int.TryParse(split[1], out int qty) && qty > 0)
                {
                    inventoryQuantities[id] = qty;
                }
            }
        }

        RefreshInventoryDebugList();
    }

    [ContextMenu("Reset Game Data (Clear Save)")]
    public void ResetGameData()
    {
        // รีเซ็ตค่าที่เก็บไว้ใน GameData (runtime)
        coins = 0;
        collectedItemIds.Clear();
        collectedItemIdsDebug.Clear();
        inventoryQuantities.Clear();
        inventoryDebug.Clear();

        // ลบข้อมูลที่เคยเซฟไว้ใน PlayerPrefs ทั้งหมดของ GameData
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.DeleteKey(CollectedKey);
        PlayerPrefs.DeleteKey(InventoryKey);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        // ให้ Unity มองว่าคอมโพเนนต์นี้ถูกแก้ (ใช้เวลาไม่ได้กด Play)
        EditorUtility.SetDirty(this);
#endif

        Debug.Log("[GameData] Reset Game Data from Inspector (runtime + PlayerPrefs cleared)");
    }




}
