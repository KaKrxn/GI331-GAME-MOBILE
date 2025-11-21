using System.Collections.Generic;
using UnityEngine;

public class InventoryLite : MonoBehaviour
{
    [System.Serializable]
    public class ItemStack
    {
        public string itemId;
        public ItemDefinition definition;
        public int quantity;

        public string DisplayName => definition ? definition.displayName : itemId;
    }

    [Header("Items (Debug / Inspector View)")]
    [SerializeField] private List<ItemStack> items = new List<ItemStack>();

    [Header("Item Database (ใช้ตอนโหลดจาก GameData)")]
    [Tooltip("ลาก ItemDefinition ทั้งหมดที่ใช้ในเกมมาใส่")]
    [SerializeField] private List<ItemDefinition> itemDatabase = new List<ItemDefinition>();

    private Dictionary<string, ItemStack> lookup = new Dictionary<string, ItemStack>();

    private void Awake()
    {
        RebuildLookup();
    }

    private void Start()
    {
        // โหลดค่าจาก GameData ครั้งเดียวตอนเข้า Scene
        LoadFromGameData();
    }

    private void OnDestroy()
    {
        // ตอนออกจาก Scene / ปิด Player object → ส่ง snapshot ปัจจุบันกลับ GameData + Save
        SaveToGameData(saveToDisk: true);
    }

    // =============== Helpers ===============

    private void RebuildLookup()
    {
        lookup.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            var stack = items[i];
            if (stack == null || string.IsNullOrEmpty(stack.itemId))
                continue;

            lookup[stack.itemId] = stack;
        }
    }

    private ItemDefinition FindDefinitionById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        for (int i = 0; i < itemDatabase.Count; i++)
        {
            if (itemDatabase[i] != null && itemDatabase[i].itemId == itemId)
                return itemDatabase[i];
        }

        return null;
    }

    private ItemStack GetOrCreateStack(ItemDefinition definition)
    {
        string itemId = definition.itemId;

        if (lookup.TryGetValue(itemId, out var existing))
            return existing;

        ItemStack stack = new ItemStack
        {
            itemId = itemId,
            definition = definition,
            quantity = 0
        };
        items.Add(stack);
        lookup[itemId] = stack;

        return stack;
    }

    // =============== Load / Save กับ GameData ===============

    public void LoadFromGameData()
    {
        if (GameData.Instance == null)
            return;

        items.Clear();
        lookup.Clear();

        var snapshot = GameData.Instance.GetInventorySnapshot();
        foreach (var entry in snapshot)
        {
            if (string.IsNullOrEmpty(entry.itemId) || entry.quantity <= 0)
                continue;

            ItemDefinition def = FindDefinitionById(entry.itemId);

            ItemStack stack = new ItemStack
            {
                itemId = entry.itemId,
                definition = def,
                quantity = entry.quantity
            };

            items.Add(stack);
            if (!lookup.ContainsKey(entry.itemId))
                lookup.Add(entry.itemId, stack);
        }
    }

    public void SaveToGameData(bool saveToDisk)
    {
        if (GameData.Instance == null)
            return;

        List<GameData.InventoryEntry> snapshot = new List<GameData.InventoryEntry>();
        foreach (var stack in items)
        {
            if (stack == null || string.IsNullOrEmpty(stack.itemId))
                continue;

            if (stack.quantity <= 0) continue;

            snapshot.Add(new GameData.InventoryEntry
            {
                itemId = stack.itemId,
                quantity = stack.quantity
            });
        }

        GameData.Instance.ApplyInventorySnapshot(snapshot);

        if (saveToDisk)
        {
            GameData.Instance.SaveToPrefs();
        }
    }

    // =============== Public API ===============

    public void AddItem(ItemDefinition definition, int amount)
    {
        if (definition == null || amount <= 0)
            return;

        if (string.IsNullOrEmpty(definition.itemId))
        {
            Debug.LogWarning($"[InventoryLite] ItemDefinition {definition.name} ไม่มี itemId");
            return;
        }

        ItemStack stack = GetOrCreateStack(definition);

        if (!definition.stackable)
        {
            // ถ้าไม่ stack ก็ถือว่า 1 ชิ้นพอ (หรือจะรองรับหลาย slot ค่อยดีไซน์เพิ่มภายหลัง)
            stack.quantity = 1;
        }
        else
        {
            stack.quantity += amount;
            if (stack.quantity > definition.maxStack)
                stack.quantity = definition.maxStack;
        }

        // บอก GameData ว่าเคยเก็บ item นี้แล้ว (ไม่เซฟทันที)
        if (GameData.Instance != null)
        {
            GameData.Instance.RegisterItemCollected(definition.itemId);
        }
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        if (!lookup.TryGetValue(itemId, out var stack))
            return false;

        if (stack.quantity < amount)
            return false;

        stack.quantity -= amount;
        if (stack.quantity < 0) stack.quantity = 0;

        return true;
    }

    public int GetQuantity(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;

        if (lookup.TryGetValue(itemId, out var stack))
            return stack.quantity;

        return 0;
    }

    public IReadOnlyList<ItemStack> GetAllStacks()
    {
        return items;
    }
}
